using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Assistant.Core.Model;
using Assistant.Dependencies.TemplateDefinitions;
using Assistant.Pipeline.Initiation;
using Microsoft.Extensions.DependencyInjection;
using Assistant.Parser.OpenXml;

public static class PrefaceBasicInfoSectionExperiment
{
    public static async Task RunAsync(ServiceProvider services)
    {
        LogStep("Starting Preface Basic Info section experiment.");

        ContextAwareInitiationOrchestrator orchestrator =
            services.GetRequiredService<ContextAwareInitiationOrchestrator>();

        // Office website used as web context source (crawler + retrieval).
        // You can override this in your runner by passing a different URL.
        string officeIdentifier = "https://www.mmr.gov.cz/";

        ITemplateDefinitionProvider definitionProvider =
            services.GetRequiredService<ITemplateDefinitionProvider>();

        LogStep($"Resolved template definition provider: {definitionProvider.GetType().FullName}.");

        if (definitionProvider is TemplateDefinitionProvider yamlDefinitionProvider)
        {
            PrintProviderDiagnostics(yamlDefinitionProvider);
        }

        ITemplateSdtParser parser =
        services.GetRequiredService<ITemplateSdtParser>();

        string pathToDocx = ResolveWorkingTemplatePath();

        LogStep($"Opening document '{pathToDocx}'.");

        using FileStream stream = File.OpenRead(pathToDocx);

        LogStep("Parsing SDTs from document. This can take a while for large files.");

        TemplateInstance instance =
            await parser.ParseAsync(stream, CancellationToken.None);

        LogStep($"Parsing finished. Parsed {instance.Bindings.Count} binding(s), diagnostics: {instance.Diagnostics.Count}.");

        TemplateDefinition definition = ResolveDefinition(definitionProvider, instance);

        LogStep(
            $"Template definition '{definition.TemplateVersion}' selected. Sections: {definition.Sections.Count}, fields: {definition.FieldAliases.Count}.");

        PrintDefinitionSummary(definition);

        IReadOnlyList<SectionDescriptor> sectionsToGenerate = definition.Sections
            .Where(section => section.FieldAliases.Count > 0)
            .OrderBy(section => section.OrderIndex ?? int.MaxValue)
            .ToArray();

        LogStep($"Selected {sectionsToGenerate.Count} section(s) with field bindings for generation.");

        Console.WriteLine("=== PARSED SDTs ===");

        foreach (FieldBinding binding in instance.Bindings)
        {
            Console.WriteLine($"Alias: {binding.Alias.Value}");
            Console.WriteLine($"  SDT Id: {binding.SdtId}");
            Console.WriteLine($"  Occurrence: {binding.OccurrenceIndex}");
            Console.WriteLine($"  ContentKind: {binding.ContentKind}");
            Console.WriteLine();
        }

        if (instance.Diagnostics.Count > 0)
        {
            Console.WriteLine("=== PARSER DIAGNOSTICS ===");

            foreach (SdtParseDiagnostic diagnostic in instance.Diagnostics)
            {
                Console.WriteLine($"{diagnostic.Code}: {diagnostic.Message}");
            }

            Console.WriteLine();
        }

        var runtimeFactory = new TemplateRuntimeFactory(new InternalModelRuntimeBuilder());

        LogStep("Building Internal Model from parsed template and definition.");

        TemplateRuntimeBuildResult buildResult = runtimeFactory.BuildRuntime(instance, definition);

        if (!buildResult.IsSuccess)
        {
            foreach (SdtParseDiagnostic diagnostic in buildResult.Diagnostics)
            {
                Console.WriteLine($"{diagnostic.Code}: {diagnostic.Message}");
            }

            throw new InvalidOperationException("Failed to build InternalModelRuntime.");
        }

        InternalModelRuntime runtime = buildResult.Runtime!;

        LogStep("Internal Model build successful.");

        foreach (SectionDescriptor section in sectionsToGenerate)
        {
            string sectionContextText = BuildSectionContext(section, definition);
            FieldAlias[] sectionFieldAliases = section.FieldAliases.ToArray();

            LogStep(
                $"Starting generation for section '{section.SectionAlias.Value}' with {sectionFieldAliases.Length} field(s).");

            await orchestrator.GenerateSectionAsync(
                internalModelRuntime: runtime,
                sectionFieldAliases: sectionFieldAliases,
                officeIdentifier: officeIdentifier,
                sectionContextText: sectionContextText,
                cancellationToken: CancellationToken.None);

            LogStep($"Section '{section.SectionAlias.Value}' generation completed.");
        }

        PrintResults(runtime.Model, definition);

        LogStep("Finished.");
    }

    private static TemplateDefinition ResolveDefinition(
        ITemplateDefinitionProvider definitionProvider,
        TemplateInstance instance)
    {
        string[] parsedAliases = instance.Bindings
            .Select(binding => binding.Alias.Value)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (definitionProvider is TemplateDefinitionProvider yamlDefinitionProvider)
        {
            TemplateDefinition? bestDefinition = yamlDefinitionProvider.Definitions
                .Select(candidateDefinition => new
                {
                    Definition = candidateDefinition,
                    MatchedAliases = parsedAliases.Count(alias =>
                        candidateDefinition.FieldAliases.Any(fieldAlias => string.Equals(fieldAlias.Value, alias, StringComparison.Ordinal))),
                    UnknownAliases = parsedAliases.Count(alias =>
                        candidateDefinition.FieldAliases.All(fieldAlias => !string.Equals(fieldAlias.Value, alias, StringComparison.Ordinal)))
                })
                .OrderBy(candidate => candidate.UnknownAliases)
                .ThenByDescending(candidate => candidate.MatchedAliases)
                .Select(candidate => candidate.Definition)
                .FirstOrDefault();

            if (bestDefinition is null)
            {
                throw new InvalidOperationException("The template definition provider did not load any definitions.");
            }

            return bestDefinition;
        }

        string? requestedTemplateVersion = Environment.GetEnvironmentVariable("ASSISTANT_TEMPLATE_VERSION");

        if (string.IsNullOrWhiteSpace(requestedTemplateVersion))
        {
            throw new InvalidOperationException(
                "Template version could not be resolved automatically for the current provider. Set ASSISTANT_TEMPLATE_VERSION.");
        }

        if (!definitionProvider.TryGetDefinition(
                requestedTemplateVersion,
                out TemplateDefinition resolvedDefinition,
                out string? error))
        {
            throw new InvalidOperationException(error);
        }

        return resolvedDefinition;
    }

    private static string BuildSectionContext(
        SectionDescriptor section,
        TemplateDefinition definition)
    {
        List<string> contextLines = new();

        if (!string.IsNullOrWhiteSpace(section.DisplayName))
        {
            contextLines.Add($"Sekce: {section.DisplayName}");
        }

        if (section.QueryHints is { Count: > 0 })
        {
            contextLines.Add($"Nápovědy pro dotaz: {string.Join(", ", section.QueryHints)}");
        }

        IReadOnlyList<string> fieldNames = section.FieldAliases
            .Select(alias => definition.DescriptorsByAlias[alias].DisplayName ?? alias.Value)
            .ToArray();

        if (fieldNames.Count > 0)
        {
            contextLines.Add($"Pole v sekci: {string.Join(", ", fieldNames)}");
        }

        return string.Join(Environment.NewLine, contextLines);
    }

    private static string ResolveWorkingTemplatePath()
    {
        string? currentDirectory = AppContext.BaseDirectory;

        while (!string.IsNullOrWhiteSpace(currentDirectory))
        {
            string[] candidates =
            {
                Path.Combine(currentDirectory, "working_template.docx"),
                Path.Combine(currentDirectory, "src", "working_template.docx"),
                Path.Combine(currentDirectory, "docs", "working_template.docx")
            };

            foreach (string candidatePath in candidates)
            {
                if (File.Exists(candidatePath))
                {
                    return candidatePath;
                }
            }

            DirectoryInfo? parentDirectory = Directory.GetParent(currentDirectory);
            if (parentDirectory is null)
            {
                break;
            }

            currentDirectory = parentDirectory.FullName;
        }

        throw new FileNotFoundException(
            "Could not locate working_template.docx starting from the application base directory.");
    }

    private static void PrintProviderDiagnostics(TemplateDefinitionProvider provider)
    {
        Console.WriteLine("=== TEMPLATE DEFINITION PROVIDER ===");
        Console.WriteLine($"Provider: {provider.GetType().FullName}");
        Console.WriteLine($"SourcePath: {provider.SourcePath}");
        Console.WriteLine($"Warnings: {provider.Warnings.Count}");

        foreach (TemplateDefinitionWarning warning in provider.Warnings)
        {
            Console.WriteLine($"  {warning}");
        }

        Console.WriteLine();
    }

    private static void PrintDefinitionSummary(TemplateDefinition definition)
    {
        Console.WriteLine("=== TEMPLATE DEFINITION SUMMARY ===");
        Console.WriteLine($"TemplateVersion: {definition.TemplateVersion}");
        Console.WriteLine($"Sections: {definition.Sections.Count}");

        foreach (SectionDescriptor section in definition.Sections)
        {
            string parentAlias = section.ParentSectionAlias?.Value ?? "<root>";

            Console.WriteLine($"  Section: {section.SectionAlias.Value}");
            Console.WriteLine($"    Parent: {parentAlias}");
            Console.WriteLine($"    DisplayName: {section.DisplayName ?? "<none>"}");
            Console.WriteLine($"    FieldCount: {section.FieldAliases.Count}");
        }

        Console.WriteLine($"Fields: {definition.FieldAliases.Count}");

        foreach (FieldAlias alias in definition.FieldAliases)
        {
            FieldDescriptor descriptor = definition.DescriptorsByAlias[alias];
            Console.WriteLine($"  Field: {alias.Value}");
            Console.WriteLine($"    DisplayName: {descriptor.DisplayName ?? "<none>"}");
            Console.WriteLine($"    FillModes: {descriptor.FillModes}");
            Console.WriteLine($"    Required: {descriptor.IsRequired}");
            Console.WriteLine($"    ValueType: {descriptor.ValueTypeHint ?? "<none>"}");
        }

        Console.WriteLine();
    }

    private static void PrintResults(
        InternalModel model,
        TemplateDefinition definition)
    {
        Console.WriteLine("=== TEMPLATE RESULTS ===");

        foreach (SectionDescriptor section in definition.Sections.Where(section => section.FieldAliases.Count > 0))
        {
            Console.WriteLine($"Section: {section.SectionAlias.Value}");
            Console.WriteLine($"  DisplayName: {section.DisplayName ?? "<none>"}");

            foreach (FieldAlias alias in section.FieldAliases)
            {
                FieldNode node = model.GetField(alias);
                Proposal? lastProposal = node.ProposalHistory.LastOrDefault();

                Console.WriteLine($"  Field: {alias.Value}");
                Console.WriteLine($"    Status: {node.CurrentValue.Status}");

                if (lastProposal is null)
                {
                    Console.WriteLine("    No proposal generated.");
                    Console.WriteLine();
                    continue;
                }

                Console.WriteLine($"    Value: {lastProposal.ProposedValue}");
                Console.WriteLine($"    Confidence: {lastProposal.Confidence}");
                Console.WriteLine($"    Explanation: {lastProposal.Explanation}");
            }

            Console.WriteLine();
        }
    }

    private static void LogStep(string message)
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {message}");
    }
}
