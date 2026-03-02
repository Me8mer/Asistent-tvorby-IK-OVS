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
            public double UrlDiversityAlpha { get; init; } = 0.3;
            public IReadOnlyCollection<string> NegativeWords { get; init; } = DefaultNegativeWords;

            public double NegativeTokenPenaltyFactor { get; init; } = 0.9;
            public double MaximumOverflowRatio { get; init; } = 0.15;

            private static readonly string[] DefaultNegativeWords =
            {
                "kariera",
                "zaměstnání",
                "novinky",
                "tisková",
                "akce",
                "galerie",
                "nabízíme",
                "plat",
                "příplatek",
                "osobnost",
                "Aktuality"
            };

            public Options()
            {
            }
        }

        private readonly OfficeCacheStore cacheStore;
        private readonly Options options;
        private readonly HashSet<string> negativeTokens;

        public WebSectionPackRetriever(OfficeCacheStore cacheStore, Options? options = null)
        {
            this.cacheStore = cacheStore ?? throw new ArgumentNullException(nameof(cacheStore));
            this.options = options ?? new Options();
            negativeTokens = BuildNegativeTokenSet(this.options.NegativeWords);
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

            var remainingCandidates = new List<Candidate>(candidates);

            var selected = new List<WebSectionPackItem>(capacity: options.MaximumChunks);
            var perUrlCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            int totalCharacters = 0;

            while (selected.Count < options.MaximumChunks && remainingCandidates.Count > 0)
            {
                Candidate? bestCandidate = null;
                double bestAdjustedScore = double.MinValue;

                for (int index = 0; index < remainingCandidates.Count; index++)
                {
                    Candidate candidate = remainingCandidates[index];

                    if (!perUrlCounts.TryGetValue(candidate.Url, out int urlCount))
                        urlCount = 0;

                    double adjustedScore =
                        candidate.Score / (1.0 + options.UrlDiversityAlpha * urlCount);

                    if (adjustedScore > bestAdjustedScore)
                    {
                        bestAdjustedScore = adjustedScore;
                        bestCandidate = candidate;
                    }
                }

                if (bestCandidate == null)
                    break;

                string text = bestCandidate.Text ?? string.Empty;

                int hardLimit = options.MaximumTotalCharacters;
                int softLimit = hardLimit + (int)(hardLimit * options.MaximumOverflowRatio);

                if (totalCharacters >= softLimit)
                    break;

                int newTotal = totalCharacters + text.Length;

                if (newTotal <= hardLimit)
                {
                    // Fully inside hard limit
                }
                else if (newTotal <= softLimit)
                {
                    // Allow full chunk even though exceeding hard limit
                }
                else
                {
                    // Would exceed soft limit -> try trimmed
                    int remaining = hardLimit - totalCharacters;

                    if (remaining <= 0)
                        break;

                    text = TrimToSentenceBoundary(text, remaining);
                }

                selected.Add(new WebSectionPackItem(
                    chunkId: bestCandidate.ChunkId,
                    url: bestCandidate.Url,
                    score: bestCandidate.Score,
                    text: text));

                if (!perUrlCounts.TryGetValue(bestCandidate.Url, out int existingCount))
                    existingCount = 0;

                perUrlCounts[bestCandidate.Url] = existingCount + 1;
                totalCharacters += text.Length;

                remainingCandidates.Remove(bestCandidate);
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

            // Apply penalty for negative tokens

            if (score <= 0)
                return 0;

            if (negativeTokens.Count > 0)
            {
                double penaltyFactor = options.NegativeTokenPenaltyFactor;

                foreach (string negativeToken in negativeTokens)
                {
                    if (chunk.TokenCounts.TryGetValue(negativeToken, out int count) && count > 0)
                    {
                        score *= Math.Pow(penaltyFactor, count);
                    }
                }
            }

            return score;
        }

        private HashSet<string> TokenizeQueryToSet(string text)
        {
            var tokens = new HashSet<string>(StringComparer.Ordinal);

            IReadOnlyList<string> rawTokens =
            LuceneCzechTokenizer
                .Tokenize(text)
                .Where(token => token.Length >= options.MinimumTokenLength)
                .ToList();

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


        private static string TrimToSentenceBoundary(string text, int maximumLength)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            if (text.Length <= maximumLength)
                return text;

            string candidate = text.Substring(0, maximumLength);

            int lastPeriod = candidate.LastIndexOf('.');
            int lastQuestion = candidate.LastIndexOf('?');
            int lastExclamation = candidate.LastIndexOf('!');

            int lastSentenceEnd = Math.Max(lastPeriod, Math.Max(lastQuestion, lastExclamation));

            if (lastSentenceEnd <= 0)
                return candidate;

            return candidate.Substring(0, lastSentenceEnd + 1);
        }

        private static HashSet<string> BuildNegativeTokenSet(IEnumerable<string> rawNegativeWords)
        {
            var set = new HashSet<string>(StringComparer.Ordinal);

            foreach (string word in rawNegativeWords)
            {
                if (string.IsNullOrWhiteSpace(word))
                    continue;

                IReadOnlyList<string> tokens = LuceneCzechTokenizer.Tokenize(word);

                for (int index = 0; index < tokens.Count; index++)
                {
                    set.Add(tokens[index]);
                }
            }

            return set;
        }

    }
}
