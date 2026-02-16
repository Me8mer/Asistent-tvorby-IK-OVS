using System;
using System.Threading;
using System.Threading.Tasks;

namespace Assistant.Dependencies.Context.Web.Crawling
{
    public sealed class StubManagedCrawler : IManagedCrawler
    {
        public Task<CrawlResult> RunAsync(
            string officeIdentifier,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string officeKey = "unused";

            var pages = new[]
            {
                new CrawlPage(
                    url: $"https://{officeKey}/kontakty",
                    retrievedAtUtc: DateTime.UtcNow,
                    extractedText:
                        "Kontakty\n" +
                        "Telefon: +420 123 456 789\n" +
                        "Email: podatelna@example.cz\n" +
                        "Datová schránka: abc123\n"),

                new CrawlPage(
                    url: $"https://{officeKey}/podani",
                    retrievedAtUtc: DateTime.UtcNow,
                    extractedText:
                        "Podání\n" +
                        "Podání lze učinit elektronicky přes datovou schránku nebo e-mailem se zaručeným podpisem.\n" +
                        "Úřední hodiny: Po-St 8:00-17:00\n"),

                new CrawlPage(
                    url: $"https://{officeKey}/poplatky",
                    retrievedAtUtc: DateTime.UtcNow,
                    extractedText:
                        "Správní poplatky\n" +
                        "Poplatek za ověření podpisu: 30 Kč.\n" +
                        "Poplatek za ověření listiny: 30 Kč.\n")
            };

            var result = new CrawlResult(
                officeKey: officeKey,
                pages: pages);

            return Task.FromResult(result);
        }
    }
}
