using System;

namespace Assistant.Core.Model
{
    public sealed class FieldDescriptor
    {
        public FieldAlias Alias { get; }
        public FillMode FillModes { get; }
        public bool IsRequired { get; }
        public string? DisplayName { get; }
        public string? Instructions { get; }
        public string? ValueTypeHint { get; }

        public FieldDescriptor(
            FieldAlias alias,
            FillMode fillModes,
            bool isRequired,
            string? displayName,
            string? instructions,
            string? valueTypeHint)
        {
            Alias = alias;
            FillModes = fillModes;
            IsRequired = isRequired;
            DisplayName = displayName;
            Instructions = instructions;
            ValueTypeHint = valueTypeHint;
        }
    }
}
