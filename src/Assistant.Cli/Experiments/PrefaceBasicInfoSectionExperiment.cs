using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Assistant.Core.Model;
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
        string templateVersion = "preface-basic-info.v01";

        LogStep($"Loading template definition '{templateVersion}'.");

        var definitionProvider = new HardcodedPrefaceBasicInfoDefinitionProvider();

        if (!definitionProvider.TryGetDefinition(templateVersion, out TemplateDefinition definition, out string? definitionError))
        {
            throw new InvalidOperationException(definitionError);
        }

        LogStep("Template definition loaded successfully.");

        FieldAlias[] sectionFieldAliases = definition.FieldAliases.ToArray();

        ITemplateSdtParser parser =
        services.GetRequiredService<ITemplateSdtParser>();

        string pathToDocx = "working_template.docx";

        LogStep($"Opening document '{pathToDocx}'.");

        using FileStream stream = File.OpenRead(pathToDocx);

        LogStep("Parsing SDTs from document. This can take a while for large files.");

        TemplateInstance instance =
            await parser.ParseAsync(stream, CancellationToken.None);

        LogStep($"Parsing finished. Parsed {instance.Bindings.Count} binding(s), diagnostics: {instance.Diagnostics.Count}.");

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

        // 6) Section context (raw text from the document)
        // This is combined with the retrieved web context by the orchestrator.
        string sectionContextText = "";

        // 7) Run AI for the whole section with web context injection
        LogStep("Starting AI section generation with web context retrieval.");

        await orchestrator.GenerateSectionAsync(
            internalModelRuntime: runtime,
            sectionFieldAliases: sectionFieldAliases,
            officeIdentifier: officeIdentifier,
            sectionContextText: sectionContextText,
            cancellationToken: CancellationToken.None);

        LogStep("AI section generation completed.");

        // 8) Print results
        PrintResults(runtime.Model, sectionFieldAliases);

        LogStep("Finished.");
    }

    private static FieldBinding CreateMockBinding(FieldAlias alias)
    {
        return new FieldBinding(
            alias: alias,
            sdtId: null,
            occurrenceIndex: null,
            locationHint: "HARDCODED_PREFACE_BASIC_INFO",
            contentKind: FieldContentKind.TableCell);
    }

    private static void PrintResults(
        InternalModel model,
        IEnumerable<FieldAlias> aliases)
    {
        Console.WriteLine("=== PREFACE BASIC INFO RESULTS ===");

        foreach (FieldAlias alias in aliases)
        {
            FieldNode node = model.GetField(alias);
            Proposal? lastProposal = node.ProposalHistory.LastOrDefault();

            Console.WriteLine(alias.Value);
            Console.WriteLine($"  Status: {node.CurrentValue.Status}");

            if (lastProposal is null)
            {
                Console.WriteLine("  No proposal generated.");
                Console.WriteLine();
                continue;
            }

            Console.WriteLine($"  Value: {lastProposal.ProposedValue}");
            Console.WriteLine($"  Confidence: {lastProposal.Confidence}");
            Console.WriteLine($"  Explanation: {lastProposal.Explanation}");
            Console.WriteLine();
        }
    }

    private static void LogStep(string message)
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {message}");
    }
}
