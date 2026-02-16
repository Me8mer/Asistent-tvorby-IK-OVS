using System;
using System.Collections.Generic;
using System.Text;
using Assistant.Dependencies.Context.Web.Storage;

namespace Assistant.Dependencies.Context.Web.Processing
{
    public sealed class WebContextPackPlaceholderBuilder
    {
        public WebContextPack BuildFromCorpus(WebCorpus corpus)
        {
            if (corpus == null)
                throw new ArgumentNullException(nameof(corpus));

            var snippets = new List<WebContextSnippet>(capacity: corpus.Pages.Count);

            foreach (WebCorpusPage page in corpus.Pages)
            {
                if (page == null)
                    continue;

                string referenceId = BuildReferenceId(
                    corpus.OfficeKey,
                    page.Url,
                    page.ContentHash);

                var snippet = new WebContextSnippet(
                    intent: "RawPage",
                    providerKind: "WEB",
                    referenceId: referenceId,
                    text: page.ExtractedText);

                snippets.Add(snippet);
            }

            return new WebContextPack(
                officeKey: corpus.OfficeKey,
                builtAtUtc: DateTime.UtcNow,
                snippets: snippets);
        }

        private static string BuildReferenceId(string officeKey, string url, string contentHash)
        {
            string safeUrlPart = ToSafeToken(url);
            string safeHashPart = string.IsNullOrWhiteSpace(contentHash) ? "nohash" : contentHash;

            return $"WEB|{officeKey}|RawPage|{safeUrlPart}|{safeHashPart}";
        }

        private static string ToSafeToken(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "empty";

            var builder = new StringBuilder(value.Length);

            foreach (char character in value)
            {
                if (char.IsLetterOrDigit(character))
                {
                    builder.Append(char.ToLowerInvariant(character));
                    continue;
                }

                if (builder.Length == 0 || builder[builder.Length - 1] == '_')
                    continue;

                builder.Append('_');
            }

            string token = builder.ToString().Trim('_');
            return string.IsNullOrWhiteSpace(token) ? "empty" : token;
        }
    }
}
