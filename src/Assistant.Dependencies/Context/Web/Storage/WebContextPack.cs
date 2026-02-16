using System;
using System.Collections.Generic;
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
            OfficeKey = officeKey;
            BuiltAtUtc = builtAtUtc;
            Snippets = snippets;
        }
    }
}
