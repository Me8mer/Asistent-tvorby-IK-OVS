using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Assistant.Core.Model;
using Assistant.Pipeline.Initiation;
using Microsoft.Extensions.DependencyInjection;

public static class PrefaceBasicInfoSectionExperiment
{
    public static async Task RunAsync(ServiceProvider services)
    {
        ContextAwareInitiationOrchestrator orchestrator =
            services.GetRequiredService<ContextAwareInitiationOrchestrator>();

        // Office website used as web context source (crawler + retrieval).
        // You can override this in your runner by passing a different URL.
        string officeIdentifier = "https://www.mmr.gov.cz/";

        // 1) Section field aliases
        FieldAlias[] sectionFieldAliases =
        {
            PrefaceBasicInfoAliases.OrgName,
            PrefaceBasicInfoAliases.OrgIco,
            PrefaceBasicInfoAliases.OrgType,
            PrefaceBasicInfoAliases.OrgAddress
        };

        // 2) Field descriptors (used by prompt builder)
        IReadOnlyList<FieldDescriptor> descriptors =
            PrefaceBasicInfoDescriptors.Create();

        Dictionary<FieldAlias, FieldDescriptor> descriptorsByAlias =
            descriptors.ToDictionary(descriptor => descriptor.Alias);

        // 3) Section graph (used by SectionQueryBuilder inside CompositeContextProvider)
        SectionAlias rootSectionAlias = new("IKOVS_ROOT");
        SectionAlias basicInfoSectionAlias = new(PrefaceBasicInfoAliases.Section.Value);

        List<SectionDescriptor> sections = new()
        {
            new SectionDescriptor(
                sectionAlias: rootSectionAlias,
                parentSectionAlias: null,
                childSectionAliases: new[] { basicInfoSectionAlias },
                fieldAliases: Array.Empty<FieldAlias>(),
                queryHints: null,
                displayName: "IKOVS",
                orderIndex: 0),

            new SectionDescriptor(
                sectionAlias: basicInfoSectionAlias,
                parentSectionAlias: rootSectionAlias,
                childSectionAliases: Array.Empty<SectionAlias>(),
                fieldAliases: sectionFieldAliases,
                // These query hints are intentionally Czech, because the office websites
                // are Czech, and the query builder should include them.
                queryHints: new[]
                {
                    "základní údaje",
                    "kontakt",
                    "IČO",
                    "adresa sídla",
                    "Ministerstvo pro místní rozvoj"
                },
                displayName: "Základní údaje",
                orderIndex: 10)
        };

        // 4) Fake bindings (simulated parsed document locations)
        Dictionary<FieldAlias, FieldBinding> bindingsByAlias =
            sectionFieldAliases.ToDictionary(alias => alias, CreateMockBinding);

        // 5) Internal model = simulated parsed document
        InternalModel model = new InternalModel(
            templateVersion: "preface-basic-info.v01",
            aliases: sectionFieldAliases,
            bindingsByAlias: bindingsByAlias,
            descriptorsByAlias: descriptorsByAlias,
            sections: sections);

        // 6) Section context (raw text from the document)
        // This is combined with the retrieved web context by the orchestrator.
        string sectionContextText = """
        Základní údaje Informační koncepce pro <<Ministerstvo pro místní rozvoj České republiky>>

        Název orgánu veřejné správy
        <<MISSING>>

        IČO
        <<MISSING>>

        Typ organizace
        Orgán státní správy

        Adresa sídla
        <<MISSING>>
        """;

        // 7) Run AI for the whole section with web context injection
        await orchestrator.GenerateSectionAsync(
            internalModel: model,
            sectionFieldAliases: sectionFieldAliases,
            officeIdentifier: officeIdentifier,
            sectionContextText: sectionContextText,
            cancellationToken: CancellationToken.None);

        // 8) Print results
        PrintResults(model, sectionFieldAliases);
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
