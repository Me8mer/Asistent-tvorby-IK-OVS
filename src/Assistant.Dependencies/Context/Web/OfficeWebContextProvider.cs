using System;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Assistant.Dependencies.Context.Web.Crawling;
using Assistant.Dependencies.Context.Web.Processing;
using Assistant.Dependencies.Context.Web.Storage;

namespace Assistant.Dependencies.Context.Web
{
    public sealed class OfficeWebContextProvider
    {
        private readonly IManagedCrawler managedCrawler;
        private readonly OfficeCacheStore cacheStore;
        private readonly WebCorpusBuilder corpusBuilder;
        private readonly WebChunkCorpusBuilder chunkCorpusBuilder;

        public OfficeWebContextProvider(
            IManagedCrawler managedCrawler,
            OfficeCacheStore cacheStore,
            WebCorpusBuilder corpusBuilder,
            WebChunkCorpusBuilder chunkCorpusBuilder)
        {
            this.managedCrawler = managedCrawler ?? throw new ArgumentNullException(nameof(managedCrawler));
            this.cacheStore = cacheStore ?? throw new ArgumentNullException(nameof(cacheStore));
            this.corpusBuilder = corpusBuilder ?? throw new ArgumentNullException(nameof(corpusBuilder));
            this.chunkCorpusBuilder = chunkCorpusBuilder ?? throw new ArgumentNullException(nameof(chunkCorpusBuilder));
        }

        public async Task<WebChunkCorpus> GetOrBuildChunkCorpusAsync(
            string officeIdentifier,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(officeIdentifier))
                throw new ArgumentException("Office identifier must not be empty.", nameof(officeIdentifier));

            string officeKey = NormalizeOfficeKey(officeIdentifier);

            WebChunkCorpus? cachedChunkCorpus = await cacheStore.LoadChunkCorpusAsync(officeKey).ConfigureAwait(false);
            if (cachedChunkCorpus != null)
                return cachedChunkCorpus;

            CrawlResult crawlResult = await managedCrawler.RunAsync(officeIdentifier, cancellationToken).ConfigureAwait(false);

            // Ensure officeKey is used for storage, even if crawler returns a different identifier.
            var normalizedCrawlResult = new CrawlResult(
                officeKey: officeKey,
                pages: crawlResult.Pages);

            WebCorpus corpus = corpusBuilder.BuildCorpus(normalizedCrawlResult);
            await cacheStore.SaveCorpusAsync(corpus).ConfigureAwait(false);

            WebChunkCorpus chunkCorpus = chunkCorpusBuilder.Build(corpus);
            await cacheStore.SaveChunkCorpusAsync(chunkCorpus).ConfigureAwait(false);

            return chunkCorpus;
        }

        private static string NormalizeOfficeKey(string officeIdentifier)
        {
            string trimmed = officeIdentifier.Trim();

            if (trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
                trimmed = trimmed.Substring("http://".Length);

            if (trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                trimmed = trimmed.Substring("https://".Length);

            int firstSlashIndex = trimmed.IndexOf('/');
            if (firstSlashIndex >= 0)
                trimmed = trimmed.Substring(0, firstSlashIndex);

            trimmed = trimmed.ToLowerInvariant();

            var builder = new StringBuilder(trimmed.Length);

            bool previousWasSeparator = false;

            foreach (char character in trimmed)
            {
                if (char.IsLetterOrDigit(character))
                {
                    builder.Append(character);
                    previousWasSeparator = false;
                    continue;
                }

                if (character == '.' || character == '_' || character == ':')
                {
                    if (!previousWasSeparator && builder.Length > 0)
                    {
                        builder.Append('-');
                        previousWasSeparator = true;
                    }
                }
            }

            string result = builder.ToString().Trim('-');

            if (string.IsNullOrWhiteSpace(result))
                result = "office-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);

            return result;
        }
    }
}
