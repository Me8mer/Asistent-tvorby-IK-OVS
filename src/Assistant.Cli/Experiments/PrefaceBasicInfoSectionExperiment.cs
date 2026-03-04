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
        ContextAwareInitiationOrchestrator orchestrator =
            services.GetRequiredService<ContextAwareInitiationOrchestrator>();

        // Office website used as web context source (crawler + retrieval).
        // You can override this in your runner by passing a different URL.
        string officeIdentifier = "https://www.mmr.gov.cz/";
        string templateVersion = "preface-basic-info.v01";

        var definitionProvider = new HardcodedPrefaceBasicInfoDefinitionProvider();

        if (!definitionProvider.TryGetDefinition(templateVersion, out TemplateDefinition definition, out string? definitionError))
        {
            throw new InvalidOperationException(definitionError);
        }

        FieldAlias[] sectionFieldAliases = definition.FieldAliases.ToArray();

        ITemplateSdtParser parser =
        services.GetRequiredService<ITemplateSdtParser>();

        string pathToDocx = "working_template.docx";

        using FileStream stream = File.OpenRead(pathToDocx);

        TemplateInstance instance =
            await parser.ParseAsync(stream, CancellationToken.None);

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

        // 6) Section context (raw text from the document)
        // This is combined with the retrieved web context by the orchestrator.
        string sectionContextText = "";

        // 7) Run AI for the whole section with web context injection
        await orchestrator.GenerateSectionAsync(
            internalModelRuntime: runtime,
            sectionFieldAliases: sectionFieldAliases,
            officeIdentifier: officeIdentifier,
            sectionContextText: sectionContextText,
            cancellationToken: CancellationToken.None);

        // 8) Print results
        PrintResults(runtime.Model, sectionFieldAliases);
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
}
