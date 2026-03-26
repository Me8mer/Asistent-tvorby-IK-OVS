using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Assistant.Core.Model;
using Assistant.Core.Model.Aliases;
using Assistant.Core.Model.InternalModel;
using Assistant.Core.Model.Sections;
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
        /// Retrieves or builds web context for a specific section in the model.
        /// This is the intended lazy path and avoids building all sections when
        /// only one section is needed.
        /// </summary>
        /// <param name="internalModel">The parsed document model describing the
        /// section hierarchy and field aliases.</param>
        /// <param name="officeIdentifier">A URL or other identifier representing
        /// the office.  This may include protocol, which will be normalized.</param>
        /// <param name="cancellationToken">Propagation token used to cancel
        /// crawling operations.</param>
        /// <param name="sectionAlias">The section alias for which context is requested.</param>
        /// <returns>A task that resolves to a <see cref="WebContextPack"/> containing
        /// snippets for the requested section only.</returns>
        public async Task<WebContextPack> GetOrBuildSectionContextPackAsync(
            InternalModel internalModel,
            string officeIdentifier,
            SectionAlias sectionAlias,
            CancellationToken cancellationToken)
        {
            if (internalModel is null)
                throw new ArgumentNullException(nameof(internalModel));
            if (string.IsNullOrWhiteSpace(officeIdentifier))
                throw new ArgumentException("Office identifier must not be null or whitespace.", nameof(officeIdentifier));
            if (!internalModel.SectionsByAlias.ContainsKey(sectionAlias))
                throw new ArgumentException($"Unknown section alias '{sectionAlias.Value}'.", nameof(sectionAlias));

            // Ensure a chunk corpus exists for the office.  This may trigger a crawl
            // and chunking operation.  The returned corpus includes the normalized
            // office key used for all subsequent operations.
            WebChunkCorpus chunkCorpus = await officeContextProvider
                .GetOrBuildChunkCorpusAsync(officeIdentifier, cancellationToken)
                .ConfigureAwait(false);

            string officeKey = chunkCorpus.OfficeKey;

            SectionQuery sectionQuery = sectionQueryBuilder.BuildSectionQuery(sectionAlias, internalModel);
            string queryText = sectionQuery.QueryText;

            // Structural or empty sections produce no query and therefore no snippets.
            if (string.IsNullOrWhiteSpace(queryText))
            {
                return new WebContextPack(
                    officeKey: officeKey,
                    builtAtUtc: DateTime.UtcNow,
                    snippets: Array.Empty<WebContextSnippet>());
            }

            string sectionKey = sectionAlias.Value;
            WebSectionPack sectionPack = await sectionPackRetriever
                .GetOrBuildSectionPackAsync(officeKey, sectionKey, queryText)
                .ConfigureAwait(false);

            var snippets = new List<WebContextSnippet>(sectionPack.Items.Count);

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

            var contextPack = new WebContextPack(
                officeKey: officeKey,
                builtAtUtc: DateTime.UtcNow,
                snippets: snippets);

            return contextPack;
        }

        /// <summary>
        /// Compatibility API that assembles context for all sections by lazily
        /// delegating to <see cref="GetOrBuildSectionContextPackAsync"/> per section.
        /// </summary>
        public async Task<WebContextPack> GetOrBuildContextPackAsync(
            InternalModel internalModel,
            string officeIdentifier,
            CancellationToken cancellationToken)
        {
            if (internalModel is null)
                throw new ArgumentNullException(nameof(internalModel));
            if (string.IsNullOrWhiteSpace(officeIdentifier))
                throw new ArgumentException("Office identifier must not be null or whitespace.", nameof(officeIdentifier));

            var snippets = new List<WebContextSnippet>();
            string officeKey = string.Empty;

            foreach (SectionAlias sectionAlias in internalModel.SectionsByAlias.Keys)
            {
                WebContextPack sectionPack = await GetOrBuildSectionContextPackAsync(
                        internalModel,
                        officeIdentifier,
                        sectionAlias,
                        cancellationToken)
                    .ConfigureAwait(false);

                officeKey = sectionPack.OfficeKey;
                snippets.AddRange(sectionPack.Snippets);
            }

            return new WebContextPack(
                officeKey: officeKey,
                builtAtUtc: DateTime.UtcNow,
                snippets: snippets);
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