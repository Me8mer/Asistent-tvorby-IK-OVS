using System;

namespace Assistant.Core.Model
{
    public readonly record struct SectionAlias
    {
        public string Value { get; }

        public SectionAlias(string value)
        {
            if (!TryValidate(value, out string? validationError))
            {
                throw new ArgumentException(validationError, nameof(value));
            }

            Value = value;
        }

        public override string ToString()
        {
            return Value;
        }

        public static bool TryCreate(string? value, out SectionAlias sectionAlias, out string? error)
        {
            if (!TryValidate(value, out error))
            {
                sectionAlias = default;
                return false;
            }

            sectionAlias = new SectionAlias(value!);
            return true;
        }

        private static bool TryValidate(string? value, out string? error)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                error = "Alias must not be null, empty, or whitespace.";
                return false;
            }

            int colonIndex = value.IndexOf(':');
            if (colonIndex != -1 && value.IndexOf(':', colonIndex + 1) != -1)
            {
                error = "Alias may contain at most one ':' character.";
                return false;
            }

            string basePart = colonIndex == -1
                ? value
                : value[..colonIndex];

            if (!basePart.StartsWith("IKOVS_", StringComparison.Ordinal))
            {
                error = "Alias must start with the prefix 'IKOVS_'.";
                return false;
            }

            for (int characterIndex = 0; characterIndex < basePart.Length; characterIndex++)
            {
                char character = basePart[characterIndex];

                bool isUppercaseLetter = character >= 'A' && character <= 'Z';
                bool isDigit = character >= '0' && character <= '9';
                bool isUnderscore = character == '_';
                bool isHyphen = character == '-';

                if (isUppercaseLetter || isDigit || isUnderscore || isHyphen)
                {
                    continue;
                }

                error =
                    $"Alias contains an invalid character '{character}' at index {characterIndex}. " +
                    "Allowed in base alias: A-Z, 0-9, '_', '-'.";
                return false;
            }

            if (colonIndex != -1 && colonIndex == value.Length - 1)
            {
                error = "Alias role must not be empty after ':'.";
                return false;
            }

            error = null;
            return true;
        }
    }
}
