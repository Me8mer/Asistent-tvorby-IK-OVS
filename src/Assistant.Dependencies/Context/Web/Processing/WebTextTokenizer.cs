using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Assistant.Dependencies.Context.Web.Processing
{
    internal static class WebTextTokenizer
    {
        public static IReadOnlyList<string> TokenizeForIndex(string? text, int minimumTokenLength)
        {
            return TokenizeInternal(text, removeDiacritics: true, minimumTokenLength);
        }

        public static IReadOnlyList<string> TokenizeForQuery(string? text, int minimumTokenLength)
        {
            return TokenizeInternal(text, removeDiacritics: true, minimumTokenLength);
        }

        private static IReadOnlyList<string> TokenizeInternal(string? text, bool removeDiacritics, int minimumTokenLength)
        {
            if (string.IsNullOrWhiteSpace(text))
                return Array.Empty<string>();

            string normalized = text.ToLowerInvariant();

            if (removeDiacritics)
                normalized = RemoveDiacritics(normalized);

            var tokens = new List<string>(Math.Min(256, normalized.Length / 4));
            var currentToken = new StringBuilder();

            for (int index = 0; index < normalized.Length; index++)
            {
                char character = normalized[index];

                bool isTokenCharacter =
                    char.IsLetterOrDigit(character) ||
                    character == '_';

                if (isTokenCharacter)
                {
                    currentToken.Append(character);
                    continue;
                }

                FlushTokenIfAny(tokens, currentToken, minimumTokenLength);
            }

            FlushTokenIfAny(tokens, currentToken, minimumTokenLength);

            return tokens;
        }

        private static void FlushTokenIfAny(List<string> tokens, StringBuilder currentToken, int minimumTokenLength)
        {
            if (currentToken.Length >= minimumTokenLength)
                tokens.Add(currentToken.ToString());

            currentToken.Clear();
        }

        private static string RemoveDiacritics(string text)
        {
            string decomposed = text.Normalize(NormalizationForm.FormD);
            var filtered = new StringBuilder(decomposed.Length);

            for (int index = 0; index < decomposed.Length; index++)
            {
                char character = decomposed[index];
                UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(character);

                if (category != UnicodeCategory.NonSpacingMark)
                    filtered.Append(character);
            }

            return filtered.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}