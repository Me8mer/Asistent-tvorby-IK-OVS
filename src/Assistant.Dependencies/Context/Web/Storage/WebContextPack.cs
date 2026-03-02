using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Assistant.Dependencies.Context.Web.Storage
{
    public sealed class WebContextPack
    {
        public string OfficeKey { get; }
        public DateTime BuiltAtUtc { get; }

        public IReadOnlyList<WebContextSnippet> Snippets { get; }

        [JsonConstructor]
        public WebContextPack(
            string officeKey,
            DateTime builtAtUtc,
            IReadOnlyList<WebContextSnippet> snippets)
        {
            OfficeKey = officeKey ?? string.Empty;
            BuiltAtUtc = builtAtUtc;
            Snippets = snippets ?? Array.Empty<WebContextSnippet>();
        }

        public (string Text, IReadOnlyList<string> References) GetContextText(string intent)
        {
            if (string.IsNullOrWhiteSpace(intent))
                return (string.Empty, Array.Empty<string>());

            List<WebContextSnippet> matching = Snippets
                .Where(s => string.Equals(s.Intent, intent, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (matching.Count == 0)
                return (string.Empty, Array.Empty<string>());

            string text = string.Join(
                "\n\n",
                matching.Select(s => s.Text)
                        .Where(t => !string.IsNullOrWhiteSpace(t)));

            IReadOnlyList<string> references = matching
                .Select(s => s.ReferenceId)
                .Where(r => !string.IsNullOrWhiteSpace(r))
                .ToArray();

            return (text, references);
        }

        public bool HasAnySnippetsForIntent(string intent)
        {
            if (string.IsNullOrWhiteSpace(intent))
                return false;

            return Snippets.Any(s =>
                string.Equals(s.Intent, intent, StringComparison.OrdinalIgnoreCase));
        }
    }
}