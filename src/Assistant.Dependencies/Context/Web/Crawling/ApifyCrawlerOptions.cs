using System;

namespace Assistant.Dependencies.Context.Web.Crawling.Apify
{
    public sealed class ApifyCrawlerOptions
    {
        public string ApiToken { get; set; } = "";
        public string ActorId { get; set; } = "apify~website-content-crawler";

        public int MaxCrawlDepth { get; set; } = 2;
        public int MaxCrawlPages { get; set; } = 120;

        public bool UseSitemaps { get; set; } = false;
        public bool UseLlmsTxt { get; set; } = false;
        public string OutputTextFieldName { get; set; } = "text";

        // Baseline: JS rendering off
        public bool EnableJavaScriptRendering { get; set; } = false;
        public int DynamicContentWaitSecs { get; set; } = 2;

        public bool RemoveCookieWarnings { get; set; } = true;
        public bool BlockMedia { get; set; } = true;

        public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(3);
        public TimeSpan MaxWaitForFinish { get; set; } = TimeSpan.FromMinutes(10);
    }
}
