using System;
using System.Text.Json.Serialization;

namespace Assistant.Dependencies.Context.Web.Storage
{
    public sealed class WebCorpusPage
    {
        public string Url { get; }
        public DateTime RetrievedAtUtc { get; }
        public string ContentHash { get; }
        public string ExtractedText { get; }

        [JsonConstructor]
        public WebCorpusPage(
            string url,
            DateTime retrievedAtUtc,
            string contentHash,
            string extractedText)
        {
            Url = url;
            RetrievedAtUtc = retrievedAtUtc;
            ContentHash = contentHash;
            ExtractedText = extractedText;
        }
    }
}
