using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Assistant.Dependencies.Context.Web.Storage
{
    public sealed class WebChunkPage
    {
        public string Url { get; }
        public string PageHash { get; }

        public IReadOnlyList<WebChunk> Chunks { get; }

        [JsonConstructor]
        public WebChunkPage(
            string url,
            string pageHash,
            IReadOnlyList<WebChunk> chunks)
        {
            Url = url;
            PageHash = pageHash;
            Chunks = chunks;
        }
    }
}
