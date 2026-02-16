using System;

namespace Assistant.Dependencies.Context.Web.Crawling
{
    public sealed class CrawlPage
    {
        public string Url { get; }
        public DateTime RetrievedAtUtc { get; }
        public string ExtractedText { get; }

        public CrawlPage(
            string url,
            DateTime retrievedAtUtc,
            string extractedText)
        {
            Url = url;
            RetrievedAtUtc = retrievedAtUtc;
            ExtractedText = extractedText;
        }
    }
}
