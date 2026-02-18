using System;
using System.Collections.Generic;

namespace Assistant.Core.Model
{
    public sealed class SectionDescriptor
    {
        public SectionAlias SectionAlias { get; }
        public SectionAlias? ParentSectionAlias { get; }
        public IReadOnlyList<SectionAlias> ChildSectionAliases { get; }
        public IReadOnlyList<FieldAlias> FieldAliases { get; }
        public IReadOnlyList<string>? QueryHints { get; }
        public string? DisplayName { get; }
        public int? OrderIndex { get; }

        public SectionDescriptor(
            SectionAlias sectionAlias,
            SectionAlias? parentSectionAlias,
            IReadOnlyList<SectionAlias> childSectionAliases,
            IReadOnlyList<FieldAlias> fieldAliases,
            IReadOnlyList<string>? queryHints,
            string? displayName,
            int? orderIndex)
        {
            SectionAlias = sectionAlias;
            ParentSectionAlias = parentSectionAlias;
            ChildSectionAliases = childSectionAliases ?? throw new ArgumentNullException(nameof(childSectionAliases));
            FieldAliases = fieldAliases ?? throw new ArgumentNullException(nameof(fieldAliases));
            QueryHints = queryHints;
            DisplayName = displayName;
            OrderIndex = orderIndex;
        }
    }
}
