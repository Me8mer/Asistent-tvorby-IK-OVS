using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Assistant.Dependencies.Context.Web.Crawling.Apify
{
    public sealed class ApifyManagedCrawler : IManagedCrawler
    {
        private readonly HttpClient httpClient;
        private readonly ApifyCrawlerOptions options;

        public ApifyManagedCrawler(HttpClient httpClient, ApifyCrawlerOptions options)
        {
            this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            this.options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public async Task<CrawlResult> RunAsync(string officeIdentifier, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(officeIdentifier))
            {
                throw new ArgumentException("Office identifier must be provided.", nameof(officeIdentifier));
            }

            if (!Uri.TryCreate(officeIdentifier, UriKind.Absolute, out Uri? seedUri))
            {
                throw new ArgumentException("Office identifier must be a valid absolute URL for now.", nameof(officeIdentifier));
            }

            string seedUrl = officeIdentifier;
            string domainIncludeGlob = $"{seedUri.Scheme}://{seedUri.Host}/**";

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(options.MaxWaitForFinish);

            EnsureAuthHeader();

            string runId = await StartRunAsync(seedUrl, domainIncludeGlob, timeoutCts.Token).ConfigureAwait(false);
            string datasetId = await WaitForRunAndGetDatasetIdAsync(runId, timeoutCts.Token).ConfigureAwait(false);

            List<CrawlPage> pages = await FetchDatasetPagesAsync(datasetId, timeoutCts.Token).ConfigureAwait(false);
            return new CrawlResult(officeIdentifier, pages);
        }

        private void EnsureAuthHeader()
        {
            if (string.IsNullOrWhiteSpace(options.ApiToken))
            {
                throw new InvalidOperationException("Apify API token is not configured.");
            }

            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiToken);
        }

        private async Task<string> StartRunAsync(string seedUrl, string domainIncludeGlob, CancellationToken cancellationToken)
        {
            object actorInput = BuildActorInput(seedUrl, domainIncludeGlob);

            string startRunUrl =
                $"https://api.apify.com/v2/acts/{Uri.EscapeDataString(options.ActorId)}/runs?waitForFinish=0";

            string requestBody = JsonSerializer.Serialize(actorInput);
            using var requestContent = new StringContent(requestBody, Encoding.UTF8, "application/json");

            using HttpResponseMessage response = await httpClient.PostAsync(startRunUrl, requestContent, cancellationToken)
                .ConfigureAwait(false);

            string responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            using JsonDocument document = JsonDocument.Parse(responseBody);
            JsonElement runData = document.RootElement.GetProperty("data");

            string? runId = runData.GetProperty("id").GetString();
            if (string.IsNullOrWhiteSpace(runId))
            {
                throw new InvalidOperationException("Apify response did not contain run id.");
            }

            return runId;
        }

        private async Task<string> WaitForRunAndGetDatasetIdAsync(string runId, CancellationToken cancellationToken)
        {
            DateTime startUtc = DateTime.UtcNow;

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string runStatusUrl = $"https://api.apify.com/v2/actor-runs/{runId}";
                using HttpResponseMessage response = await httpClient.GetAsync(runStatusUrl, cancellationToken).ConfigureAwait(false);

                string responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                using JsonDocument document = JsonDocument.Parse(responseBody);
                JsonElement runData = document.RootElement.GetProperty("data");

                string status = runData.GetProperty("status").GetString() ?? "";

                if (status == "SUCCEEDED")
                {
                    string? datasetId = runData.TryGetProperty("defaultDatasetId", out JsonElement datasetIdValue)
                        ? datasetIdValue.GetString()
                        : null;

                    if (string.IsNullOrWhiteSpace(datasetId))
                    {
                        throw new InvalidOperationException("Apify run succeeded but defaultDatasetId was missing.");
                    }

                    return datasetId;
                }

                if (status == "FAILED" || status == "TIMED-OUT" || status == "ABORTED")
                {
                    throw new InvalidOperationException($"Apify run did not succeed. Final status: {status}");
                }

                if (DateTime.UtcNow - startUtc > options.MaxWaitForFinish)
                {
                    throw new TimeoutException("Apify run did not finish within the configured time limit.");
                }

                await Task.Delay(options.PollInterval, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task<List<CrawlPage>> FetchDatasetPagesAsync(string datasetId, CancellationToken cancellationToken)
        {
            // Request only fields we need.
            string itemsUrl =
                $"https://api.apify.com/v2/datasets/{datasetId}/items" +
                $"?format=json&clean=true&fields=url,{options.OutputTextFieldName}&limit={options.MaxCrawlPages}";

            using HttpResponseMessage response = await httpClient.GetAsync(itemsUrl, cancellationToken).ConfigureAwait(false);
            string responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            using JsonDocument document = JsonDocument.Parse(responseBody);

            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                throw new InvalidOperationException("Apify dataset items response was not a JSON array.");
            }

            DateTime retrievedAtUtc = DateTime.UtcNow;
            var pages = new List<CrawlPage>();

            foreach (JsonElement item in document.RootElement.EnumerateArray())
            {
                string? url = TryGetString(item, "url");
                if (string.IsNullOrWhiteSpace(url))
                {
                    continue;
                }

                string? extractedText = TryGetString(item, options.OutputTextFieldName);
                if (string.IsNullOrWhiteSpace(extractedText))
                {
                    continue;
                }

                if (LooksLikeBrowserErrorPage(extractedText))
                {
                    continue;
                }

                pages.Add(new CrawlPage(url, retrievedAtUtc, extractedText));
            }

            return pages;
        }

        private object BuildActorInput(string seedUrl, string domainIncludeGlob)
        {
            string crawlerType = options.EnableJavaScriptRendering ? "playwright:adaptive" : "cheerio";

            var excludeUrlGlobs = new[]
            {
                "**/novinky/**",
                "**/aktuality/**",

                "**/fotogalerie/**",
                "**/galerie/**",

                "**/search**",
                "**?*query=*",
                "**?*s=*",

                "**/cookies**",
                "**/gdpr**",
                "**/privacy**",
                "**/pristupnost**"
            };

            var input = new Dictionary<string, object?>
            {
                ["startUrls"] = new[] { new Dictionary<string, string> { ["url"] = seedUrl } },

                ["crawlerType"] = crawlerType,

                ["maxCrawlDepth"] = options.MaxCrawlDepth,
                ["maxCrawlPages"] = options.MaxCrawlPages,

                ["useSitemaps"] = options.UseSitemaps,
                ["useLlmsTxt"] = options.UseLlmsTxt,

                ["removeCookieWarnings"] = options.RemoveCookieWarnings,
                ["blockMedia"] = options.BlockMedia,

                // Output: text
                ["saveMarkdown"] = false,
                ["saveHtmlAsFile"] = false,
                ["saveFiles"] = false,

                // Scope control
                ["includeUrlGlobs"] = new[] { domainIncludeGlob },
                ["excludeUrlGlobs"] = excludeUrlGlobs
            };

            if (options.EnableJavaScriptRendering)
            {
                input["dynamicContentWaitSecs"] = options.DynamicContentWaitSecs;
            }

            return input;
        }

        private static string? TryGetString(JsonElement element, string propertyName)
        {
            if (element.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (!element.TryGetProperty(propertyName, out JsonElement propertyValue))
            {
                return null;
            }

            if (propertyValue.ValueKind == JsonValueKind.String)
            {
                return propertyValue.GetString();
            }

            return propertyValue.ToString();
        }

        private static bool LooksLikeBrowserErrorPage(string extractedText)
        {
            string normalized = extractedText.ToLowerInvariant();

            return normalized.Contains("can't open this page") ||
                   normalized.Contains("error code:") ||
                   normalized.Contains("will not allow") ||
                   normalized.Contains("x-frame-options") ||
                   normalized.Contains("access denied") ||
                   normalized.Contains("forbidden") ||
                   normalized.Contains("captcha");
        }
    }
}
