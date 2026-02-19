using System;
using System.Collections.Generic;
using Assistant.Dependencies.Context.Web.Storage;

namespace Assistant.Dependencies.Context.Web.Processing
{
    public sealed class WebChunkCorpusBuilder
    {
        public sealed class Options
        {
            public int MinimumPageTextLength { get; init; } = 100;
            public int MinimumChunkTextLength { get; init; } = 100;

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

                    keptChunks.Add(new WebChunk(
                        chunkId: chunkId,
                        chunkIndex: chunkIndex,
                        text: chunkText));
                }

                if (keptChunks.Count == 0)
                    continue;

                chunkPages.Add(new WebChunkPage(
                    url: page.Url,
                    pageHash: page.ContentHash,
                    chunks: keptChunks));
            }

            return new WebChunkCorpus(
                officeKey: corpus.OfficeKey,
                builtAtUtc: DateTime.UtcNow,
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
            // "novinky",
            "news"
        };
    }
}
