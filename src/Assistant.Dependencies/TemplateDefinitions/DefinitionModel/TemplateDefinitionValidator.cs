using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Assistant.Core.Model;

namespace Assistant.Dependencies.TemplateDefinitions;

internal sealed class TemplateDefinitionValidator
{
    public List<TemplateDefinitionWarning> Validate(DefinitionTemplate template)
    {
        var warnings = new List<TemplateDefinitionWarning>();

        var knownSectionAliases = new HashSet<string>(StringComparer.Ordinal);
        var knownFieldAliases = new HashSet<string>(StringComparer.Ordinal);

        if (template.Sections != null)
        {
            foreach (DefinitionSection section in template.Sections)
            {
                if (string.IsNullOrWhiteSpace(section.Alias))
                {
                    warnings.Add(new TemplateDefinitionWarning(
                        "DEF_SECTION_EMPTY_ALIAS",
                        "Section has empty alias.",
                        template.Key));
                    continue;
                }

                if (!knownSectionAliases.Add(section.Alias))
                {
                    warnings.Add(new TemplateDefinitionWarning(
                        "DEF_DUPLICATE_SECTION_ALIAS",
                        $"Duplicate section alias '{section.Alias}'.",
                        template.Key));
                }
            }

            foreach (DefinitionSection section in template.Sections)
            {
                if (string.IsNullOrWhiteSpace(section.Parent))
                    continue;

                if (!knownSectionAliases.Contains(section.Parent))
                {
                    warnings.Add(new TemplateDefinitionWarning(
                        "DEF_UNKNOWN_SECTION_PARENT",
                        $"Section '{section.Alias}' references unknown parent section '{section.Parent}'.",
                        template.Key));
                }
            }
        }

        if (template.Fields != null)
        {
            foreach (DefinitionField field in template.Fields)
            {
                if (string.IsNullOrWhiteSpace(field.Alias))
                {
                    warnings.Add(new TemplateDefinitionWarning(
                        "DEF_FIELD_EMPTY_ALIAS",
                        "Field has empty alias.",
                        template.Key));
                    continue;
                }

                if (!knownFieldAliases.Add(field.Alias))
                {
                    warnings.Add(new TemplateDefinitionWarning(
                        "DEF_DUPLICATE_FIELD_ALIAS",
                        $"Duplicate field alias '{field.Alias}'. Later occurrence will be ignored.",
                        template.Key));
                }

                if (string.IsNullOrWhiteSpace(field.Parent))
                {
                    warnings.Add(new TemplateDefinitionWarning(
                        "DEF_FIELD_MISSING_PARENT",
                        $"Field '{field.Alias}' has no parent section.",
                        template.Key));
                    continue;
                }

                if (!knownSectionAliases.Contains(field.Parent))
                {
                    warnings.Add(new TemplateDefinitionWarning(
                        "DEF_UNKNOWN_FIELD_PARENT",
                        $"Field '{field.Alias}' references unknown parent section '{field.Parent}'.",
                        template.Key));
                }
            }
        }

        return warnings;
    }
}