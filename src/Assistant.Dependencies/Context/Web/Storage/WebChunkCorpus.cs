using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Assistant.Dependencies.Context.Web.Storage
{
    public sealed class WebChunkCorpus
    {
        public string OfficeKey { get; }
        public DateTime BuiltAtUtc { get; }

        public IReadOnlyList<WebChunkPage> Pages { get; }

        [JsonConstructor]
        public WebChunkCorpus(
            string officeKey,
            DateTime builtAtUtc,
            IReadOnlyList<WebChunkPage> pages)
        {
            OfficeKey = officeKey;
            BuiltAtUtc = builtAtUtc;
            Pages = pages;
        }
    }
}
