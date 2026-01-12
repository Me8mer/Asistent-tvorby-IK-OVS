using System;

namespace Assistant.AI.Models
{
    public sealed class LlmGenerationParameters
    {
        public double? Temperature { get; }
        public int? MaxOutputTokens { get; }

        public LlmGenerationParameters(double? temperature = null, int? maxOutputTokens = null)
        {
            if (temperature.HasValue && (temperature.Value < 0.0 || temperature.Value > 2.0))
            {
                throw new ArgumentOutOfRangeException(nameof(temperature), "Temperature must be in range 0.0 to 2.0.");
            }

            if (maxOutputTokens.HasValue && maxOutputTokens.Value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxOutputTokens), "MaxOutputTokens must be positive.");
            }

            Temperature = temperature;
            MaxOutputTokens = maxOutputTokens;
        }
    }
}
