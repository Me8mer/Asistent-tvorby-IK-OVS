using System;
using System.Collections.Generic;
using System.Linq;
using Assistant.Dependencies.Context.Web.Storage;
using Assistant.Dependencies.Context.Web.Processing;
using System.Threading.Tasks;


namespace Assistant.Dependencies.Context.Web.Retrieval
{
    public sealed class WebSectionPackRetriever
    {
        public sealed class Options
        {
            public int MaximumChunks { get; init; } = 10;
            public int MaximumTotalCharacters { get; init; } = 5000;
            public int MaximumChunksPerUrl { get; init; } = 2;

            public int MinimumTokenLength { get; init; } = 3;
            public double Bm25K1 { get; init; } = 1.2;
            public double Bm25B { get; init; } = 0.75;

            public Options()
            {
            }
        }

        private readonly OfficeCacheStore cacheStore;
        private readonly Options options;

        public WebSectionPackRetriever(OfficeCacheStore cacheStore, Options? options = null)
        {
            this.cacheStore = cacheStore ?? throw new ArgumentNullException(nameof(cacheStore));
            this.options = options ?? new Options();
        }

        public async Task<WebSectionPack> GetOrBuildSectionPackAsync(
            string officeKey,
            string sectionKey,
            string queryText)
        {
            if (string.IsNullOrWhiteSpace(officeKey))
                throw new ArgumentException("Office key is required.", nameof(officeKey));

            if (string.IsNullOrWhiteSpace(sectionKey))
                throw new ArgumentException("Section key is required.", nameof(sectionKey));

            WebSectionPack? cached = await cacheStore.LoadSectionPackAsync(officeKey, sectionKey);
            if (cached != null && string.Equals(cached.QueryText, queryText ?? string.Empty, StringComparison.Ordinal))
                return cached;

            WebChunkCorpus? chunkCorpus = await cacheStore.LoadChunkCorpusAsync(officeKey);
            if (chunkCorpus == null)
            {
                return new WebSectionPack(
                    officeKey: officeKey,
                    sectionKey: sectionKey,
                    builtAtUtc: DateTime.UtcNow,
                    queryText: queryText,
                    items: Array.Empty<WebSectionPackItem>());
            }

            IReadOnlyList<WebSectionPackItem> items = BuildSectionPackItems(chunkCorpus, queryText);

            var sectionPack = new WebSectionPack(
                officeKey: officeKey,
                sectionKey: sectionKey,
                builtAtUtc: DateTime.UtcNow,
                queryText: queryText,
                items: items);

            await cacheStore.SaveSectionPackAsync(sectionPack);

            return sectionPack;
        }

        private IReadOnlyList<WebSectionPackItem> BuildSectionPackItems(WebChunkCorpus chunkCorpus, string queryText)
        {
            HashSet<string> queryTokens = TokenizeQueryToSet(queryText);

            var candidates = new List<Candidate>();

            foreach (WebChunkPage page in chunkCorpus.Pages)
            {
                foreach (WebChunk chunk in page.Chunks)
                {
                    double score = ScoreChunkBm25(chunkCorpus, page, chunk, queryTokens);
                    if (score <= 0)
                        continue;

                    candidates.Add(new Candidate(
                        url: page.Url,
                        chunkId: chunk.ChunkId,
                        text: chunk.Text,
                        score: score));
                }
            }

            IReadOnlyList<Candidate> sorted = candidates
                .OrderByDescending(candidate => candidate.Score)
                .ThenBy(candidate => candidate.Url, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var selected = new List<WebSectionPackItem>(capacity: options.MaximumChunks);
            var perUrlCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            int totalCharacters = 0;

            foreach (Candidate candidate in sorted)
            {
                if (selected.Count >= options.MaximumChunks)
                    break;

                if (!perUrlCounts.TryGetValue(candidate.Url, out int urlCount))
                    urlCount = 0;

                if (urlCount >= options.MaximumChunksPerUrl)
                    continue;

                int remaining = options.MaximumTotalCharacters - totalCharacters;
                if (remaining <= 0)
                    break;

                string text = candidate.Text ?? string.Empty;
                if (text.Length > remaining)
                    text = text.Substring(0, remaining);

                selected.Add(new WebSectionPackItem(
                    chunkId: candidate.ChunkId,
                    url: candidate.Url,
                    score: candidate.Score,
                    text: text));

                perUrlCounts[candidate.Url] = urlCount + 1;
                totalCharacters += text.Length;
            }

            return selected;
        }

        private double ScoreChunkBm25(WebChunkCorpus chunkCorpus, WebChunkPage page, WebChunk chunk, HashSet<string> queryTokens)
        {
            if (queryTokens.Count == 0)
                return 0;

            if (chunk.TotalTokenCount <= 0)
                return 0;

            double averageDocumentLength = chunkCorpus.AverageChunkTokenCount;
            if (averageDocumentLength <= 0)
                return 0;

            double k1 = options.Bm25K1;
            double b = options.Bm25B;

            double documentLength = chunk.TotalTokenCount;

            double score = 0;

            foreach (string queryToken in queryTokens)
            {
                if (!chunk.TokenCounts.TryGetValue(queryToken, out int termFrequency))
                    continue;

                if (!chunkCorpus.InverseDocumentFrequencyByToken.TryGetValue(queryToken, out double inverseDocumentFrequency))
                    continue;

                double tf = termFrequency;

                double normalization = k1 * (1.0 - b + (b * (documentLength / averageDocumentLength)));
                double numerator = tf * (k1 + 1.0);
                double denominator = tf + normalization;

                score += inverseDocumentFrequency * (numerator / denominator);
            }

            return score;
        }

        private HashSet<string> TokenizeQueryToSet(string text)
        {
            var tokens = new HashSet<string>(StringComparer.Ordinal);

            IReadOnlyList<string> rawTokens = WebTextTokenizer.TokenizeForQuery(text, options.MinimumTokenLength);

            for (int index = 0; index < rawTokens.Count; index++)
                tokens.Add(rawTokens[index]);

            return tokens;
        }

        private sealed class Candidate
        {
            public string Url { get; }
            public string ChunkId { get; }
            public string Text { get; }
            public double Score { get; }

            public Candidate(string url, string chunkId, string text, double score)
            {
                Url = url ?? string.Empty;
                ChunkId = chunkId ?? string.Empty;
                Text = text ?? string.Empty;
                Score = score;
            }
        }
    }
}
