using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Assistant.Dependencies.Context.Web.Storage
{
    public sealed class WebChunk
    {
        public string ChunkId { get; }
        public int ChunkIndex { get; }
        public string Text { get; }

        public int TotalTokenCount { get; }
        public IReadOnlyDictionary<string, int> TokenCounts { get; }

        [JsonConstructor]
        public WebChunk(
            string chunkId,
            int chunkIndex,
            string text,
            int totalTokenCount = 0,
            IReadOnlyDictionary<string, int>? tokenCounts = null)
        {
            if (string.IsNullOrWhiteSpace(chunkId))
                throw new ArgumentException("Chunk id is required.", nameof(chunkId));

            ChunkId = chunkId;
            ChunkIndex = chunkIndex;
            Text = text ?? string.Empty;

            TotalTokenCount = totalTokenCount;
            TokenCounts = tokenCounts ?? new Dictionary<string, int>(StringComparer.Ordinal);
        }
    }
}