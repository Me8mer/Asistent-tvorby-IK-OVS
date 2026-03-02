using System;
using System.Collections.Generic;

namespace Assistant.Core.Model
{
    public static class SectionDescriptorNormalizer
    {
        public static SectionDescriptor Normalize(SectionDescriptor sectionDescriptor)
        {
            if (sectionDescriptor is null)
            {
                throw new ArgumentNullException(nameof(sectionDescriptor));
            }

            IReadOnlyList<string>? normalizedQueryHints = NormalizeQueryHints(sectionDescriptor.QueryHints);

            if (ReferenceEquals(normalizedQueryHints, sectionDescriptor.QueryHints))
            {
                return sectionDescriptor;
            }

            return new SectionDescriptor(
                sectionAlias: sectionDescriptor.SectionAlias,
                parentSectionAlias: sectionDescriptor.ParentSectionAlias,
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
    }
}