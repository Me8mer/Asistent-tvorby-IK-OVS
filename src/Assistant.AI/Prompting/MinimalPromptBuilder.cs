using System;
using System.Text;
using Assistant.AI.Abstractions;
using Assistant.AI.Models;
using Assistant.Core.Model;

namespace Assistant.AI.Prompting
{
    public sealed class MinimalPromptBuilder : IPromptBuilder
    {
        private const int DefaultMaxContextCharacters = 3000;
        private const string ProposalSchemaFile = "proposal.v1.schema.json";

        public LlmPrompt BuildPrompt(GenerationRequest request)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            string systemText = BuildSystemText();
            string userText = BuildUserText(request);

            return new LlmPrompt(systemText, userText, request.ContextReferences);
        }

        private static string BuildSystemText()
        {
            string schemaText = SchemaLoader.LoadSchemaText(ProposalSchemaFile);

            return
                """
                You fill exactly one field in a structured document.

                You MUST return exactly one JSON object that conforms to the following JSON Schema.
                Do not include markdown.
                Do not include explanations outside the JSON.
                Do not include additional properties.

                JSON Schema:
                """ + "\n" + schemaText;
        }

        private static string BuildUserText(GenerationRequest request)
        {
            FieldDescriptor? descriptor = request.Descriptor;

            StringBuilder builder = new StringBuilder();

            builder.AppendLine("Task");
            builder.AppendLine("Fill the following field.");
            builder.AppendLine();

            builder.AppendLine("Field");
            builder.Append("Alias: ").AppendLine(request.Alias.Value);

            if (!string.IsNullOrWhiteSpace(descriptor?.DisplayName))
            {
                builder.Append("DisplayName: ").AppendLine(descriptor.DisplayName);
            }

            if (!string.IsNullOrWhiteSpace(descriptor?.ValueTypeHint))
            {
                builder.Append("ValueTypeHint: ").AppendLine(descriptor.ValueTypeHint);
            }

            builder.Append("Mode: ").AppendLine(request.Mode.ToString());
            builder.AppendLine();

            if (!string.IsNullOrWhiteSpace(descriptor?.Instructions))
            {
                builder.AppendLine("Instructions");
                builder.AppendLine(descriptor.Instructions);
                builder.AppendLine();
            }

            string? contextText = request.DocumentContextText;
            if (!string.IsNullOrWhiteSpace(contextText))
            {
                int maxContextCharacters = ResolveMaxContextCharacters(descriptor);
                string normalizedContext = NormalizeContext(contextText, maxContextCharacters);

                builder.AppendLine("Context");
                builder.AppendLine(normalizedContext);
                builder.AppendLine();
            }

            builder.AppendLine("Remember");
            builder.AppendLine("Return only the JSON object, nothing else.");

            return builder.ToString();
        }

        private static int ResolveMaxContextCharacters(FieldDescriptor? descriptor)
        {
            int defaultLimit = DefaultMaxContextCharacters;

            ContextRequirement? contextRequirement = descriptor?.ContextRequirement;
            if (contextRequirement is null || !contextRequirement.MaxCharacters.HasValue)
            {
                return defaultLimit;
            }

            int configuredLimit = contextRequirement.MaxCharacters.Value;
            return configuredLimit > 0 ? configuredLimit : defaultLimit;
        }

        private static string NormalizeContext(string contextText, int maxCharacters)
        {
            string normalized = contextText.Replace("\r\n", "\n").Trim();

            if (normalized.Length <= maxCharacters)
            {
                return normalized;
            }

            return normalized.Substring(0, maxCharacters).TrimEnd() + "\n[Context truncated]";
        }
    }
}
