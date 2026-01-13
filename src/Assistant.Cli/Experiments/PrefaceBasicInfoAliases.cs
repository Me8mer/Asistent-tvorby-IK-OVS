using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Assistant.Core.Model;
using Assistant.Pipeline.Initiation;
using Microsoft.Extensions.DependencyInjection;

public static class PrefaceBasicInfoDescriptors
{
    public static IReadOnlyList<FieldDescriptor> Create()
    {
        return new[]
        {
            new FieldDescriptor(
                PrefaceBasicInfoAliases.OrgName,
                FillMode.Deterministic | FillMode.Ai | FillMode.Chatbot,
                isRequired: true,
                displayName: "Název orgánu veřejné správy",
                instructions: null,
                valueTypeHint: "string",
                contextRequirement: null),

            new FieldDescriptor(
                PrefaceBasicInfoAliases.OrgIco,
                FillMode.Deterministic,
                isRequired: true,
                displayName: "IČO",
                instructions: null,
                valueTypeHint: "ico",
                contextRequirement: null),

            new FieldDescriptor(
                PrefaceBasicInfoAliases.OrgType,
                FillMode.Deterministic | FillMode.Ai,
                isRequired: true,
                displayName: "Typ organizace",
                instructions: null,
                valueTypeHint: "enum",
                contextRequirement: null),

            new FieldDescriptor(
                PrefaceBasicInfoAliases.OrgAddress,
                FillMode.Deterministic | FillMode.Ai,
                isRequired: true,
                displayName: "Adresa sídla",
                instructions: null,
                valueTypeHint: "address",
                contextRequirement: null),
        };
    }
}



public static class PrefaceBasicInfoAliases
{
    // Section
    public static readonly FieldAlias Section =
        new("IKOVS_PREFACE_BASIC-INFO");

    // Table fields
    public static readonly FieldAlias OrgName =
        new("IKOVS_PREFACE_BASIC-INFO_ORG_NAME");

    public static readonly FieldAlias OrgIco =
        new("IKOVS_PREFACE_BASIC-INFO_ORG_ICO");

    public static readonly FieldAlias OrgType =
        new("IKOVS_PREFACE_BASIC-INFO_ORG_TYPE");

    public static readonly FieldAlias OrgAddress =
        new("IKOVS_PREFACE_BASIC-INFO_ORG_ADDRESS");

}


