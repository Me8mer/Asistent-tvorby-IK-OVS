using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Assistant.Dependencies.Context.Web.Storage
{
    public sealed class WebSectionPack
    {
        public string OfficeKey { get; }
        public string SectionKey { get; }
        public DateTime BuiltAtUtc { get; }
        public string QueryText { get; }

        public IReadOnlyList<WebSectionPackItem> Items { get; }

        [JsonConstructor]
        public WebSectionPack(
            string officeKey,
            string sectionKey,
            DateTime builtAtUtc,
            string queryText,
            IReadOnlyList<WebSectionPackItem> items)
        {
            OfficeKey = officeKey;
            SectionKey = sectionKey;
            BuiltAtUtc = builtAtUtc;
            QueryText = queryText ?? string.Empty;
            Items = items ?? Array.Empty<WebSectionPackItem>();
        }
    }

    public sealed class WebSectionPackItem
    {
        public string ChunkId { get; }
        public string Url { get; }
        public double Score { get; }
        public string Text { get; }

        [JsonConstructor]
        public WebSectionPackItem(string chunkId, string url, double score, string text)
        {
            ChunkId = chunkId ?? string.Empty;
            Url = url ?? string.Empty;
            Score = score;
            Text = text ?? string.Empty;
        }
    }
}
