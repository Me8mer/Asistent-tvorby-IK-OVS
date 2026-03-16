using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Assistant.Core.Model;
using Assistant.Dependencies.Context.Web;
using Assistant.Dependencies.Context.Web.Retrieval;
using Assistant.Dependencies.Context.Web.Storage;

namespace Assistant.Dependencies.Context
{
    /// <summary>
    /// Orchestrates construction of a <see cref="WebContextPack"/> for a given
    /// office using the internal document model.  This composite provider is
    /// intended to be consumed by higher‑level layers of the application (e.g.
    /// the initiation pipeline) and hides the details of crawling, query
    /// generation and chunk retrieval.
    ///
    /// When <see cref="GetOrBuildContextPackAsync"/> is invoked the provider
    /// performs the following steps:
    /// <list type="number">
    ///   <item>
    ///     <description>Ensures the web pages for the office have been crawled and
    ///     chunked, returning a normalized office key.  This is handled by
    ///     <see cref="OfficeWebContextProvider.GetOrBuildChunkCorpusAsync"/>.</description>
    ///   </item>
    ///   <item>
    ///     <description>Attempts to load a previously saved <see cref="WebContextPack"/> from
    ///     <see cref="OfficeCacheStore"/>.  If a cached pack exists it is returned
    ///     immediately.</description>
    ///   </item>
    ///   <item>
    ///     <description>Iterates over all sections in the provided <see cref="InternalModel"/> and uses
    ///     <see cref="SectionQueryBuilder"/> to derive a set of search terms and a
    ///     single multi‑line query string.  The builder assembles terms from the
    ///     section’s path, field aliases and optional query hints, deduplicates
    ///     them and respects a maximum character limit【128327383949581†L27-L49】.</description>
    ///   </item>
    ///   <item>
    ///     <description>For each non‑empty query the provider invokes
    ///     <see cref="WebSectionPackRetriever.GetOrBuildSectionPackAsync"/> to retrieve
    ///     the highest scoring chunks according to a BM25 ranking.  The retriever
    ///     caches results keyed by office and section so repeated calls are
    ///     inexpensive.</description>
    ///   </item>
    ///   <item>
    ///     <description>All retrieved chunks are converted into <see cref="WebContextSnippet"/>
    ///     objects with a structured reference identifier and aggregated into a
    ///     single <see cref="WebContextPack"/>.  The assembled pack is
    ///     persisted via <see cref="OfficeCacheStore.SaveContextPackAsync"/>.</description>
    ///   </item>
    /// </list>
    ///
    /// This design separates concerns, allows multiple sections to be processed
    /// concurrently and ensures that external callers do not need to interact
    /// with low‑level crawling or retrieval classes.  Additional context sources
    /// (e.g. intranet search, document libraries) can be integrated in the
    /// future by extending this provider.
    /// </summary>
    public sealed class CompositeContextProvider
    {
        private readonly OfficeWebContextProvider officeContextProvider;
        private readonly WebSectionPackRetriever sectionPackRetriever;
        private readonly OfficeCacheStore cacheStore;
        private readonly SectionQueryBuilder sectionQueryBuilder;

        /// <summary>
        /// Initializes a new instance of the <see cref="CompositeContextProvider"/>.
        /// </summary>
        /// <param name="officeContextProvider">Responsible for crawling and
        /// chunking web pages for a given office.</param>
        /// <param name="sectionPackRetriever">Retrieves the most relevant chunks
        /// for a given query.  The retrieval algorithm applies BM25 scoring with
        /// diversity and length constraints.</param>
        /// <param name="cacheStore">Cache store used to persist and retrieve
        /// previously built corpora, section packs and context packs.</param>
        /// <param name="sectionQueryBuilder">Builds structured queries for each
        /// section based on its descriptor and position in the internal model.</param>
        public CompositeContextProvider(
            OfficeWebContextProvider officeContextProvider,
            WebSectionPackRetriever sectionPackRetriever,
            OfficeCacheStore cacheStore,
            SectionQueryBuilder? sectionQueryBuilder = null)
        {
            this.officeContextProvider = officeContextProvider ?? throw new ArgumentNullException(nameof(officeContextProvider));
            this.sectionPackRetriever = sectionPackRetriever ?? throw new ArgumentNullException(nameof(sectionPackRetriever));
            this.cacheStore = cacheStore ?? throw new ArgumentNullException(nameof(cacheStore));
            this.sectionQueryBuilder = sectionQueryBuilder ?? new SectionQueryBuilder();
        }

        /// <summary>
        /// Retrieves or builds a web context pack for a given office and internal model.
        /// This is a thin wrapper around <see cref="GetOrBuildContextPackAsync"/> for
        /// backward compatibility.  Callers should prefer <see cref="GetOrBuildContextPackAsync"/>
        /// for clarity.
        /// </summary>
        /// <param name="internalModel">The parsed document model describing the
        /// section hierarchy and field aliases.</param>
        /// <param name="officeIdentifier">A URL or other identifier representing
        /// the office.  This may include protocol, which will be normalized.</param>
        /// <param name="cancellationToken">Propagation token used to cancel
        /// crawling operations.</param>
        /// <returns>An assembled <see cref="WebContextPack"/>.</returns>
        public Task<WebContextPack> GetOrBuildAsync(
            InternalModel internalModel,
            string officeIdentifier,
            CancellationToken cancellationToken) =>
            GetOrBuildContextPackAsync(internalModel, officeIdentifier, cancellationToken);

        /// <summary>
        /// Retrieves or builds a consolidated web context pack for the specified
        /// office.  The internal model defines the set of sections for which
        /// context is required.  If a cached pack exists on disk for the given
        /// office the cached result is returned.  Otherwise the method crawls
        /// the office website, constructs search queries via
        /// <see cref="SectionQueryBuilder"/> and retrieves relevant chunks via
        /// <see cref="WebSectionPackRetriever"/>.  All section retrievals are
        /// performed concurrently to improve throughput.
        /// </summary>
        /// <param name="internalModel">The parsed document model describing the
        /// section hierarchy and field aliases.</param>
        /// <param name="officeIdentifier">A URL or other identifier representing
        /// the office.  This may include protocol, which will be normalized.</param>
        /// <param name="cancellationToken">Propagation token used to cancel
        /// crawling operations.</param>
        /// <returns>A task that resolves to a <see cref="WebContextPack"/> containing
        /// snippets for each section in the model.</returns>
        public async Task<WebContextPack> GetOrBuildContextPackAsync(
            InternalModel internalModel,
            string officeIdentifier,
            CancellationToken cancellationToken)
        {
            if (internalModel is null)
                throw new ArgumentNullException(nameof(internalModel));
            if (string.IsNullOrWhiteSpace(officeIdentifier))
                throw new ArgumentException("Office identifier must not be null or whitespace.", nameof(officeIdentifier));

            // Ensure a chunk corpus exists for the office.  This may trigger a crawl
            // and chunking operation.  The returned corpus includes the normalized
            // office key used for all subsequent operations.
            WebChunkCorpus chunkCorpus = await officeContextProvider
                .GetOrBuildChunkCorpusAsync(officeIdentifier, cancellationToken)
                .ConfigureAwait(false);

            string officeKey = chunkCorpus.OfficeKey;

            // Try to load an existing context pack from the cache.  If present return
            // immediately to avoid redundant work.
            WebContextPack? cachedPack = await cacheStore.LoadContextPackAsync(officeKey).ConfigureAwait(false);
            if (cachedPack != null)
                return cachedPack;

            IReadOnlyDictionary<SectionAlias, SectionDescriptor> sectionsByAlias = internalModel.SectionsByAlias;

            // Build queries for each section.  Collect tasks for concurrent retrieval.
            var retrievalTasks = new List<Task<(string SectionKey, WebSectionPack SectionPack)>>();

            foreach (KeyValuePair<SectionAlias, SectionDescriptor> kvp in sectionsByAlias)
            {
                SectionAlias sectionAlias = kvp.Key;

                    // Use the query builder to assemble search terms from localized
                    // section and field metadata, with alias-based fallback only when
                    // necessary. The builder enforces
                // deduplication and length limits
                    SectionQuery sectionQuery = sectionQueryBuilder.BuildSectionQuery(sectionAlias, internalModel);
                string queryText = sectionQuery.QueryText;

                // Skip sections with no meaningful search terms.  Structural or empty
                // sections produce an empty query.
                if (string.IsNullOrWhiteSpace(queryText))
                    continue;

                string sectionKey = sectionAlias.Value;

                retrievalTasks.Add(FetchSectionPackAsync(officeKey, sectionKey, queryText));
            }

            // Execute all section retrievals in parallel.  Each task returns the
            // section key along with the retrieved pack.
            (string SectionKey, WebSectionPack SectionPack)[] results = await Task.WhenAll(retrievalTasks).ConfigureAwait(false);

            var snippets = new List<WebContextSnippet>();

            foreach ((string sectionKey, WebSectionPack sectionPack) in results)
            {
                foreach (WebSectionPackItem item in sectionPack.Items)
                {
                    string referenceId = BuildReferenceId(officeKey, sectionKey, item.ChunkId);
                    var snippet = new WebContextSnippet(
                        intent: sectionKey,
                        providerKind: "WEB",
                        referenceId: referenceId,
                        text: item.Text);
                    snippets.Add(snippet);
                }
            }

            var contextPack = new WebContextPack(
                officeKey: officeKey,
                builtAtUtc: DateTime.UtcNow,
                snippets: snippets);

            // Persist the assembled pack to the cache for future use.
            await cacheStore.SaveContextPackAsync(contextPack).ConfigureAwait(false);

            return contextPack;
        }

        /// <summary>
        /// Helper method used to fetch a section pack and return it along with its
        /// section key.  This allows the retrieval calls to be composed
        /// concurrently via <see cref="Task.WhenAll"/>.
        /// </summary>
        /// <param name="officeKey">The normalized key for the office.</param>
        /// <param name="sectionKey">The key of the section (alias value).</param>
        /// <param name="queryText">The query text to pass to the retriever.</param>
        private async Task<(string SectionKey, WebSectionPack SectionPack)> FetchSectionPackAsync(
            string officeKey,
            string sectionKey,
            string queryText)
        {
            WebSectionPack pack = await sectionPackRetriever.GetOrBuildSectionPackAsync(officeKey, sectionKey, queryText).ConfigureAwait(false);
            return (sectionKey, pack);
        }

        /// <summary>
        /// Builds a structured reference identifier for a snippet.  The
        /// identifier is composed of multiple tokens so that clients can
        /// reconstruct the provenance of each snippet (office, section, chunk).
        /// </summary>
        private static string BuildReferenceId(string officeKey, string sectionKey, string chunkId)
        {
            string safeOfficeKey = string.IsNullOrWhiteSpace(officeKey) ? "unknown" : officeKey;
            string safeSectionKey = string.IsNullOrWhiteSpace(sectionKey) ? "unknown" : sectionKey;
            string safeChunkId = string.IsNullOrWhiteSpace(chunkId) ? "unknown" : chunkId;
            return $"WEB|{safeOfficeKey}|{safeSectionKey}|{safeChunkId}";
        }
    }
}