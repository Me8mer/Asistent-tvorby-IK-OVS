using System.Collections.Generic;

namespace Assistant.Dependencies.TemplateDefinitions
{
    /// <summary>
    /// Represents a field definition loaded from a YAML template.  The
    /// properties correspond directly to attributes defined in the input
    /// specification.  Fields may optionally specify one or more context
    /// sources from which supplemental information should be retrieved.
    /// </summary>
internal sealed class DefinitionField
{
        /// <summary>
        /// Alias identifying the field.  Must be unique across the template.
        /// </summary>
        public string Alias { get; set; } = string.Empty;

        /// <summary>
        /// Optional parent section alias.  If null or empty the field does not
        /// belong to any section.
        /// </summary>
    public string? Parent { get; set; }

        /// <summary>
        /// Optional human‑friendly display name in Czech.
        /// </summary>
    public string? LabelCs { get; set; }

        /// <summary>
        /// Specifies how the field should be filled (deterministically, via AI, etc.).
        /// </summary>
    public DefinitionFill? Fill { get; set; }

        /// <summary>
        /// Indicates whether the field is required.  If null, defaults to false.
        /// </summary>
    public bool? Required { get; set; }

        /// <summary>
        /// Optional hint about the expected value type (e.g. "string", "date").
        /// </summary>
    public string? ValueType { get; set; }

        /// <summary>
        /// Optional list of additional search query hints to improve context retrieval.
        /// </summary>
    public List<string>? QueryHints { get; set; }

        /// <summary>
        /// Optional instructions shown to the AI model when filling the field.
        /// </summary>
    public string? Instructions { get; set; }

        /// <summary>
        /// Optional list of context source aliases.  When specified, these
        /// sources indicate where supplemental information for this field
        /// should be gathered from.  If null or empty, the default
        /// <see cref="Assistant.Core.Model.ContextSource.WebOffice"/> source
        /// is used.  Valid values correspond to aliases defined in
        /// docs/sources.md and in <see cref="Assistant.Core.Model.ContextSource"/>.
        /// Unknown values are ignored.
        /// </summary>
        public List<string>? ContextSources { get; set; }
    }
}