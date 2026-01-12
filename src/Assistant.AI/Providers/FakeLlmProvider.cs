using System;
using System.Threading;
using System.Threading.Tasks;
using Assistant.AI.Abstractions;
using Assistant.AI.Models;

namespace Assistant.AI.Providers
{
    public sealed class FakeLlmProvider : ILlmProvider
    {
        private readonly Func<ModelRouting, LlmPrompt, string> outputFactory;
        private readonly string? providerRequestIdPrefix;

        public FakeLlmProvider(
            Func<ModelRouting, LlmPrompt, string>? outputFactory = null,
            string? providerRequestIdPrefix = "fake")
        {
            this.outputFactory = outputFactory ?? DefaultOutputFactory;
            this.providerRequestIdPrefix = providerRequestIdPrefix;
        }

        public Task<LlmRawResponse> GenerateAsync(
            ModelRouting routing,
            LlmPrompt prompt,
            CancellationToken cancellationToken)
        {
            if (routing is null)
            {
                throw new ArgumentNullException(nameof(routing));
            }

            if (prompt is null)
            {
                throw new ArgumentNullException(nameof(prompt));
            }

            cancellationToken.ThrowIfCancellationRequested();

            string outputText = outputFactory(routing, prompt);

            string? requestId = providerRequestIdPrefix is null
                ? null
                : $"{providerRequestIdPrefix}-{Guid.NewGuid():N}";

            LlmRawResponse response = new LlmRawResponse(
                outputText: outputText,
                providerRequestId: requestId,
                providerModel: routing.ModelName,
                timestamp: DateTimeOffset.UtcNow);

            return Task.FromResult(response);
        }

        private static string DefaultOutputFactory(ModelRouting routing, LlmPrompt prompt)
        {
            return
                """
                {
                  "value": "FAKE_VALUE",
                  "confidence": 0.5,
                  "explanation": "Fake provider output for wiring tests."
                }
                """;
        }
    }
}
