using System;

namespace Assistant.AI.Models
{
    public sealed class ModelRouting
    {
        public LlmProviderType ProviderType { get; }
        public string ModelName { get; }
        public LlmGenerationParameters Parameters { get; }

        public ModelRouting(LlmProviderType providerType, string modelName, LlmGenerationParameters? parameters = null)
        {
            if (providerType == LlmProviderType.Unknown)
            {
                throw new ArgumentException("ProviderType must not be Unknown.", nameof(providerType));
            }

            if (string.IsNullOrWhiteSpace(modelName))
            {
                throw new ArgumentException("ModelName must not be null, empty, or whitespace.", nameof(modelName));
            }

            ProviderType = providerType;
            ModelName = modelName;
            Parameters = parameters ?? new LlmGenerationParameters();
        }
    }
}