using System;
using System.Collections.Generic;

/// <summary>
/// Definition of a section (from template definition).
/// 
/// Contains:
/// - field grouping
/// - hierarchy (parent)
/// - optional query hints
/// 
/// Used for context retrieval + logical grouping.
/// </summary>
namespace Assistant.Core.Model.Templates
{
    public sealed class TemplateDefinition
    {
        public string TemplateVersion { get; }
        public IReadOnlyList<FieldAlias> FieldAliases { get; }
        public IReadOnlyDictionary<FieldAlias, FieldDescriptor> DescriptorsByAlias { get; }
        public IReadOnlyList<SectionDescriptor> Sections { get; }

        public TemplateDefinition(
            string templateVersion,
            IReadOnlyList<FieldAlias> fieldAliases,
            IReadOnlyDictionary<FieldAlias, FieldDescriptor> descriptorsByAlias,
            IReadOnlyList<SectionDescriptor> sections)
        {
            if (string.IsNullOrWhiteSpace(templateVersion))
            {
                throw new ArgumentException("Template version must not be null or whitespace.", nameof(templateVersion));
            }

            TemplateVersion = templateVersion;
            FieldAliases = fieldAliases ?? throw new ArgumentNullException(nameof(fieldAliases));
            DescriptorsByAlias = descriptorsByAlias ?? throw new ArgumentNullException(nameof(descriptorsByAlias));
            Sections = sections ?? throw new ArgumentNullException(nameof(sections));
        }
    }
}