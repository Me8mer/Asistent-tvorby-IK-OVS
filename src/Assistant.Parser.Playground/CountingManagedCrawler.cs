// using System.Threading;
// using System.Threading.Tasks;
// using Assistant.Dependencies.Context.Web.Crawling;

// namespace Assistant.ContextSmokeTest
// {
//     public sealed class CountingManagedCrawler : IManagedCrawler
//     {
//         private readonly IManagedCrawler innerCrawler;

//         public int CallCount { get; private set; }

//         public CountingManagedCrawler(IManagedCrawler innerCrawler)
//         {
//             this.innerCrawler = innerCrawler;
//         }

//         public async Task<CrawlResult> RunAsync(string officeIdentifier, CancellationToken cancellationToken)
//         {
//             CallCount++;
//             return await innerCrawler.RunAsync(officeIdentifier, cancellationToken);
//         }
//     }
// }
