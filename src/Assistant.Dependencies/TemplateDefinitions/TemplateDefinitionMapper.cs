using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Assistant.Core.Model;

namespace Assistant.Dependencies.TemplateDefinitions;

internal sealed class TemplateDefinitionMapper
{
    public TemplateDefinition Map(
        DefinitionTemplate template,
        ICollection<TemplateDefinitionWarning>? warnings = null)
    {
        var fieldAliases = new List<FieldAlias>();
        var descriptorsByAlias = new Dictionary<FieldAlias, FieldDescriptor>();
        var sections = new List<SectionDescriptor>();

        var sectionAliasMap = new Dictionary<string, SectionAlias>(StringComparer.Ordinal);
        var fieldsByParent = new Dictionary<string, List<DefinitionField>>(StringComparer.Ordinal);

        if (template.Sections != null)
        {
            foreach (DefinitionSection section in template.Sections)
            {
                if (!SectionAlias.TryCreate(section.Alias, out SectionAlias sectionAlias, out string? error))
                {
                    warnings?.Add(new TemplateDefinitionWarning(
                        "DEF_INVALID_SECTION_ALIAS",
                        $"Invalid section alias '{section.Alias}'. {error}",
                        template.Key));
                    continue;
                }

                if (sectionAliasMap.ContainsKey(section.Alias))
                {
                    warnings?.Add(new TemplateDefinitionWarning(
                        "DEF_DUPLICATE_SECTION_ALIAS",
                        $"Duplicate section alias '{section.Alias}'. Later occurrence will be ignored.",
                        template.Key));
                    continue;
                }

                sectionAliasMap.Add(section.Alias, sectionAlias);
            }
        }

        if (template.Fields != null)
        {
            foreach (DefinitionField field in template.Fields)
            {
                if (!FieldAlias.TryCreate(field.Alias, out FieldAlias fieldAlias, out string? error))
                {
                    warnings?.Add(new TemplateDefinitionWarning(
                        "DEF_INVALID_FIELD_ALIAS",
                        $"Invalid field alias '{field.Alias}'. {error}",
                        template.Key));
                    continue;
                }

                if (descriptorsByAlias.ContainsKey(fieldAlias))
                {
                    warnings?.Add(new TemplateDefinitionWarning(
                        "DEF_DUPLICATE_FIELD_ALIAS",
                        $"Duplicate field alias '{field.Alias}'. Later occurrence will be ignored.",
                        template.Key));
                    continue;
                }

                FillMode fillMode = FillMode.None;

                if (field.Fill != null)
                {
                    if (field.Fill.Deterministic)
                        fillMode |= FillMode.Deterministic;

                    if (field.Fill.Ai)
                        fillMode |= FillMode.Ai;

                    if (field.Fill.Chatbot)
                        fillMode |= FillMode.Chatbot;
                }

                var descriptor = new FieldDescriptor(
                    alias: fieldAlias,
                    fillModes: fillMode,
                    isRequired: field.Required ?? false,
                    displayName: field.LabelCs,
                    queryHints: field.QueryHints,
                    instructions: field.Instructions,
                    valueTypeHint: field.ValueType,
                    contextRequirement: null);

                descriptorsByAlias.Add(fieldAlias, descriptor);
                fieldAliases.Add(fieldAlias);

                if (string.IsNullOrWhiteSpace(field.Parent))
                {
                    warnings?.Add(new TemplateDefinitionWarning(
                        "DEF_FIELD_MISSING_PARENT",
                        $"Field '{field.Alias}' has no parent section and will not belong to any section.",
                        template.Key));
                    continue;
                }

                if (!fieldsByParent.TryGetValue(field.Parent, out List<DefinitionField>? fieldList))
                {
                    fieldList = new List<DefinitionField>();
                    fieldsByParent.Add(field.Parent, fieldList);
                }

                fieldList.Add(field);
            }
        }

        if (template.Sections != null)
        {
            foreach (DefinitionSection section in template.Sections)
            {
                if (!sectionAliasMap.TryGetValue(section.Alias, out SectionAlias sectionAlias))
                    continue;

                SectionAlias? parentAlias = null;

                if (!string.IsNullOrWhiteSpace(section.Parent))
                {
                    if (sectionAliasMap.TryGetValue(section.Parent, out SectionAlias resolvedParent))
                    {
                        parentAlias = resolvedParent;
                    }
                    else
                    {
                        warnings?.Add(new TemplateDefinitionWarning(
                            "DEF_UNKNOWN_SECTION_PARENT",
                            $"Section '{section.Alias}' references unknown parent '{section.Parent}'. It will be treated as root.",
                            template.Key));
                    }
                }

                var sectionFieldAliases = new List<FieldAlias>();

                if (fieldsByParent.TryGetValue(section.Alias, out List<DefinitionField>? ownedFields))
                {
                    foreach (DefinitionField field in ownedFields)
                    {
                        if (FieldAlias.TryCreate(field.Alias, out FieldAlias fieldAlias, out _)
                            && descriptorsByAlias.ContainsKey(fieldAlias))
                        {
                            sectionFieldAliases.Add(fieldAlias);
                        }
                    }
                }

                sections.Add(new SectionDescriptor(
                    sectionAlias: sectionAlias,
                    parentSectionAlias: parentAlias,
                    fieldAliases: sectionFieldAliases,
                    queryHints: section.QueryHints,
                    displayName: section.LabelCs,
                    orderIndex: section.OrderIndex));
            }
        }

        return new TemplateDefinition(
            templateVersion: template.Key,
            fieldAliases: fieldAliases,
            descriptorsByAlias: descriptorsByAlias,
            sections: sections);
    }
}