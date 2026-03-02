using System;
using System.Collections.Generic;
using Assistant.Core.Merge;


namespace Assistant.Core.Model
{
    public sealed class InternalModel
    {
        private readonly Dictionary<FieldAlias, FieldNode> fieldsByAlias;
        private readonly Dictionary<SectionAlias, SectionDescriptor> sectionsByAlias;
        private readonly Dictionary<FieldAlias, SectionAlias> owningSectionByFieldAlias;

        public string TemplateVersion { get; }
        public IReadOnlyDictionary<FieldAlias, FieldNode> FieldsByAlias => fieldsByAlias;
        public IReadOnlyDictionary<SectionAlias, SectionDescriptor> SectionsByAlias => sectionsByAlias;

        public InternalModel(
            string templateVersion,
            IEnumerable<FieldAlias> aliases,
            IReadOnlyDictionary<FieldAlias, FieldBinding>? bindingsByAlias,
            IReadOnlyDictionary<FieldAlias, FieldDescriptor>? descriptorsByAlias,
            IEnumerable<SectionDescriptor>? sections)
        {
            if (string.IsNullOrWhiteSpace(templateVersion))
            {
                throw new ArgumentException("Template version must not be null, empty, or whitespace.", nameof(templateVersion));
            }

            if (aliases is null)
            {
                throw new ArgumentNullException(nameof(aliases));
            }

            TemplateVersion = templateVersion;
            fieldsByAlias = new Dictionary<FieldAlias, FieldNode>();
            sectionsByAlias = new Dictionary<SectionAlias, SectionDescriptor>();
            owningSectionByFieldAlias = new Dictionary<FieldAlias, SectionAlias>();


            foreach (FieldAlias alias in aliases)
            {
                if (fieldsByAlias.ContainsKey(alias))
                {
                    throw new InvalidOperationException($"Duplicate alias detected in internal model: {alias}.");
                }

                FieldBinding? binding = null;
                if (bindingsByAlias is not null)
                {
                    bindingsByAlias.TryGetValue(alias, out binding);
                }

                FieldDescriptor? descriptor = null;
                if (descriptorsByAlias is not null)
                {
                    descriptorsByAlias.TryGetValue(alias, out descriptor);
                }

                fieldsByAlias.Add(alias, new FieldNode(alias, binding, descriptor));
            }

            if (sections is not null)
            {
                AddSections(sections);
            }
            
            if (sections is null)
            {
                throw new ArgumentNullException(nameof(sections), "InternalModel requires sections for this prototype.");
            }
        }

        private static SectionDescriptor NormalizeSectionDescriptor(SectionDescriptor sectionDescriptor)
        {
            IReadOnlyList<string>? normalizedQueryHints = NormalizeQueryHints(sectionDescriptor.QueryHints);

            if (ReferenceEquals(normalizedQueryHints, sectionDescriptor.QueryHints))
            {
                return sectionDescriptor;
            }

            return new SectionDescriptor(
                sectionAlias: sectionDescriptor.SectionAlias,
                parentSectionAlias: sectionDescriptor.ParentSectionAlias,
                childSectionAliases: sectionDescriptor.ChildSectionAliases,
                fieldAliases: sectionDescriptor.FieldAliases,
                queryHints: normalizedQueryHints,
                displayName: sectionDescriptor.DisplayName,
                orderIndex: sectionDescriptor.OrderIndex);
        }

        private static IReadOnlyList<string>? NormalizeQueryHints(IReadOnlyList<string>? queryHints)
        {
            if (queryHints is null || queryHints.Count == 0)
            {
                return null;
            }

            var normalizedList = new List<string>(capacity: queryHints.Count);
            var seenHints = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (string hint in queryHints)
            {
                if (hint is null)
                {
                    continue;
                }

                string trimmedHint = hint.Trim();
                if (trimmedHint.Length == 0)
                {
                    continue;
                }

                if (seenHints.Add(trimmedHint))
                {
                    normalizedList.Add(trimmedHint);
                }
            }

            return normalizedList.Count == 0 ? null : normalizedList;
        }


        private void AddSections(IEnumerable<SectionDescriptor> sections)
        {
            foreach (SectionDescriptor sectionDescriptor in sections)
            {
                SectionDescriptor normalizedSectionDescriptor = NormalizeSectionDescriptor(sectionDescriptor);

                if (sectionsByAlias.ContainsKey(sectionDescriptor.SectionAlias))
                {
                    throw new InvalidOperationException($"Duplicate section alias '{sectionDescriptor.SectionAlias}'.");
                }

                sectionsByAlias.Add(normalizedSectionDescriptor.SectionAlias, normalizedSectionDescriptor);

                foreach (FieldAlias fieldAlias in normalizedSectionDescriptor.FieldAliases)
                {
                    if (!fieldsByAlias.ContainsKey(fieldAlias))
                    {
                        throw new InvalidOperationException(
                            $"Section '{normalizedSectionDescriptor.SectionAlias}' references unknown field alias '{fieldAlias}'.");
                    }

                    if (owningSectionByFieldAlias.TryGetValue(fieldAlias, out SectionAlias existingOwner))
                    {
                        throw new InvalidOperationException(
                            $"Field alias '{fieldAlias}' is owned by multiple sections: '{existingOwner}' and '{normalizedSectionDescriptor.SectionAlias}'.");
                    }

                    owningSectionByFieldAlias.Add(fieldAlias, normalizedSectionDescriptor.SectionAlias);
                }
            }

            ValidateSectionGraph();
            ValidateAllFieldsAreAssignedToSection();

        }

        public bool TryGetField(FieldAlias alias, out FieldNode fieldNode)
        {
            return fieldsByAlias.TryGetValue(alias, out fieldNode!);
        }

        public FieldNode GetField(FieldAlias alias)
        {
            if (!fieldsByAlias.TryGetValue(alias, out FieldNode? fieldNode))
            {
                throw new KeyNotFoundException($"Field with alias '{alias}' was not found in the internal model.");
            }

            return fieldNode;
        }

        public IEnumerable<FieldNode> GetAllFields()
        {
            return fieldsByAlias.Values;
        }

        public bool TryAddProposal(Proposal proposal, out string? rejectionReason)
        {
            if (!fieldsByAlias.TryGetValue(proposal.Alias, out FieldNode? fieldNode))
            {
                rejectionReason = $"Field with alias '{proposal.Alias}' does not exist.";
                return false;
            }

            fieldNode.AddProposal(proposal);
            rejectionReason = null;
            return true;
        }

        public bool TrySetCurrentValue(FieldValue newValue, out string? rejectionReason)
        {
            if (!fieldsByAlias.TryGetValue(newValue.Alias, out FieldNode? fieldNode))
            {
                rejectionReason = $"Field with alias '{newValue.Alias}' does not exist.";
                return false;
            }

            fieldNode.SetCurrentValue(newValue);
            rejectionReason = null;
            return true;
        }

        public bool TryUpdateDependencySnapshot(
            FieldAlias alias,
            DependencySnapshot snapshot,
            out string? rejectionReason)
        {
            if (!fieldsByAlias.TryGetValue(alias, out FieldNode? fieldNode))
            {
                rejectionReason = $"Field with alias '{alias}' does not exist.";
                return false;
            }

            fieldNode.SetDependencySnapshot(snapshot);
            rejectionReason = null;
            return true;
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
            return owningSectionByFieldAlias.TryGetValue(fieldAlias, out owningSectionAlias);
        }


        public MergeDecision ApplyProposal(Proposal proposal)
        {
            if (!fieldsByAlias.TryGetValue(proposal.Alias, out FieldNode? fieldNode))
            {
                return MergeDecision.Deny($"Field with alias '{proposal.Alias}' does not exist.", currentStatus: FieldStatus.Empty);
            }

            fieldNode.AddProposal(proposal);

            FieldValue currentValue = fieldNode.CurrentValue;
            MergeDecision decision = MergeRules.DecideApply(currentValue, proposal);

            if (!decision.IsAllowed)
            {
                return decision;
            }

            FieldValue updatedValue = currentValue.With(
                status: decision.ResultingStatus,
                value: proposal.ProposedValue,
                source: proposal.Source,
                confidence: proposal.Confidence);

            fieldNode.SetCurrentValue(updatedValue);

            return decision;
        }

        private void ValidateSectionGraph()
        {
            int rootSectionCount = 0;

            foreach (SectionDescriptor sectionDescriptor in sectionsByAlias.Values)
            {
                if (sectionDescriptor.ParentSectionAlias is null)
                {
                    rootSectionCount++;
                }
                else
                {
                    SectionAlias parentSectionAlias = sectionDescriptor.ParentSectionAlias.Value;

                    if (!sectionsByAlias.ContainsKey(parentSectionAlias))
                    {
                        throw new InvalidOperationException(
                            $"Section '{sectionDescriptor.SectionAlias}' references unknown parent section '{parentSectionAlias}'.");
                    }
                }

                foreach (SectionAlias childSectionAlias in sectionDescriptor.ChildSectionAliases)
                {
                    if (!sectionsByAlias.TryGetValue(childSectionAlias, out SectionDescriptor? childSectionDescriptor))
                    {
                        throw new InvalidOperationException(
                            $"Section '{sectionDescriptor.SectionAlias}' references unknown child section '{childSectionAlias}'.");
                    }

                    if (!childSectionDescriptor.ParentSectionAlias.HasValue ||
                        !childSectionDescriptor.ParentSectionAlias.Value.Equals(sectionDescriptor.SectionAlias))
                    {
                        throw new InvalidOperationException(
                            $"Child section '{childSectionAlias}' must have ParentSectionAlias '{sectionDescriptor.SectionAlias}'.");
                    }
                }
            }

            if (rootSectionCount != 1)
            {
                throw new InvalidOperationException(
                    $"Internal model must contain exactly one root section. Found {rootSectionCount}.");
            }

            foreach (SectionAlias sectionAlias in sectionsByAlias.Keys)
            {
                EnsureNoParentCycle(sectionAlias);
            }
        }

        private void ValidateAllFieldsAreAssignedToSection()
        {
            foreach (FieldAlias fieldAlias in fieldsByAlias.Keys)
            {
                if (!owningSectionByFieldAlias.ContainsKey(fieldAlias))
                {
                    throw new InvalidOperationException(
                        $"Field alias '{fieldAlias}' is not assigned to any section. " +
                        "All fields must belong to exactly one section.");
                }
            }
        }

        private void EnsureNoParentCycle(SectionAlias startSectionAlias)
        {
            var visitedAliases = new HashSet<SectionAlias>();
            SectionAlias? currentAlias = startSectionAlias;

            while (currentAlias.HasValue)
            {
                SectionAlias currentSectionAlias = currentAlias.Value;

                if (!visitedAliases.Add(currentSectionAlias))
                {
                    throw new InvalidOperationException($"Cycle detected in section graph at '{currentSectionAlias}'.");
                }

                SectionDescriptor currentSectionDescriptor = sectionsByAlias[currentSectionAlias];
                currentAlias = currentSectionDescriptor.ParentSectionAlias;
            }
        }



    } 
}
