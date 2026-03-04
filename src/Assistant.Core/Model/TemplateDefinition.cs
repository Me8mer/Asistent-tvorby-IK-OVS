using System;
using System.Collections.Generic;

namespace Assistant.Core.Model
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