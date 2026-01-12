using System;

namespace Assistant.AI.Models
{
    public sealed class LlmRawResponse
    {
        public string OutputText { get; }
        public string? ProviderRequestId { get; }
        public string? ProviderModel { get; }
        public DateTimeOffset Timestamp { get; }

        public LlmRawResponse(string outputText, string? providerRequestId, string? providerModel, DateTimeOffset timestamp)
        {
            if (string.IsNullOrWhiteSpace(outputText))
            {
                throw new ArgumentException("OutputText must not be null, empty, or whitespace.", nameof(outputText));
            }

            OutputText = outputText;
            ProviderRequestId = providerRequestId;
            ProviderModel = providerModel;
            Timestamp = timestamp;
        }

        public static LlmRawResponse Create(string outputText, string? providerRequestId = null, string? providerModel = null)
        {
            return new LlmRawResponse(outputText, providerRequestId, providerModel, DateTimeOffset.UtcNow);
        }
    }
}
