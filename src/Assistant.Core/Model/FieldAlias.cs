using System;

namespace Assistant.Core.Model
{
    public readonly record struct FieldAlias
    {
        public string Value { get; }

        public FieldAlias(string value)
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

        public static bool TryCreate(string? value, out FieldAlias fieldAlias, out string? error)
        {
            if (!TryValidate(value, out error))
            {
                fieldAlias = default;
                return false;
            }

            fieldAlias = new FieldAlias(value!);
            return true;
        }

        private static bool TryValidate(string? value, out string? error)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                error = "Alias must not be null, empty, or whitespace.";
                return false;
            }

            if (!value.StartsWith("IKOVS_", StringComparison.Ordinal))
            {
                error = "Alias must start with the prefix 'IKOVS_'.";
                return false;
            }

            for (int characterIndex = 0; characterIndex < value.Length; characterIndex++)
            {
                char character = value[characterIndex];

                bool isUppercaseLetter = character >= 'A' && character <= 'Z';
                bool isDigit = character >= '0' && character <= '9';
                bool isUnderscore = character == '_';
                bool isHyphen = character == '-';

                if (isUppercaseLetter || isDigit || isUnderscore || isHyphen)
                {
                    continue;
                }

                error = $"Alias contains an invalid character '{character}' at index {characterIndex}. Allowed: A-Z, 0-9, '_', '-'.";
                return false;
            }

            error = null;
            return true;
        }
    }
}
