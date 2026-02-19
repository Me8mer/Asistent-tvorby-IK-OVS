using System;
using System.Collections.Generic;
using System.Linq;
using Assistant.Dependencies.Context.Web.Storage;
using System.Globalization;
using System.Text;


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
            HashSet<string> queryTokens = TokenizeToSet(queryText);

            var candidates = new List<Candidate>();

            foreach (WebChunkPage page in chunkCorpus.Pages)
            {
                foreach (WebChunk chunk in page.Chunks)
                {
                    int score = ScoreChunk(chunk.Text, queryTokens);
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

        private int ScoreChunk(string chunkText, HashSet<string> queryTokens)
        {
            if (queryTokens.Count == 0)
                return 0;

            Dictionary<string, int> chunkTokenCounts = TokenizeToCounts(chunkText);

            int score = 0;

            foreach (string queryToken in queryTokens)
            {
                if (chunkTokenCounts.TryGetValue(queryToken, out int count))
                {
                    score += 2;
                    if (count >= 2)
                        score += 1;
                }
            }

            return score;
        }

        private HashSet<string> TokenizeToSet(string text)
        {
            var tokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(text))
                return tokens;

            foreach (string token in Tokenize(text))
                if (!string.IsNullOrWhiteSpace(token))
                    tokens.Add(token);

            return tokens;
        }

        private Dictionary<string, int> TokenizeToCounts(string text)
        {
            var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(text))
                return counts;

            foreach (string token in Tokenize(text))
            {
                if (!counts.TryGetValue(token, out int count))
                    count = 0;

                if (!string.IsNullOrWhiteSpace(token))
                    counts[token] = count + 1;
            }

            return counts;
        }

        private IEnumerable<string> Tokenize(string text)
        {
            int startIndex = -1;

            for (int index = 0; index < text.Length; index++)
            {
                char character = text[index];

                bool isTokenChar = char.IsLetterOrDigit(character);

                if (isTokenChar)
                {
                    if (startIndex == -1)
                        startIndex = index;

                    continue;
                }

                if (startIndex != -1)
                {
                    string? token = NormalizeToken(text.Substring(startIndex, index - startIndex));
                    if (!string.IsNullOrEmpty(token))
                        yield return token;

                    startIndex = -1;
                }
            }

            if (startIndex != -1)
            {
                string? token = NormalizeToken(text.Substring(startIndex));
                if (!string.IsNullOrEmpty(token))
                    yield return token;
            }
        }

        private string? NormalizeToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return null;

            string lowered = token.ToLowerInvariant().Trim();

            string withoutDiacritics = RemoveDiacritics(lowered);

            if (withoutDiacritics.Length < options.MinimumTokenLength)
                return null;

            return withoutDiacritics;
        }

        private static string RemoveDiacritics(string text)
        {
            string decomposed = text.Normalize(NormalizationForm.FormD);

            var builder = new StringBuilder(decomposed.Length);

            foreach (char character in decomposed)
            {
                UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(character);

                if (category != UnicodeCategory.NonSpacingMark &&
                    category != UnicodeCategory.SpacingCombiningMark &&
                    category != UnicodeCategory.EnclosingMark)
                {
                    builder.Append(character);
                }
            }

            return builder.ToString().Normalize(NormalizationForm.FormC);
        }


        private sealed class Candidate
        {
            public string Url { get; }
            public string ChunkId { get; }
            public string Text { get; }
            public int Score { get; }

            public Candidate(string url, string chunkId, string text, int score)
            {
                Url = url ?? string.Empty;
                ChunkId = chunkId ?? string.Empty;
                Text = text ?? string.Empty;
                Score = score;
            }
        }
    }
}
