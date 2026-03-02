using System;
using System.Collections.Generic;

namespace Assistant.Core.Model
{
    public sealed class InternalModel
    {
        private readonly Dictionary<FieldAlias, FieldNode> fieldsByAlias;
        private readonly Dictionary<SectionAlias, SectionDescriptor> sectionsByAlias;

        internal InternalModel(
            string templateVersion,
            Dictionary<FieldAlias, FieldNode> fieldsByAlias,
            Dictionary<SectionAlias, SectionDescriptor> sectionsByAlias,
            SectionGraphIndex sectionGraph)
        {
            if (string.IsNullOrWhiteSpace(templateVersion))
            {
                throw new ArgumentException("Template version must not be null or whitespace.", nameof(templateVersion));
            }

            TemplateVersion = templateVersion;
            this.fieldsByAlias = fieldsByAlias ?? throw new ArgumentNullException(nameof(fieldsByAlias));
            this.sectionsByAlias = sectionsByAlias ?? throw new ArgumentNullException(nameof(sectionsByAlias));
            SectionGraph = sectionGraph ?? throw new ArgumentNullException(nameof(sectionGraph));
        }

        public string TemplateVersion { get; }

        public IReadOnlyDictionary<FieldAlias, FieldNode> FieldsByAlias => fieldsByAlias;
        public IReadOnlyDictionary<SectionAlias, SectionDescriptor> SectionsByAlias => sectionsByAlias;

        public SectionGraphIndex SectionGraph { get; }

        internal Dictionary<FieldAlias, FieldNode> MutableFieldsByAlias => fieldsByAlias;
        internal Dictionary<SectionAlias, SectionDescriptor> MutableSectionsByAlias => sectionsByAlias;

        public bool TryGetField(FieldAlias alias, out FieldNode fieldNode)
        {
            return fieldsByAlias.TryGetValue(alias, out fieldNode!);
        }

        public FieldNode GetField(FieldAlias alias)
        {
            if (!fieldsByAlias.TryGetValue(alias, out FieldNode? fieldNode))
            {
                throw new KeyNotFoundException($"Field with alias '{alias}' was not found.");
            }

            return fieldNode;
        }

        public bool TryGetSection(SectionAlias sectionAlias, out SectionDescriptor sectionDescriptor)
        {
            return sectionsByAlias.TryGetValue(sectionAlias, out sectionDescriptor!);
        }

        public SectionDescriptor GetSection(SectionAlias sectionAlias)
        {
            if (!sectionsByAlias.TryGetValue(sectionAlias, out SectionDescriptor? sectionDescriptor))
            {
                throw new KeyNotFoundException($"Section '{sectionAlias}' was not found.");
            }

            return sectionDescriptor;
        }

        public bool TryGetOwningSectionAlias(FieldAlias fieldAlias, out SectionAlias owningSectionAlias)
        {
            if (!fieldsByAlias.TryGetValue(fieldAlias, out FieldNode? fieldNode))
            {
                owningSectionAlias = default;
                return false;
            }

            if (!fieldNode.ParentSectionAlias.HasValue)
            {
                owningSectionAlias = default;
                return false;
            }

            owningSectionAlias = fieldNode.ParentSectionAlias.Value;
            return true;
        }
    }
}