using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Assistant.Dependencies.Context.Web.Storage
{
    public sealed class WebCorpus
    {
        public string OfficeKey { get; }
        public DateTime BuiltAtUtc { get; }
        public IReadOnlyList<WebCorpusPage> Pages { get; }

        [JsonConstructor]
        public WebCorpus(
            string officeKey,
            DateTime builtAtUtc,
            IReadOnlyList<WebCorpusPage> pages)
        {
            OfficeKey = officeKey;
            BuiltAtUtc = builtAtUtc;
            Pages = pages;
        }
    }
}
