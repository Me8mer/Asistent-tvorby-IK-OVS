using System;
using System.Collections.Generic;
using System.IO;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Cz;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Util;

namespace Assistant.Dependencies.Context.Web.Processing
{
    internal static class LuceneCzechTokenizer
    {
        private static readonly LuceneVersion LuceneVersion = LuceneVersion.LUCENE_48;

        public static IReadOnlyList<string> Tokenize(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return Array.Empty<string>();

            var tokens = new List<string>();

            Analyzer analyzer = new CzechAnalyzer(LuceneVersion);

            using var reader = new StringReader(text);
            using TokenStream tokenStream = analyzer.GetTokenStream("field", reader);

            var termAttribute = tokenStream.AddAttribute<ICharTermAttribute>();

            tokenStream.Reset();

            while (tokenStream.IncrementToken())
            {
                tokens.Add(termAttribute.ToString());
            }

            tokenStream.End();

            return tokens;
        }
    }
}