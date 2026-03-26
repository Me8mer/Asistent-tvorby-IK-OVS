using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Assistant.Core.Model;
using Assistant.Core.Model.Templates;

namespace Assistant.Dependencies.TemplateDefinitions;

public sealed class TemplateDefinitionProvider : ITemplateDefinitionProvider
{
    private readonly Dictionary<string, TemplateDefinition> definitions;
    private readonly List<TemplateDefinition> loadedDefinitions;

    public string SourcePath { get; }
    public IReadOnlyList<TemplateDefinition> Definitions => loadedDefinitions;
    public IReadOnlyList<TemplateDefinitionWarning> Warnings { get; }

    public TemplateDefinitionProvider(string yamlPath)
    {
        SourcePath = Path.GetFullPath(yamlPath);
        var source = new TemplateDefinitionSource();
        List<DefinitionTemplate> templates = source.Load(SourcePath);

        var validator = new TemplateDefinitionValidator();
        var mapper = new TemplateDefinitionMapper();

        var warnings = new List<TemplateDefinitionWarning>();
        loadedDefinitions = new List<TemplateDefinition>();
        definitions = new Dictionary<string, TemplateDefinition>(StringComparer.Ordinal);

        foreach (DefinitionTemplate template in templates)
        {
            warnings.AddRange(validator.Validate(template));

            TemplateDefinition definition = mapper.Map(template, warnings);

            if (definitions.ContainsKey(definition.TemplateVersion))
            {
                warnings.Add(new TemplateDefinitionWarning(
                    "DEF_DUPLICATE_TEMPLATE_KEY",
                    $"Duplicate template key '{definition.TemplateVersion}'. Later occurrence will be ignored.",
                    definition.TemplateVersion));
                continue;
            }

            definitions.Add(definition.TemplateVersion, definition);
            loadedDefinitions.Add(definition);
        }

        Warnings = warnings;
    }

    public bool TryGetDefinition( string templateVersion, out TemplateDefinition definition, out string? error)
    {
        if (string.IsNullOrWhiteSpace(templateVersion))
        {
            definition = default!;
            error = "Template version must not be empty.";
            return false;
        }

        if (definitions.TryGetValue(templateVersion, out definition!))
        {
            error = null;
            return true;
        }

        error = $"Template definition '{templateVersion}' not found.";
        return false;
    }
}