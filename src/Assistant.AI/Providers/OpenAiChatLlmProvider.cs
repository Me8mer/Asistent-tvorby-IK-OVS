using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
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
        private readonly bool useWebSearch;
        private readonly HttpClient httpClient;

        /// <summary>
        /// Create a new OpenAI chat provider. When <paramref name="useWebSearch"/> is
        /// true the provider will call the OpenAI Responses API with the
        /// web_search tool enabled, allowing the model to perform its own
        /// retrieval rather than relying on the caller to provide context. In
        /// this mode the <paramref name="modelName"/> should refer to a model
        /// compatible with the Responses API (for example gpt‑5), and the
        /// underlying request will be composed via <see cref="HttpClient"/>.
        /// </summary>
        /// <param name="modelName">The OpenAI model name.</param>
        /// <param name="apiKey">Your OpenAI API key.</param>
        /// <param name="useWebSearch">Whether to enable web search mode.</param>
        /// <param name="httpClient">Optional HttpClient to use for Responses requests.</param>
        public OpenAiChatLlmProvider(string modelName, string apiKey, bool useWebSearch = false, HttpClient? httpClient = null)
        {
            this.modelName = modelName;
            this.apiKey = apiKey;
            this.useWebSearch = useWebSearch;
            this.httpClient = httpClient ?? new HttpClient();
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

            // When useWebSearch is enabled call the Responses API with the web_search tool.
            if (useWebSearch)
            {
                string input = string.IsNullOrEmpty(prompt.SystemText)
                    ? prompt.UserText
                    : prompt.SystemText + "\n" + prompt.UserText;
                string output = await GenerateWithResponsesWebSearchAsync(input, cancellationToken)
                    .ConfigureAwait(false);
                return new LlmRawResponse(
                    outputText: output,
                    providerRequestId: null,
                    providerModel: modelName,
                    timestamp: DateTimeOffset.UtcNow);
            }

            // Default behaviour uses the chat completions API via OpenAI SDK.
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

        /// <summary>
        /// Call the OpenAI Responses API with the web_search tool enabled. Returns
        /// the model's final output text from the response. This method wraps
        /// HTTP operations and JSON parsing. Any HTTP or serialization errors
        /// will propagate to the caller.
        /// </summary>
        private async Task<string> GenerateWithResponsesWebSearchAsync(
            string input,
            CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                "https://api.openai.com/v1/responses");

            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

            var payload = new
            {
                model = modelName,
                input = input,
                tool_choice = "auto",
                tools = new object[]
                {
                    new
                    {
                        type = "web_search"
                    }
                }
            };

            string json = System.Text.Json.JsonSerializer.Serialize(payload);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken)
                .ConfigureAwait(false);

            string responseJson = await response.Content.ReadAsStringAsync(cancellationToken)
                .ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            using var document = System.Text.Json.JsonDocument.Parse(responseJson);
            JsonElement root = document.RootElement;

            if (root.TryGetProperty("output_text", out JsonElement outputTextElement))
            {
                string? outputText = outputTextElement.GetString();
                if (!string.IsNullOrWhiteSpace(outputText))
                {
                    return outputText;
                }
            }

            if (root.TryGetProperty("output", out JsonElement outputArray) &&
                outputArray.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement outputItem in outputArray.EnumerateArray())
                {
                    if (!outputItem.TryGetProperty("type", out JsonElement typeElement))
                        continue;
                    string? type = typeElement.GetString();
                    if (!string.Equals(type, "message", StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (!outputItem.TryGetProperty("content", out JsonElement contentArray) || contentArray.ValueKind != JsonValueKind.Array)
                        continue;
                    foreach (JsonElement contentItem in contentArray.EnumerateArray())
                    {
                        if (!contentItem.TryGetProperty("type", out JsonElement contentTypeElement))
                            continue;
                        string? contentType = contentTypeElement.GetString();
                        if (!string.Equals(contentType, "output_text", StringComparison.OrdinalIgnoreCase) &&
                            !string.Equals(contentType, "text", StringComparison.OrdinalIgnoreCase))
                            continue;
                        if (contentItem.TryGetProperty("text", out JsonElement textElement))
                        {
                            string? text = textElement.GetString();
                            if (!string.IsNullOrWhiteSpace(text))
                            {
                                return text;
                            }
                        }
                    }
                }
            }
            throw new InvalidOperationException("OpenAI Responses API returned no text output.");
        }
    }
}