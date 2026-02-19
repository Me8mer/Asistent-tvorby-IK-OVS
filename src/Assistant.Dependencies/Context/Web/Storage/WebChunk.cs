using System.Text.Json.Serialization;

namespace Assistant.Dependencies.Context.Web.Storage
{
    public sealed class WebChunk
    {
        public string ChunkId { get; }
        public int ChunkIndex { get; }
        public string Text { get; }

        [JsonConstructor]
        public WebChunk(
            string chunkId,
            int chunkIndex,
            string text
        )
        {
            ChunkId = chunkId;
            ChunkIndex = chunkIndex;
            Text = text;
        }
    }
}
    