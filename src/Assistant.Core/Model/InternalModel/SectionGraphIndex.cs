using System;
using System.Collections.Generic;

/// <summary>
/// Represents hierarchy of sections (tree/forest).
/// 
/// Provides:
/// - root sections
/// - child lookup
/// 
/// Validates:
/// - no cycles
/// - valid parent references
/// </summary>
namespace Assistant.Core.Model.InternalModel
{
    public sealed class SectionGraphIndex
    {
        private readonly Dictionary<SectionAlias, List<SectionAlias>> childrenBySectionAlias;
        private readonly List<SectionAlias> rootSectionAliases;

        public IReadOnlyList<SectionAlias> RootSectionAliases => rootSectionAliases;

        private SectionGraphIndex(
            Dictionary<SectionAlias, List<SectionAlias>> childrenBySectionAlias,
            List<SectionAlias> rootSectionAliases)
        {
            this.childrenBySectionAlias = childrenBySectionAlias;
            this.rootSectionAliases = rootSectionAliases;
        }

        public IReadOnlyList<SectionAlias> GetChildSections(SectionAlias sectionAlias)
        {
            return childrenBySectionAlias.TryGetValue(sectionAlias, out List<SectionAlias>? children)
                ? children
                : Array.Empty<SectionAlias>();
        }

        internal static SectionGraphIndex BuildAndValidate(IReadOnlyDictionary<SectionAlias, SectionDescriptor> sectionsByAlias)
        {
            if (sectionsByAlias is null)
            {
                throw new ArgumentNullException(nameof(sectionsByAlias));
            }

            var childrenIndex = new Dictionary<SectionAlias, List<SectionAlias>>();
            var roots = new List<SectionAlias>();

            foreach (SectionDescriptor sectionDescriptor in sectionsByAlias.Values)
            {
                if (sectionDescriptor.ParentSectionAlias.HasValue)
                {
                    SectionAlias parentAlias = sectionDescriptor.ParentSectionAlias.Value;

                    if (!sectionsByAlias.ContainsKey(parentAlias))
                    {
                        throw new InvalidOperationException(
                            $"Section '{sectionDescriptor.SectionAlias}' references unknown parent section '{parentAlias}'.");
                    }

                    if (!childrenIndex.TryGetValue(parentAlias, out List<SectionAlias>? children))
                    {
                        children = new List<SectionAlias>();
                        childrenIndex.Add(parentAlias, children);
                    }

                    children.Add(sectionDescriptor.SectionAlias);
                }
                else
                {
                    roots.Add(sectionDescriptor.SectionAlias);
                }
            }

            ValidateNoParentCycles(sectionsByAlias);

            return new SectionGraphIndex(childrenIndex, roots);
        }

        private static void ValidateNoParentCycles(IReadOnlyDictionary<SectionAlias, SectionDescriptor> sectionsByAlias)
        {
            foreach (SectionAlias sectionAlias in sectionsByAlias.Keys)
            {
                EnsureNoParentCycle(sectionsByAlias, sectionAlias);
            }
        }

        private static void EnsureNoParentCycle(
            IReadOnlyDictionary<SectionAlias, SectionDescriptor> sectionsByAlias,
            SectionAlias startSectionAlias)
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