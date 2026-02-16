using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Assistant.Dependencies.Context.Web.Crawling;
using Assistant.Dependencies.Context.Web.Storage;

namespace Assistant.Dependencies.Context.Web.Processing
{
    public sealed class WebCorpusBuilder
    {
        public WebCorpus BuildCorpus(CrawlResult crawlResult)
        {
            if (crawlResult == null)
                throw new ArgumentNullException(nameof(crawlResult));

            var pages = new List<WebCorpusPage>(crawlResult.Pages.Count);

            foreach (CrawlPage crawlPage in crawlResult.Pages)
            {
                if (crawlPage == null)
                    continue;

                string extractedText = crawlPage.ExtractedText ?? string.Empty;
                string contentHash = ComputeSha256Hex(extractedText);

                var corpusPage = new WebCorpusPage(
                    url: crawlPage.Url ?? string.Empty,
                    retrievedAtUtc: crawlPage.RetrievedAtUtc,
                    contentHash: contentHash,
                    extractedText: extractedText);

                pages.Add(corpusPage);
            }

            return new WebCorpus(
                officeKey: crawlResult.OfficeKey,
                builtAtUtc: DateTime.UtcNow,
                pages: pages);
        }

        private static string ComputeSha256Hex(string text)
        {
            byte[] utf8Bytes = Encoding.UTF8.GetBytes(text ?? string.Empty);

            byte[] hashBytes = SHA256.HashData(utf8Bytes);

            var stringBuilder = new StringBuilder(capacity: hashBytes.Length * 2);
            foreach (byte hashByte in hashBytes)
            {
                stringBuilder.Append(hashByte.ToString("x2"));
            }

            return stringBuilder.ToString();
        }
    }
}
