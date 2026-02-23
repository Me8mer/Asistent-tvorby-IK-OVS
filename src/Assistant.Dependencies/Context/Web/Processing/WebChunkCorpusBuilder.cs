using System;
using System.Collections.Generic;
using Assistant.Dependencies.Context.Web.Storage;
using System.Linq;

namespace Assistant.Dependencies.Context.Web.Processing
{
    public sealed class WebChunkCorpusBuilder
    {
        public sealed class Options
        {
            public int MinimumPageTextLength { get; init; } = 100;
            public int MinimumChunkTextLength { get; init; } = 100;
            public int MinimumTokenLength { get; init; } = 3;

            public Options()
            {
            }
        }

        private readonly Options options;

        public WebChunkCorpusBuilder(Options? options = null)
        {
            this.options = options ?? new Options();
        }

        public WebChunkCorpus Build(WebCorpus corpus)
        {
            if (corpus == null)
                throw new ArgumentNullException(nameof(corpus));

            IReadOnlyList<WebCorpusPage> filteredPages = FilterPages(corpus.Pages);

            var chunkPages = new List<WebChunkPage>(filteredPages.Count);

            foreach (WebCorpusPage page in filteredPages)
            {
                IReadOnlyList<string> chunks = WebTextChunker.SplitIntoChunks(page.ExtractedText);

                var keptChunks = new List<WebChunk>(chunks.Count);

                for (int chunkIndex = 0; chunkIndex < chunks.Count; chunkIndex++)
                {
                    string chunkText = chunks[chunkIndex]?.Trim() ?? string.Empty;

                    if (chunkText.Length < options.MinimumChunkTextLength)
                        continue;

                    if (LooksLikeNavigationNoise(chunkText))
                        continue;

                    string chunkId = BuildChunkId(page.ContentHash, chunkIndex);

                    List<string> tokensInChunk = WebTextTokenizer.TokenizeForIndex(chunkText, options.MinimumTokenLength).ToList();

                    if (tokensInChunk.Count == 0)
                        continue;

                    var tokenCounts = new Dictionary<string, int>(StringComparer.Ordinal);

                    for (int tokenIndex = 0; tokenIndex < tokensInChunk.Count; tokenIndex++)
                    {
                        string token = tokensInChunk[tokenIndex];

                        if (tokenCounts.TryGetValue(token, out int existingCount))
                            tokenCounts[token] = existingCount + 1;
                        else
                            tokenCounts[token] = 1;
                    }

                    keptChunks.Add(new WebChunk(
                        chunkId: chunkId,
                        chunkIndex: chunkIndex,
                        text: chunkText,
                        totalTokenCount: tokensInChunk.Count,
                        tokenCounts: tokenCounts));
                }

                if (keptChunks.Count == 0)
                    continue;

                chunkPages.Add(new WebChunkPage(
                    url: page.Url,
                    pageHash: page.ContentHash,
                    chunks: keptChunks));
            }

            (int totalChunks, double averageChunkTokenCount, IReadOnlyDictionary<string, double> idfByToken) =
                ComputeStatistics(chunkPages, minimumTokenLength: options.MinimumTokenLength);
            return new WebChunkCorpus(
                officeKey: corpus.OfficeKey,
                builtAtUtc: DateTime.UtcNow,
                totalChunks: totalChunks,
                averageChunkTokenCount: averageChunkTokenCount,
                inverseDocumentFrequencyByToken: idfByToken,
                pages: chunkPages);
        }

        private IReadOnlyList<WebCorpusPage> FilterPages(IReadOnlyList<WebCorpusPage> pages)
        {
            var filteredPages = new List<WebCorpusPage>(pages.Count);

            var seenUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var seenHashes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (WebCorpusPage page in pages)
            {
                if (page == null)
                    continue;

                if (string.IsNullOrWhiteSpace(page.Url))
                    continue;

                string trimmedUrl = page.Url.Trim();
                if (!seenUrls.Add(trimmedUrl))
                    continue;

                string extractedText = page.ExtractedText ?? string.Empty;
                if (extractedText.Length < options.MinimumPageTextLength)
                    continue;

                string pageHash = page.ContentHash ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(pageHash))
                {
                    if (!seenHashes.Add(pageHash))
                        continue;
                }

                if (LooksLikeJunkUrl(trimmedUrl))
                    continue;

                filteredPages.Add(page);
            }

            return filteredPages;
        }

        private static bool LooksLikeJunkUrl(string url)
        {
            string lower = url.ToLowerInvariant();

            foreach (string substring in JunkUrlSubstrings)
            {
                if (lower.Contains(substring))
                    return true;
            }

            return false;
        }

        private static bool LooksLikeNavigationNoise(string chunkText)
        {
            string[] lines = chunkText.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            if (lines.Length >= 8)
            {
                int shortLineCount = 0;

                foreach (string line in lines)
                {
                    string trimmedLine = line.Trim();
                    if (trimmedLine.Length <= 25)
                        shortLineCount++;
                }

                if (shortLineCount * 100 / lines.Length >= 70)
                    return true;
            }

            return false;
        }

        private static string BuildChunkId(string pageHash, int chunkIndex)
        {
            string safePageHash = string.IsNullOrWhiteSpace(pageHash) ? "nohash" : pageHash;
            return safePageHash + "_" + chunkIndex;
        }


        private static (int TotalChunks, double AverageChunkTokenCount, IReadOnlyDictionary<string, double> IdfByToken)
            ComputeStatistics(IReadOnlyList<WebChunkPage> pages, int minimumTokenLength)
        {
            int totalChunks = 0;
            long totalTokenCount = 0;

            var documentFrequencyByToken = new Dictionary<string, int>(StringComparer.Ordinal);

            for (int pageIndex = 0; pageIndex < pages.Count; pageIndex++)
            {
                WebChunkPage page = pages[pageIndex];

                for (int chunkIndex = 0; chunkIndex < page.Chunks.Count; chunkIndex++)
                {
                    WebChunk chunk = page.Chunks[chunkIndex];
                    totalChunks++;

                    List<string> tokensInChunk = WebTextTokenizer.TokenizeForIndex(chunk.Text, minimumTokenLength).ToList();

                    totalTokenCount += tokensInChunk.Count;

                    var distinctTokensInChunk = new HashSet<string>(tokensInChunk, StringComparer.Ordinal);

                    foreach (string token in chunk.TokenCounts.Keys)
                    {
                        if (documentFrequencyByToken.TryGetValue(token, out int existingCount))
                            documentFrequencyByToken[token] = existingCount + 1;
                        else
                            documentFrequencyByToken[token] = 1;
                    }
                }
            }

            double averageChunkTokenCount = totalChunks > 0
                ? (double)totalTokenCount / totalChunks
                : 0;

            var idfByToken = new Dictionary<string, double>(documentFrequencyByToken.Count, StringComparer.Ordinal);

            foreach (KeyValuePair<string, int> pair in documentFrequencyByToken)
            {
                string token = pair.Key;
                int documentFrequency = pair.Value;

                double idf = Math.Log(1.0 + ((totalChunks - documentFrequency + 0.5) / (documentFrequency + 0.5)));
                idfByToken[token] = idf;
            }

            return (totalChunks, averageChunkTokenCount, idfByToken);
        }

        // TODO. Check if too agressive
        private static readonly string[] JunkUrlSubstrings =
        {
            "cookie",
            "cookies",
            // "gdpr",
            // "privacy",
            // "ochrana-osobnich-udaju",
            // "pristupnost",
            // "accessibility",
            // "sitemap",
            // "mapa-stranek",
            // "vyhledavani",
            // "search",
            // "galerie",
            // "fotogalerie",
            // "gallery",
            // "photo",
            // "archiv",
            // "archive",
            "novinky",
            "news"
        };
    }
}
