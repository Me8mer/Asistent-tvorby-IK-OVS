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
namespace Assistant.Core.Model.Sections
{
    public sealed class SectionDescriptor
    {
        public SectionAlias SectionAlias { get; }
        public SectionAlias? ParentSectionAlias { get; }

        public IReadOnlyList<FieldAlias> FieldAliases { get; }
        public IReadOnlyList<string>? QueryHints { get; }
        public string? DisplayName { get; }
        public int? OrderIndex { get; }

        public SectionDescriptor(
            SectionAlias sectionAlias,
            SectionAlias? parentSectionAlias,
            IReadOnlyList<FieldAlias> fieldAliases,
            IReadOnlyList<string>? queryHints,
            string? displayName,
            int? orderIndex)
        {
            SectionAlias = sectionAlias;
            ParentSectionAlias = parentSectionAlias;

            FieldAliases = fieldAliases ?? throw new ArgumentNullException(nameof(fieldAliases));

            QueryHints = queryHints;
            DisplayName = displayName;
            OrderIndex = orderIndex;
        }
    }
}