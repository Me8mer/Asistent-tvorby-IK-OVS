using System;

namespace Assistant.Core.Model
{
    public sealed class ContextRequirement
    {
        public ContextScope Scope { get; }
        public string? SelectorHint { get; }
        public int? MaxCharacters { get; }

        public ContextRequirement(
            ContextScope scope,
            string? selectorHint = null,
            int? maxCharacters = null)
        {
            if (maxCharacters.HasValue && maxCharacters.Value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxCharacters), "MaxCharacters must be positive.");
            }

            Scope = scope;
            SelectorHint = selectorHint;
            MaxCharacters = maxCharacters;
        }

        public static ContextRequirement None()
        {
            return new ContextRequirement(ContextScope.None);
        }
    }
}
