using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assistant.Dependencies.Context.Web.Processing;
using Assistant.Dependencies.Context.Web.Storage;

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
            public int MinimumUrlPrefixMatchLength { get; init; } = 4;
            public double Bm25K1 { get; init; } = 1.2;
            public double Bm25B { get; init; } = 0.75;
            public double UrlDiversityAlpha { get; init; } = 0.3;
            public double UrlTokenMatchBoostPerToken { get; init; } = 0.14;
            public int UrlTokenMatchBoostCap { get; init; } = 5;
            public double UrlPrefixMatchBoostPerToken { get; init; } = 0.2;
            public int UrlPrefixMatchBoostCap { get; init; } = 5;
            public double QueryCoverageBoostFactor { get; init; } = 0.20;
            public IReadOnlyCollection<string> NegativeWords { get; init; } = DefaultNegativeWords;

            public double NegativeTokenPenaltyFactor { get; init; } = 0.9;
            public double MaximumOverflowRatio { get; init; } = 0.15;
            public bool EnableDebugLogging { get; init; } = false;
            public int DebugTopCandidatesToPrint { get; init; } = 8;

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
            {
                if (options.EnableDebugLogging)
                {
                    Console.WriteLine($"[Retriever Debug] Cache hit for office='{officeKey}', section='{sectionKey}', query='{queryText}'.");
                }

                return cached;
            }

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

            if (options.EnableDebugLogging)
            {
                Console.WriteLine($"[Retriever Debug] Saved section pack for office='{officeKey}', section='{sectionKey}', items={items.Count}.");
            }

            return sectionPack;
        }

        private IReadOnlyList<WebSectionPackItem> BuildSectionPackItems(WebChunkCorpus chunkCorpus, string queryText)
        {
            QueryProfile queryProfile = BuildQueryProfile(queryText);
            if (queryProfile.Bm25Tokens.Count == 0)
                return Array.Empty<WebSectionPackItem>();

            var candidates = new List<Candidate>();

            foreach (WebChunkPage page in chunkCorpus.Pages)
            {
                foreach (WebChunk chunk in page.Chunks)
                {
                    ScoreBreakdown breakdown = ScoreChunkBm25(chunkCorpus, page, chunk, queryProfile);
                    if (breakdown.FinalScore <= 0)
                        continue;

                    candidates.Add(new Candidate(
                        url: page.Url,
                        chunkId: chunk.ChunkId,
                        text: chunk.Text,
                        score: breakdown.FinalScore,
                        bm25Score: breakdown.Bm25Score,
                        scoreAfterNegativePenalty: breakdown.ScoreAfterNegativePenalty,
                        negativePenaltyApplied: breakdown.NegativePenaltyApplied,
                        matchedQueryTokenCount: breakdown.MatchedQueryTokenCount,
                        queryCoverageRatio: breakdown.QueryCoverageRatio,
                        exactUrlTokenMatches: breakdown.ExactUrlTokenMatches,
                        prefixUrlTokenMatches: breakdown.PrefixUrlTokenMatches,
                        exactUrlMatchedTokens: breakdown.ExactUrlMatchedTokens,
                        prefixUrlMatchedTokens: breakdown.PrefixUrlMatchedTokens,
                        urlBoostFactor: breakdown.UrlBoostFactor));
                }
            }

            if (options.EnableDebugLogging)
            {
                PrintCandidateDebugSummary(queryText, queryProfile, candidates);
            }

            var remainingCandidates = new List<Candidate>(candidates);
            var selected = new List<WebSectionPackItem>(capacity: options.MaximumChunks);
            var perUrlCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            int totalCharacters = 0;

            while (selected.Count < options.MaximumChunks && remainingCandidates.Count > 0)
            {
                Candidate? bestCandidate = null;
                double bestAdjustedScore = double.MinValue;

                for (int candidateIndex = 0; candidateIndex < remainingCandidates.Count; candidateIndex++)
                {
                    Candidate candidate = remainingCandidates[candidateIndex];

                    if (!perUrlCounts.TryGetValue(candidate.Url, out int urlCount))
                        urlCount = 0;

                    if (urlCount >= options.MaximumChunksPerUrl)
                        continue;

                    double adjustedScore = candidate.Score / (1.0 + options.UrlDiversityAlpha * urlCount);
                    if (adjustedScore > bestAdjustedScore)
                    {
                        bestAdjustedScore = adjustedScore;
                        bestCandidate = candidate;
                    }
                }

                if (bestCandidate == null)
                    break;

                string selectedText = bestCandidate.Text ?? string.Empty;
                int hardLimit = options.MaximumTotalCharacters;
                int softLimit = hardLimit + (int)(hardLimit * options.MaximumOverflowRatio);

                if (totalCharacters >= softLimit)
                    break;

                int newTotal = totalCharacters + selectedText.Length;
                if (newTotal > softLimit)
                {
                    int remainingCharacters = hardLimit - totalCharacters;
                    if (remainingCharacters <= 0)
                        break;

                    selectedText = TrimToSentenceBoundary(selectedText, remainingCharacters);
                }

                selected.Add(new WebSectionPackItem(
                    chunkId: bestCandidate.ChunkId,
                    url: bestCandidate.Url,
                    score: bestCandidate.Score,
                    text: selectedText));

                if (!perUrlCounts.TryGetValue(bestCandidate.Url, out int existingCount))
                    existingCount = 0;

                perUrlCounts[bestCandidate.Url] = existingCount + 1;
                totalCharacters += selectedText.Length;
                remainingCandidates.Remove(bestCandidate);
            }

            if (options.EnableDebugLogging)
            {
                Console.WriteLine($"[Retriever Debug] Selected {selected.Count} chunks (of {candidates.Count} candidates).\n");
            }

            return selected;
        }

        private ScoreBreakdown ScoreChunkBm25(
            WebChunkCorpus chunkCorpus,
            WebChunkPage page,
            WebChunk chunk,
            QueryProfile queryProfile)
        {
            if (queryProfile.Bm25Tokens.Count == 0)
                return ScoreBreakdown.Zero;

            if (chunk.TotalTokenCount <= 0)
                return ScoreBreakdown.Zero;

            double averageDocumentLength = chunkCorpus.AverageChunkTokenCount;
            if (averageDocumentLength <= 0)
                return ScoreBreakdown.Zero;

            double bm25Score = 0;
            int matchedQueryTokenCount = 0;
            double documentLength = chunk.TotalTokenCount;

            foreach (string queryToken in queryProfile.Bm25Tokens)
            {
                if (!chunk.TokenCounts.TryGetValue(queryToken, out int termFrequency))
                    continue;

                if (!chunkCorpus.InverseDocumentFrequencyByToken.TryGetValue(queryToken, out double inverseDocumentFrequency))
                    continue;

                matchedQueryTokenCount++;

                double normalization = options.Bm25K1 * (1.0 - options.Bm25B + (options.Bm25B * (documentLength / averageDocumentLength)));
                double numerator = termFrequency * (options.Bm25K1 + 1.0);
                double denominator = termFrequency + normalization;

                bm25Score += inverseDocumentFrequency * (numerator / denominator);
            }

            if (bm25Score <= 0)
                return ScoreBreakdown.Zero;

            double scoreAfterNegativePenalty = bm25Score;
            bool negativePenaltyApplied = false;

            if (negativeTokens.Count > 0)
            {
                foreach (string negativeToken in negativeTokens)
                {
                    if (chunk.TokenCounts.TryGetValue(negativeToken, out int count) && count > 0)
                    {
                        negativePenaltyApplied = true;
                        scoreAfterNegativePenalty *= Math.Pow(options.NegativeTokenPenaltyFactor, count);
                    }
                }
            }

            double queryCoverageRatio = (double)matchedQueryTokenCount / queryProfile.Bm25Tokens.Count;
            double coverageBoostFactor = 1.0 + (queryCoverageRatio * options.QueryCoverageBoostFactor);

            UrlMatchBreakdown urlMatchBreakdown = CountUrlMatches(page.Url, queryProfile.UrlTokens);

            int effectiveExactMatches = Math.Min(urlMatchBreakdown.ExactMatchCount, options.UrlTokenMatchBoostCap);
            int effectivePrefixMatches = Math.Min(urlMatchBreakdown.PrefixMatchCount, options.UrlPrefixMatchBoostCap);

            double urlBoostFactor = 1.0
                + (effectiveExactMatches * options.UrlTokenMatchBoostPerToken)
                + (effectivePrefixMatches * options.UrlPrefixMatchBoostPerToken);

            double finalScore = scoreAfterNegativePenalty * coverageBoostFactor * urlBoostFactor;

            return new ScoreBreakdown(
                bm25Score: bm25Score,
                scoreAfterNegativePenalty: scoreAfterNegativePenalty,
                negativePenaltyApplied: negativePenaltyApplied,
                matchedQueryTokenCount: matchedQueryTokenCount,
                queryCoverageRatio: queryCoverageRatio,
                exactUrlTokenMatches: urlMatchBreakdown.ExactMatchCount,
                prefixUrlTokenMatches: urlMatchBreakdown.PrefixMatchCount,
                exactUrlMatchedTokens: urlMatchBreakdown.ExactMatchedTokens,
                prefixUrlMatchedTokens: urlMatchBreakdown.PrefixMatchedTokens,
                urlBoostFactor: urlBoostFactor,
                finalScore: finalScore);
        }

        private UrlMatchBreakdown CountUrlMatches(string? url, HashSet<string> queryUrlTokens)
        {
            if (string.IsNullOrWhiteSpace(url) || queryUrlTokens.Count == 0)
                return UrlMatchBreakdown.Zero;

            IReadOnlyList<string> urlTokens = TokenizeUrlPath(url);
            if (urlTokens.Count == 0)
                return UrlMatchBreakdown.Zero;

            var exactMatchedTokens = new List<string>();
            var prefixMatchedTokens = new List<string>();
            var exactMatchedTokenSet = new HashSet<string>(StringComparer.Ordinal);

            foreach (string queryUrlToken in queryUrlTokens)
            {
                if (urlTokens.Contains(queryUrlToken, StringComparer.Ordinal))
                {
                    exactMatchedTokenSet.Add(queryUrlToken);
                    exactMatchedTokens.Add(queryUrlToken);
                }
            }

            foreach (string queryUrlToken in queryUrlTokens)
            {
                if (queryUrlToken.Length < options.MinimumUrlPrefixMatchLength)
                    continue;

                if (exactMatchedTokenSet.Contains(queryUrlToken))
                    continue;

                string? matchedUrlToken = null;

                foreach (string urlToken in urlTokens)
                {
                    if (urlToken.Length < options.MinimumUrlPrefixMatchLength)
                        continue;

                    if (urlToken.StartsWith(queryUrlToken, StringComparison.Ordinal) ||
                        queryUrlToken.StartsWith(urlToken, StringComparison.Ordinal))
                    {
                        matchedUrlToken = urlToken;
                        break;
                    }
                }

                if (matchedUrlToken == null)
                    continue;

                prefixMatchedTokens.Add($"{queryUrlToken}->{matchedUrlToken}");
            }

            return new UrlMatchBreakdown(
                exactMatchCount: exactMatchedTokens.Count,
                prefixMatchCount: prefixMatchedTokens.Count,
                exactMatchedTokens: exactMatchedTokens,
                prefixMatchedTokens: prefixMatchedTokens);
        }

        private QueryProfile BuildQueryProfile(string queryText)
        {
            var bm25Tokens = new HashSet<string>(StringComparer.Ordinal);
            IReadOnlyList<string> rawBm25Tokens = LuceneCzechTokenizer
                .Tokenize(queryText)
                .Where(token => token.Length >= options.MinimumTokenLength)
                .ToList();

            foreach (string rawBm25Token in rawBm25Tokens)
                bm25Tokens.Add(rawBm25Token);

            string normalizedQueryTextForUrl = NormalizeTextForUrlMatching(queryText);
            var urlTokens = new HashSet<string>(StringComparer.Ordinal);
            IReadOnlyList<string> rawUrlTokens = LuceneCzechTokenizer
                .Tokenize(normalizedQueryTextForUrl)
                .Where(token => token.Length >= options.MinimumTokenLength)
                .ToList();

            foreach (string rawUrlToken in rawUrlTokens)
                urlTokens.Add(rawUrlToken);

            return new QueryProfile(bm25Tokens, urlTokens);
        }

        private static IReadOnlyList<string> TokenizeUrlPath(string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return Array.Empty<string>();

            string normalizedPathText = ExtractNormalizedUrlPathText(url);
            if (string.IsNullOrWhiteSpace(normalizedPathText))
                return Array.Empty<string>();

            return LuceneCzechTokenizer
                .Tokenize(normalizedPathText)
                .Where(token => token.Length >= 2)
                .Distinct(StringComparer.Ordinal)
                .ToList();
        }

        private static string ExtractNormalizedUrlPathText(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? parsedUri))
                return NormalizeTextForUrlMatching(url);

            string path = Uri.UnescapeDataString(parsedUri.AbsolutePath ?? string.Empty);
            if (string.IsNullOrWhiteSpace(path))
                return string.Empty;

            return NormalizeTextForUrlMatching(path);
        }

        private static string NormalizeTextForUrlMatching(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            string lowered = text.ToLowerInvariant();
            string decomposed = lowered.Normalize(NormalizationForm.FormD);

            var normalizedCharacters = new char[decomposed.Length];
            int normalizedLength = 0;
            bool previousWasSpace = true;

            for (int characterIndex = 0; characterIndex < decomposed.Length; characterIndex++)
            {
                char currentCharacter = decomposed[characterIndex];
                UnicodeCategory unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(currentCharacter);

                if (unicodeCategory == UnicodeCategory.NonSpacingMark)
                    continue;

                if (char.IsLetterOrDigit(currentCharacter))
                {
                    normalizedCharacters[normalizedLength++] = currentCharacter;
                    previousWasSpace = false;
                    continue;
                }

                if (!previousWasSpace)
                {
                    normalizedCharacters[normalizedLength++] = ' ';
                    previousWasSpace = true;
                }
            }

            if (normalizedLength > 0 && normalizedCharacters[normalizedLength - 1] == ' ')
                normalizedLength--;

            return new string(normalizedCharacters, 0, normalizedLength);
        }

        private void PrintCandidateDebugSummary(
            string queryText,
            QueryProfile queryProfile,
            List<Candidate> candidates)
        {
            int topCount = Math.Max(0, options.DebugTopCandidatesToPrint);

            Console.WriteLine("[Retriever Debug] -------------------------------");
            Console.WriteLine($"[Retriever Debug] Query: {queryText}");
            Console.WriteLine($"[Retriever Debug] BM25 query tokens ({queryProfile.Bm25Tokens.Count}): {string.Join(", ", queryProfile.Bm25Tokens.OrderBy(token => token, StringComparer.Ordinal))}");
            Console.WriteLine($"[Retriever Debug] URL query tokens ({queryProfile.UrlTokens.Count}): {string.Join(", ", queryProfile.UrlTokens.OrderBy(token => token, StringComparer.Ordinal))}");
            Console.WriteLine($"[Retriever Debug] Candidates: {candidates.Count}");

            if (topCount == 0 || candidates.Count == 0)
            {
                Console.WriteLine("[Retriever Debug] --------------------------------\n");
                return;
            }

            List<Candidate> topCandidates = candidates
                .OrderByDescending(candidate => candidate.Score)
                .Take(topCount)
                .ToList();

            for (int candidateIndex = 0; candidateIndex < topCandidates.Count; candidateIndex++)
            {
                Candidate candidate = topCandidates[candidateIndex];
                Console.WriteLine($"[Retriever Debug] #{candidateIndex + 1}: score={candidate.Score:F4}, bm25={candidate.Bm25Score:F4}, afterPenalty={candidate.ScoreAfterNegativePenalty:F4}, matchedTokens={candidate.MatchedQueryTokenCount}, coverage={candidate.QueryCoverageRatio:F3}, exactUrlMatches={candidate.ExactUrlTokenMatches}, prefixUrlMatches={candidate.PrefixUrlTokenMatches}, urlBoost={candidate.UrlBoostFactor:F3}, negativePenalty={candidate.NegativePenaltyApplied}");
                Console.WriteLine($"[Retriever Debug]     url={candidate.Url}");
                Console.WriteLine($"[Retriever Debug]     chunk={candidate.ChunkId}");
                Console.WriteLine($"[Retriever Debug]     exactUrlMatchedTokens={FormatTokenList(candidate.ExactUrlMatchedTokens)}");
                Console.WriteLine($"[Retriever Debug]     prefixUrlMatchedTokens={FormatTokenList(candidate.PrefixUrlMatchedTokens)}");
            }

            Console.WriteLine("[Retriever Debug] --------------------------------\n");
        }

        private static string FormatTokenList(IReadOnlyList<string> tokens)
        {
            return tokens.Count == 0
                ? "<none>"
                : string.Join(", ", tokens);
        }

        private sealed class Candidate
        {
            public string Url { get; }
            public string ChunkId { get; }
            public string Text { get; }
            public double Score { get; }
            public double Bm25Score { get; }
            public double ScoreAfterNegativePenalty { get; }
            public bool NegativePenaltyApplied { get; }
            public int MatchedQueryTokenCount { get; }
            public double QueryCoverageRatio { get; }
            public int ExactUrlTokenMatches { get; }
            public int PrefixUrlTokenMatches { get; }
            public IReadOnlyList<string> ExactUrlMatchedTokens { get; }
            public IReadOnlyList<string> PrefixUrlMatchedTokens { get; }
            public double UrlBoostFactor { get; }

            public Candidate(
                string url,
                string chunkId,
                string text,
                double score,
                double bm25Score,
                double scoreAfterNegativePenalty,
                bool negativePenaltyApplied,
                int matchedQueryTokenCount,
                double queryCoverageRatio,
                int exactUrlTokenMatches,
                int prefixUrlTokenMatches,
                IReadOnlyList<string> exactUrlMatchedTokens,
                IReadOnlyList<string> prefixUrlMatchedTokens,
                double urlBoostFactor)
            {
                Url = url ?? string.Empty;
                ChunkId = chunkId ?? string.Empty;
                Text = text ?? string.Empty;
                Score = score;
                Bm25Score = bm25Score;
                ScoreAfterNegativePenalty = scoreAfterNegativePenalty;
                NegativePenaltyApplied = negativePenaltyApplied;
                MatchedQueryTokenCount = matchedQueryTokenCount;
                QueryCoverageRatio = queryCoverageRatio;
                ExactUrlTokenMatches = exactUrlTokenMatches;
                PrefixUrlTokenMatches = prefixUrlTokenMatches;
                ExactUrlMatchedTokens = exactUrlMatchedTokens ?? Array.Empty<string>();
                PrefixUrlMatchedTokens = prefixUrlMatchedTokens ?? Array.Empty<string>();
                UrlBoostFactor = urlBoostFactor;
            }
        }

        private readonly struct QueryProfile
        {
            public HashSet<string> Bm25Tokens { get; }
            public HashSet<string> UrlTokens { get; }

            public QueryProfile(HashSet<string> bm25Tokens, HashSet<string> urlTokens)
            {
                Bm25Tokens = bm25Tokens ?? new HashSet<string>(StringComparer.Ordinal);
                UrlTokens = urlTokens ?? new HashSet<string>(StringComparer.Ordinal);
            }
        }

        private readonly struct UrlMatchBreakdown
        {
            public static UrlMatchBreakdown Zero => new(
                exactMatchCount: 0,
                prefixMatchCount: 0,
                exactMatchedTokens: Array.Empty<string>(),
                prefixMatchedTokens: Array.Empty<string>());

            public int ExactMatchCount { get; }
            public int PrefixMatchCount { get; }
            public IReadOnlyList<string> ExactMatchedTokens { get; }
            public IReadOnlyList<string> PrefixMatchedTokens { get; }

            public UrlMatchBreakdown(
                int exactMatchCount,
                int prefixMatchCount,
                IReadOnlyList<string> exactMatchedTokens,
                IReadOnlyList<string> prefixMatchedTokens)
            {
                ExactMatchCount = exactMatchCount;
                PrefixMatchCount = prefixMatchCount;
                ExactMatchedTokens = exactMatchedTokens ?? Array.Empty<string>();
                PrefixMatchedTokens = prefixMatchedTokens ?? Array.Empty<string>();
            }
        }

        private readonly struct ScoreBreakdown
        {
            public static ScoreBreakdown Zero => new(
                bm25Score: 0,
                scoreAfterNegativePenalty: 0,
                negativePenaltyApplied: false,
                matchedQueryTokenCount: 0,
                queryCoverageRatio: 0,
                exactUrlTokenMatches: 0,
                prefixUrlTokenMatches: 0,
                exactUrlMatchedTokens: Array.Empty<string>(),
                prefixUrlMatchedTokens: Array.Empty<string>(),
                urlBoostFactor: 1.0,
                finalScore: 0);

            public double Bm25Score { get; }
            public double ScoreAfterNegativePenalty { get; }
            public bool NegativePenaltyApplied { get; }
            public int MatchedQueryTokenCount { get; }
            public double QueryCoverageRatio { get; }
            public int ExactUrlTokenMatches { get; }
            public int PrefixUrlTokenMatches { get; }
            public IReadOnlyList<string> ExactUrlMatchedTokens { get; }
            public IReadOnlyList<string> PrefixUrlMatchedTokens { get; }
            public double UrlBoostFactor { get; }
            public double FinalScore { get; }

            public ScoreBreakdown(
                double bm25Score,
                double scoreAfterNegativePenalty,
                bool negativePenaltyApplied,
                int matchedQueryTokenCount,
                double queryCoverageRatio,
                int exactUrlTokenMatches,
                int prefixUrlTokenMatches,
                IReadOnlyList<string> exactUrlMatchedTokens,
                IReadOnlyList<string> prefixUrlMatchedTokens,
                double urlBoostFactor,
                double finalScore)
            {
                Bm25Score = bm25Score;
                ScoreAfterNegativePenalty = scoreAfterNegativePenalty;
                NegativePenaltyApplied = negativePenaltyApplied;
                MatchedQueryTokenCount = matchedQueryTokenCount;
                QueryCoverageRatio = queryCoverageRatio;
                ExactUrlTokenMatches = exactUrlTokenMatches;
                PrefixUrlTokenMatches = prefixUrlTokenMatches;
                ExactUrlMatchedTokens = exactUrlMatchedTokens ?? Array.Empty<string>();
                PrefixUrlMatchedTokens = prefixUrlMatchedTokens ?? Array.Empty<string>();
                UrlBoostFactor = urlBoostFactor;
                FinalScore = finalScore;
            }
        }

        private static string TrimToSentenceBoundary(string text, int maximumLength)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            if (text.Length <= maximumLength)
                return text;

            string candidateText = text.Substring(0, maximumLength);

            int lastPeriod = candidateText.LastIndexOf('.');
            int lastQuestion = candidateText.LastIndexOf('?');
            int lastExclamation = candidateText.LastIndexOf('!');
            int lastSentenceEnd = Math.Max(lastPeriod, Math.Max(lastQuestion, lastExclamation));

            if (lastSentenceEnd <= 0)
                return candidateText;

            return candidateText.Substring(0, lastSentenceEnd + 1);
        }

        private static HashSet<string> BuildNegativeTokenSet(IEnumerable<string> rawNegativeWords)
        {
            var tokenSet = new HashSet<string>(StringComparer.Ordinal);

            foreach (string rawNegativeWord in rawNegativeWords)
            {
                if (string.IsNullOrWhiteSpace(rawNegativeWord))
                    continue;

                IReadOnlyList<string> tokens = LuceneCzechTokenizer.Tokenize(rawNegativeWord);
                foreach (string token in tokens)
                    tokenSet.Add(token);
            }

            return tokenSet;
        }
    }
}
