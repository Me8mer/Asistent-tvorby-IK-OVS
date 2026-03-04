using System;
using System.Collections.Generic;
using System.Linq;
using Assistant.Core.Model;


public sealed class HardcodedPrefaceBasicInfoDefinitionProvider : ITemplateDefinitionProvider
{
    public bool TryGetDefinition(
        string templateVersion,
        out TemplateDefinition definition,
        out string? error)
    {
        error = null;

        if (!string.Equals(templateVersion, "preface-basic-info.v01", StringComparison.OrdinalIgnoreCase))
        {
            definition = null!;
            error = $"Unknown template version '{templateVersion}'.";
            return false;
        }

        FieldAlias[] sectionFieldAliases =
        {
            PrefaceBasicInfoAliases.OrgName,
            PrefaceBasicInfoAliases.OrgIco,
            PrefaceBasicInfoAliases.OrgType,
            PrefaceBasicInfoAliases.OrgAddress
        };

        IReadOnlyList<FieldDescriptor> descriptors = PrefaceBasicInfoDescriptors.Create();
        Dictionary<FieldAlias, FieldDescriptor> descriptorsByAlias =
            descriptors.ToDictionary(descriptor => descriptor.Alias);

        SectionAlias rootSectionAlias = new("IKOVS_ROOT");
        SectionAlias basicInfoSectionAlias = new(PrefaceBasicInfoAliases.Section.Value);

        List<SectionDescriptor> sections = new()
        {
            new SectionDescriptor(
                sectionAlias: rootSectionAlias,
                parentSectionAlias: null,
                fieldAliases: Array.Empty<FieldAlias>(),
                queryHints: null,
                displayName: "IKOVS",
                orderIndex: 0),

            new SectionDescriptor(
                sectionAlias: basicInfoSectionAlias,
                parentSectionAlias: rootSectionAlias,
                fieldAliases: sectionFieldAliases,
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

        definition = new TemplateDefinition(
            templateVersion: "preface-basic-info.v01",
            fieldAliases: sectionFieldAliases,
            descriptorsByAlias: descriptorsByAlias,
            sections: sections);

        return true;
    }
}
