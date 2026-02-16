using System.Threading;
using System.Threading.Tasks;

namespace Assistant.Dependencies.Context.Web.Crawling
{
    public interface IManagedCrawler
    {
        Task<CrawlResult> RunAsync(
            string officeIdentifier,
            CancellationToken cancellationToken);
    }
}
