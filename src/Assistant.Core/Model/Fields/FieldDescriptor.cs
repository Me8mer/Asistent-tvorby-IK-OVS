using System.Collections.Generic;

namespace Assistant.Core.Model.Fields
{
    /// <summary>
    /// Immutable definition of a field. THIS IS NOT RUNTIME STATE.
    /// Describes a field in the internal model.  Field descriptors are
    /// immutable and capture declarative metadata such as filling modes,
    /// display names, query hints and context requirements.  They may
    /// optionally specify one or more context sources from which external
    /// information should be gathered when building web context packs.
    /// </summary>
    public sealed class FieldDescriptor
    {
        public FieldAlias Alias { get; }
        public FillMode FillModes { get; }
        public bool IsRequired { get; }
        public string? DisplayName { get; }
        public IReadOnlyList<string>? QueryHints { get; }
        public string? Instructions { get; }
        public string? ValueTypeHint { get; }
        public ContextRequirement? ContextRequirement { get; }
        public IReadOnlyList<string>? ContextSources { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="FieldDescriptor"/>.  The
        /// <paramref name="contextSources"/> parameter allows callers to
        /// specify additional context source aliases to use during context
        /// retrieval.  When null, the default source (WebOffice) will be used.
        /// </summary>
        public FieldDescriptor(
            FieldAlias alias,
            FillMode fillModes,
            bool isRequired,
            string? displayName,
            IReadOnlyList<string>? queryHints,
            string? instructions,
            string? valueTypeHint,
            ContextRequirement? contextRequirement,
            IReadOnlyList<string>? contextSources = null)
        {
            Alias = alias;
            FillModes = fillModes;
            IsRequired = isRequired;
            DisplayName = displayName;
            QueryHints = queryHints;
            Instructions = instructions;
            ValueTypeHint = valueTypeHint;
            ContextRequirement = contextRequirement;
            ContextSources = contextSources;
        }
    }
}