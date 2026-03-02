using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Assistant.Dependencies.Context.Web;
using Assistant.Dependencies.Context.Web.Crawling;
using Assistant.Dependencies.Context.Web.Crawling.Apify;
using Assistant.Dependencies.Context.Web.Processing;
using Assistant.Dependencies.Context.Web.Storage;
using Assistant.Dependencies.Context.Web.Retrieval;

using Microsoft.Extensions.DependencyInjection;

namespace Assistant.Tools.WebContextSmokeTest
{
    internal static class Program
    {
        public static async Task<int> Main(string[] args)
        {
            string officeUrl = args.Length > 0 ? args[0] : "https://example.com/";
            string apifyToken = Environment.GetEnvironmentVariable("APIFY_TOKEN") ?? string.Empty;

            if (string.IsNullOrWhiteSpace(apifyToken))
            {
                Console.Error.WriteLine("Missing APIFY_TOKEN environment variable.");
                return 2;
            }

            ServiceProvider serviceProvider = BuildServiceProvider(apifyToken);

            try
            {
                var provider = serviceProvider.GetRequiredService<OfficeWebContextProvider>();

                using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(15));

                Console.WriteLine($"Office URL: {officeUrl}");

                Console.WriteLine("Run 1: building or loading chunk corpus");
                var chunkCorpusRun1 = await provider.GetOrBuildChunkCorpusAsync(officeUrl, cancellationTokenSource.Token);

                Console.WriteLine("---- Retrieval smoke test ----");

                var retriever = serviceProvider.GetRequiredService<WebSectionPackRetriever>();

                string fakeSectionKey = "pusobnost";
                string fakeQuery = "působnost agenda kompetence odbor oddělení řeší";

                WebSectionPack pack = await retriever.GetOrBuildSectionPackAsync(
                    officeKey: NormalizeOfficeKeyForTest(officeUrl),
                    sectionKey: fakeSectionKey,
                    queryText: fakeQuery);

                Console.WriteLine($"Retrieved chunks: {pack.Items.Count}");

                foreach (var item in pack.Items)
                {
                    Console.WriteLine("----");
                    Console.WriteLine($"Score: {item.Score}");
                    Console.WriteLine($"URL: {item.Url}");
                    Console.WriteLine(item.Text.Length > 1000
                        ? item.Text.Substring(0, 1000) + "..."
                        : item.Text);
                }



                int pagesRun1 = chunkCorpusRun1.Pages.Count;
                int chunksRun1 = CountChunks(chunkCorpusRun1);

                Console.WriteLine($"Pages: {pagesRun1}");
                Console.WriteLine($"Chunks: {chunksRun1}");

                Console.WriteLine("Run 2: should load from cache");
                var chunkCorpusRun2 = await provider.GetOrBuildChunkCorpusAsync(officeUrl, cancellationTokenSource.Token);

                int pagesRun2 = chunkCorpusRun2.Pages.Count;
                int chunksRun2 = CountChunks(chunkCorpusRun2);

                Console.WriteLine($"Pages: {pagesRun2}");
                Console.WriteLine($"Chunks: {chunksRun2}");

                Console.WriteLine("Done.");
                return 0;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine(exception);
                return 1;
            }
            finally
            {
                await serviceProvider.DisposeAsync();
            }
        }

        private static ServiceProvider BuildServiceProvider(string apifyToken)
        {
            var services = new ServiceCollection();

            services.AddHttpClient();

            services.AddSingleton(new ApifyCrawlerOptions
            {
                ApiToken = apifyToken,
                ActorId = "apify~website-content-crawler",

                MaxCrawlDepth = 2,
                MaxCrawlPages = 120,

                UseSitemaps = false,
                UseLlmsTxt = false,

                EnableJavaScriptRendering = false,
                DynamicContentWaitSecs = 2,

                RemoveCookieWarnings = true,
                BlockMedia = true
            });

            services.AddTransient<IManagedCrawler>(serviceProvider =>
            {
                var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
                HttpClient httpClient = httpClientFactory.CreateClient();

                var options = serviceProvider.GetRequiredService<ApifyCrawlerOptions>();
                return new ApifyManagedCrawler(httpClient, options);
            });

            services.AddSingleton<OfficeCacheStore>(serviceProvider =>
            {
                string cacheRootDirectory = System.IO.Path.Combine(
                    Environment.CurrentDirectory,
                    ".office-cache");

                return new OfficeCacheStore(cacheRootDirectory);
            });

            services.AddSingleton<WebCorpusBuilder>();
            services.AddSingleton<WebChunkCorpusBuilder>();

            services.AddSingleton<OfficeWebContextProvider>();

            services.AddSingleton(new WebSectionPackRetriever.Options
            {
                MaximumChunks = 15,
                MaximumTotalCharacters = 5000,
                MaximumChunksPerUrl = 10,
                MinimumTokenLength = 3
            });

            services.AddSingleton<WebSectionPackRetriever>();

            return services.BuildServiceProvider();

        }

        private static int CountChunks(WebChunkCorpus chunkCorpus)
        {
            int totalChunks = 0;

            foreach (WebChunkPage page in chunkCorpus.Pages)
                totalChunks += page.Chunks.Count;

            return totalChunks;
        }


        private static string NormalizeOfficeKeyForTest(string officeIdentifier)
        {
            string trimmed = officeIdentifier.Trim();

            if (trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
                trimmed = trimmed.Substring("http://".Length);

            if (trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                trimmed = trimmed.Substring("https://".Length);

            int firstSlashIndex = trimmed.IndexOf('/');
            if (firstSlashIndex >= 0)
                trimmed = trimmed.Substring(0, firstSlashIndex);

            return trimmed.ToLowerInvariant().Replace('.', '-');
        }

    }
}
