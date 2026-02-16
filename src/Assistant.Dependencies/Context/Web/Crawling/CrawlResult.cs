using System.Collections.Generic;

namespace Assistant.Dependencies.Context.Web.Crawling
{
    public sealed class CrawlResult
    {
        public string OfficeKey { get; }
        public IReadOnlyList<CrawlPage> Pages { get; }

        public CrawlResult(
            string officeKey,
            IReadOnlyList<CrawlPage> pages)
        {
            OfficeKey = officeKey;
            Pages = pages;
        }
    }
}
