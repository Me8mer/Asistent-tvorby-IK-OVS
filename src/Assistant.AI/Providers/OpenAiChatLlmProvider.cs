using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Assistant.AI.Abstractions;
using Assistant.AI.Models;
using Assistant.AI.Prompting;
using OpenAI.Chat;

namespace Assistant.AI.Providers
{
    public sealed class OpenAiChatLlmProvider : ILlmProvider
    {
        private const string ProposalSchemaFileName = "proposal.v1.schema.json";
        private readonly string modelName;
        private readonly string apiKey;

        public OpenAiChatLlmProvider(string modelName, string? apiKey = null)
        {
            this.modelName = !string.IsNullOrWhiteSpace(modelName)
                ? modelName
                : throw new ArgumentException("Model name must be provided.", nameof(modelName));

            this.apiKey = !string.IsNullOrWhiteSpace(apiKey)
                ? apiKey
                : (Environment.GetEnvironmentVariable("OPENAI_API_KEY")
                   ?? throw new InvalidOperationException("OPENAI_API_KEY environment variable is not set."));
        }

        public async Task<LlmRawResponse> GenerateAsync(
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

            ChatClient chatClient = new(model: modelName, apiKey: apiKey);

            List<ChatMessage> messages =
            [
                new SystemChatMessage(prompt.SystemText),
                new UserChatMessage(prompt.UserText)
            ];

            string schemaText = SchemaLoader.LoadSchemaText(ProposalSchemaFileName);

            ChatCompletionOptions options = new()
            {
                ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                    jsonSchemaFormatName: "proposal_v1",
                    jsonSchema: BinaryData.FromBytes(Encoding.UTF8.GetBytes(schemaText)),
                    jsonSchemaIsStrict: true)
            };

            ChatCompletion completion = await chatClient.CompleteChatAsync(messages, options, cancellationToken);

            string outputText = completion.Content.Count > 0 ? completion.Content[0].Text : string.Empty;

            return new LlmRawResponse(
                outputText: outputText,
                providerRequestId: null,
                providerModel: modelName,
                timestamp: DateTimeOffset.UtcNow);
        }
    }
}
