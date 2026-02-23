using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Assistant.Dependencies.Context.Web.Storage
{
    public sealed class WebChunkCorpus
    {
        public string OfficeKey { get; }
        public DateTime BuiltAtUtc { get; }

        public int TotalChunks { get; }
        public double AverageChunkTokenCount { get; }
        public IReadOnlyDictionary<string, double> InverseDocumentFrequencyByToken { get; }

        public IReadOnlyList<WebChunkPage> Pages { get; }

        [JsonConstructor]
        public WebChunkCorpus(
            string officeKey,
            DateTime builtAtUtc,
            int totalChunks = 0,
            double averageChunkTokenCount = 0,
            IReadOnlyDictionary<string, double>? inverseDocumentFrequencyByToken = null,
            IReadOnlyList<WebChunkPage>? pages = null)
        {
            if (string.IsNullOrWhiteSpace(officeKey))
                throw new ArgumentException("Office key is required.", nameof(officeKey));

            OfficeKey = officeKey;
            BuiltAtUtc = builtAtUtc;

            TotalChunks = totalChunks;
            AverageChunkTokenCount = averageChunkTokenCount;

            InverseDocumentFrequencyByToken =
                inverseDocumentFrequencyByToken ?? new Dictionary<string, double>(StringComparer.Ordinal);

            Pages = pages ?? Array.Empty<WebChunkPage>();
        }
    }
}