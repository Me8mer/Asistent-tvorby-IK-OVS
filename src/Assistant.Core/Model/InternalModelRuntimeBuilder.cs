using System;
using System.Collections.Generic;

namespace Assistant.Core.Model
{
    public sealed class InternalModelRuntimeBuilder
    {
        public InternalModelRuntime Build(
            string templateVersion,
            IEnumerable<FieldAlias> aliases,
            IReadOnlyDictionary<FieldAlias, FieldBinding>? bindingsByAlias,
            IReadOnlyDictionary<FieldAlias, FieldDescriptor>? descriptorsByAlias,
            IEnumerable<SectionDescriptor>? sections)
        {
            if (string.IsNullOrWhiteSpace(templateVersion))
            {
                throw new ArgumentException("Template version must not be null or whitespace.", nameof(templateVersion));
            }

            if (aliases is null)
            {
                throw new ArgumentNullException(nameof(aliases));
            }

            Dictionary<FieldAlias, FieldNode> fieldsByAlias = BuildFields(aliases, bindingsByAlias, descriptorsByAlias);
            Dictionary<SectionAlias, SectionDescriptor> sectionsByAlias = BuildSections(sections);

            ClearFieldSectionOwnership(fieldsByAlias);
            AssignFieldOwnership(fieldsByAlias, sectionsByAlias);

            SectionGraphIndex sectionGraph = SectionGraphIndex.BuildAndValidate(sectionsByAlias);

            var model = new InternalModel(templateVersion, fieldsByAlias, sectionsByAlias, sectionGraph);

            var fieldStore = new FieldStore(model.FieldsByAlias);
            var proposalApplier = new ProposalApplier(fieldStore);

            return new InternalModelRuntime(model, fieldStore, proposalApplier);
        }

        private static Dictionary<FieldAlias, FieldNode> BuildFields(
            IEnumerable<FieldAlias> aliases,
            IReadOnlyDictionary<FieldAlias, FieldBinding>? bindingsByAlias,
            IReadOnlyDictionary<FieldAlias, FieldDescriptor>? descriptorsByAlias)
        {
            var fieldsByAlias = new Dictionary<FieldAlias, FieldNode>();

            foreach (FieldAlias alias in aliases)
            {
                if (fieldsByAlias.ContainsKey(alias))
                {
                    throw new InvalidOperationException($"Duplicate alias detected in internal model: {alias}.");
                }

                FieldBinding? binding = null;
                bindingsByAlias?.TryGetValue(alias, out binding);

                FieldDescriptor? descriptor = null;
                descriptorsByAlias?.TryGetValue(alias, out descriptor);

                fieldsByAlias.Add(alias, new FieldNode(alias, binding, descriptor));
            }

            return fieldsByAlias;
        }

        private static Dictionary<SectionAlias, SectionDescriptor> BuildSections(IEnumerable<SectionDescriptor>? sections)
        {
            var sectionsByAlias = new Dictionary<SectionAlias, SectionDescriptor>();

            if (sections is null)
            {
                return sectionsByAlias;
            }

            foreach (SectionDescriptor sectionDescriptor in sections)
            {
                SectionDescriptor normalizedDescriptor = SectionDescriptorNormalizer.Normalize(sectionDescriptor);

                if (sectionsByAlias.ContainsKey(normalizedDescriptor.SectionAlias))
                {
                    throw new InvalidOperationException($"Duplicate section alias '{normalizedDescriptor.SectionAlias}'.");
                }

                sectionsByAlias.Add(normalizedDescriptor.SectionAlias, normalizedDescriptor);
            }

            return sectionsByAlias;
        }

        private static void ClearFieldSectionOwnership(Dictionary<FieldAlias, FieldNode> fieldsByAlias)
        {
            foreach (FieldNode fieldNode in fieldsByAlias.Values)
            {
                fieldNode.ParentSectionAlias = null;
            }
        }

        private static void AssignFieldOwnership(
            Dictionary<FieldAlias, FieldNode> fieldsByAlias,
            Dictionary<SectionAlias, SectionDescriptor> sectionsByAlias)
        {
            var owningSectionByFieldAlias = new Dictionary<FieldAlias, SectionAlias>();

            foreach (SectionDescriptor sectionDescriptor in sectionsByAlias.Values)
            {
                foreach (FieldAlias fieldAlias in sectionDescriptor.FieldAliases)
                {
                    if (!fieldsByAlias.TryGetValue(fieldAlias, out FieldNode? fieldNode))
                    {
                        throw new InvalidOperationException(
                            $"Section '{sectionDescriptor.SectionAlias}' references unknown field alias '{fieldAlias}'.");
                    }

                    if (owningSectionByFieldAlias.TryGetValue(fieldAlias, out SectionAlias existingOwner))
                    {
                        throw new InvalidOperationException(
                            $"Field alias '{fieldAlias}' is owned by multiple sections: '{existingOwner}' and '{sectionDescriptor.SectionAlias}'.");
                    }

                    owningSectionByFieldAlias.Add(fieldAlias, sectionDescriptor.SectionAlias);
                    fieldNode.ParentSectionAlias = sectionDescriptor.SectionAlias;
                }
            }
        }
    }
}