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
        InitiationOrchestrator orchestrator =
            services.GetRequiredService<InitiationOrchestrator>();

        // 1) Section aliases
        FieldAlias[] sectionFieldAliases =
        {
            PrefaceBasicInfoAliases.OrgName,
            PrefaceBasicInfoAliases.OrgIco,
            PrefaceBasicInfoAliases.OrgType,
            PrefaceBasicInfoAliases.OrgAddress
        };

        // 2) Descriptors
        IReadOnlyList<FieldDescriptor> descriptors =
            PrefaceBasicInfoDescriptors.Create();

        Dictionary<FieldAlias, FieldDescriptor> descriptorsByAlias =
            descriptors.ToDictionary(d => d.Alias);

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
                queryHints: new[] { "preface basic info", "základní údaje" },
                displayName: "Základní údaje",
                orderIndex: 10)
        };
        // 3) Fake bindings (for now)
        Dictionary<FieldAlias, FieldBinding> bindingsByAlias =
            sectionFieldAliases.ToDictionary(a => a, CreateMockBinding);

        // 4) Internal model = simulated parsed document
        InternalModel model = new InternalModel(
            templateVersion: "preface-basic-info.v01",
            aliases: sectionFieldAliases,
            bindingsByAlias: bindingsByAlias,
            descriptorsByAlias: descriptorsByAlias,
            sections: sections);

        // 5) Section context (raw text)
        string sectionContext = """
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

        // 6) Run AI for the whole section
        await orchestrator.GenerateSectionAsync(
            internalModel: model,
            sectionFieldAliases: sectionFieldAliases,
            sectionContextText: sectionContext,
            cancellationToken: CancellationToken.None);

        // 7) Print results
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



