using System;
using System.Globalization;
using System.Text.Json;
using Assistant.AI.Abstractions;
using Assistant.AI.Models;
using Assistant.Core.Model;

namespace Assistant.AI.Parsing
{
    public sealed class StrictJsonProposalParser : IProposalParser
    {
        private readonly bool allowModelProposedStatus;

        public StrictJsonProposalParser(bool allowModelProposedStatus = false)
        {
            this.allowModelProposedStatus = allowModelProposedStatus;
        }

        public Proposal ParseProposal(GenerationRequest request, ModelRouting routing, LlmRawResponse response)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (routing is null)
            {
                throw new ArgumentNullException(nameof(routing));
            }

            if (response is null)
            {
                throw new ArgumentNullException(nameof(response));
            }

            string jsonText = ExtractFirstJsonObject(response.OutputText);

            ProposalJsonV1 parsed = DeserializeStrict(jsonText);

            double confidence = NormalizeConfidence(parsed.Confidence);

            FieldStatus proposedStatus = DetermineProposedStatus(request, parsed.ProposedStatus);

            string source = BuildSource(routing, response);

            return Proposal.Create(
                alias: request.Alias,
                proposedStatus: proposedStatus,
                proposedValue: parsed.Value,
                source: source,
                confidence: confidence,
                explanation: parsed.Explanation,
                contextReferences: request.ContextReferences);
        }

        private static ProposalJsonV1 DeserializeStrict(string jsonText)
        {
            try
            {
                ProposalJsonV1? parsed = JsonSerializer.Deserialize<ProposalJsonV1>(
                    jsonText,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (parsed is null)
                {
                    throw new FormatException("Model output JSON could not be deserialized.");
                }

                if (parsed.Value is null)
                {
                    throw new FormatException("Missing required JSON property 'value'.");
                }

                if (parsed.Confidence is null)
                {
                    throw new FormatException("Missing required JSON property 'confidence'.");
                }

                return parsed;
            }
            catch (JsonException jsonException)
            {
                throw new FormatException("Model output is not valid JSON for proposal.v1.", jsonException);
            }
        }

        private static string ExtractFirstJsonObject(string outputText)
        {
            if (string.IsNullOrWhiteSpace(outputText))
            {
                throw new FormatException("Model output is empty.");
            }

            int firstBraceIndex = outputText.IndexOf('{');
            int lastBraceIndex = outputText.LastIndexOf('}');

            if (firstBraceIndex < 0 || lastBraceIndex < 0 || lastBraceIndex <= firstBraceIndex)
            {
                throw new FormatException("Model output does not contain a JSON object.");
            }

            string candidate = outputText.Substring(firstBraceIndex, lastBraceIndex - firstBraceIndex + 1).Trim();

            if (candidate.Length == 0 || candidate[0] != '{' || candidate[candidate.Length - 1] != '}')
            {
                throw new FormatException("Failed to extract JSON object from model output.");
            }

            return candidate;
        }

        private static double NormalizeConfidence(double? confidence)
        {
            if (!confidence.HasValue)
            {
                return 0.0;
            }

            if (double.IsNaN(confidence.Value) || double.IsInfinity(confidence.Value))
            {
                return 0.0;
            }

            if (confidence.Value < 0.0)
            {
                return 0.0;
            }

            if (confidence.Value > 1.0)
            {
                return 1.0;
            }

            return confidence.Value;
        }

        private FieldStatus DetermineProposedStatus(GenerationRequest request, string? modelProposedStatus)
        {
            if (allowModelProposedStatus && !string.IsNullOrWhiteSpace(modelProposedStatus))
            {
                if (TryMapProposedStatus(modelProposedStatus, out FieldStatus mappedStatus))
                {
                    return mappedStatus;
                }
            }

            return request.Mode switch
            {
                GenerationMode.InitialPrefill => FieldStatus.AiProposal,
                GenerationMode.Regenerate => FieldStatus.AiProposal,
                GenerationMode.Improve => FieldStatus.AiProposal,
                _ => FieldStatus.AiProposal
            };
        }

        private static bool TryMapProposedStatus(string text, out FieldStatus proposedStatus)
        {
            if (text.Equals("AiProposal", StringComparison.OrdinalIgnoreCase))
            {
                proposedStatus = FieldStatus.AiProposal;
                return true;
            }

            if (text.Equals("AiInteractive", StringComparison.OrdinalIgnoreCase))
            {
                proposedStatus = FieldStatus.AiInteractive;
                return true;
            }

            if (text.Equals("NeedsValidation", StringComparison.OrdinalIgnoreCase))
            {
                proposedStatus = FieldStatus.NeedsValidation;
                return true;
            }

            if (text.Equals("Deterministic", StringComparison.OrdinalIgnoreCase))
            {
                proposedStatus = FieldStatus.Deterministic;
                return true;
            }

            proposedStatus = default;
            return false;
        }

        private static string BuildSource(ModelRouting routing, LlmRawResponse response)
        {
            string providerType = routing.ProviderType.ToString();
            string modelName = routing.ModelName;

            if (!string.IsNullOrWhiteSpace(response.ProviderRequestId))
            {
                return string.Create(
                    CultureInfo.InvariantCulture,
                    $"{providerType}:{modelName} (req:{response.ProviderRequestId})");
            }

            return string.Create(CultureInfo.InvariantCulture, $"{providerType}:{modelName}");
        }

        private sealed class ProposalJsonV1
        {
            public string? Value { get; set; }
            public double? Confidence { get; set; }
            public string? Explanation { get; set; }
            public string? ProposedStatus { get; set; }
        }
    }
}
