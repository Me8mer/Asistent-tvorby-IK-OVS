using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Assistant.ContextSmokeTest;
using Assistant.Dependencies.Context.Web;
using Assistant.Dependencies.Context.Web.Crawling;
using Assistant.Dependencies.Context.Web.Processing;
using Assistant.Dependencies.Context.Web.Storage;

internal static class Program
{
    private static async Task<int> Main()
    {
        string cacheRootPath = Path.Combine(Directory.GetCurrentDirectory(), ".office-cache-smoketest");
        string officeIdentifier = "example.cz";

        if (Directory.Exists(cacheRootPath))
            Directory.Delete(cacheRootPath, recursive: true);

        var cacheStore = new OfficeCacheStore(cacheRootPath);

        var innerCrawler = new StubManagedCrawler();
        var countingCrawler = new CountingManagedCrawler(innerCrawler);

        var corpusBuilder = new WebCorpusBuilder();
        var placeholderPackBuilder = new WebContextPackPlaceholderBuilder();

        var provider = new OfficeWebContextProvider(
            managedCrawler: countingCrawler,
            cacheStore: cacheStore,
            corpusBuilder: corpusBuilder,
            placeholderPackBuilder: placeholderPackBuilder);

        Console.WriteLine("Run 1");
        WebContextPack firstPack = await provider.GetOrBuildAsync(officeIdentifier, CancellationToken.None);

        Console.WriteLine($"Crawler calls after run 1: {countingCrawler.CallCount}");
        Console.WriteLine($"Snippets after run 1: {firstPack.Snippets.Count}");

        Console.WriteLine("Run 2");
        WebContextPack secondPack = await provider.GetOrBuildAsync(officeIdentifier, CancellationToken.None);

        Console.WriteLine($"Crawler calls after run 2: {countingCrawler.CallCount}");
        Console.WriteLine($"Snippets after run 2: {secondPack.Snippets.Count}");

        string normalizedOfficeKey = "example-cz";
        string officeDirectory = Path.Combine(cacheRootPath, normalizedOfficeKey);

        string corpusPath = Path.Combine(officeDirectory, "corpus.json");
        string contextPackPath = Path.Combine(officeDirectory, "context-pack.json");

        Console.WriteLine($"Corpus exists: {File.Exists(corpusPath)}");
        Console.WriteLine($"Context pack exists: {File.Exists(contextPackPath)}");

        if (countingCrawler.CallCount != 1)
        {
            Console.WriteLine("FAIL: crawler call count should be 1 after two runs.");
            return 1;
        }

        if (!File.Exists(corpusPath) || !File.Exists(contextPackPath))
        {
            Console.WriteLine("FAIL: expected cache files are missing.");
            return 1;
        }

        if (firstPack.Snippets.Count == 0)
        {
            Console.WriteLine("FAIL: context pack should not be empty after run 1.");
            return 1;
        }

        Console.WriteLine("PASS");
        return 0;
    }
}
