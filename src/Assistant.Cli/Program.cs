using System;
using System.Collections.Generic;
using System.Threading;
using Assistant.AI.Abstractions;
using Assistant.AI.Generators;
using Assistant.AI.Models;
using Assistant.AI.Parsing;
using Assistant.AI.Prompting;
using Assistant.AI.Providers;
using Assistant.AI.Routing;
using Assistant.Core.Model;
using Assistant.Pipeline.Initiation;
using Microsoft.Extensions.DependencyInjection;

ServiceCollection services = new ServiceCollection();

RoutingDefaults routingDefaults = new RoutingDefaults(
    initialPrefill: new ModelRouting(
        providerType: LlmProviderType.LocalModel,
        modelName: "fake-model",
        parameters: new LlmGenerationParameters(temperature: 0.0, maxOutputTokens: 256)),
    regenerate: new ModelRouting(
        providerType: LlmProviderType.LocalModel,
        modelName: "fake-model",
        parameters: new LlmGenerationParameters(temperature: 0.2, maxOutputTokens: 256)),
    improve: new ModelRouting(
        providerType: LlmProviderType.LocalModel,
        modelName: "fake-model",
        parameters: new LlmGenerationParameters(temperature: 0.2, maxOutputTokens: 256)));

services.AddSingleton(routingDefaults);
services.AddSingleton<IRoutingPolicy>(serviceProvider => new DefaultRoutingPolicy(routingDefaults));
services.AddSingleton<IPromptBuilder, MinimalPromptBuilder>();
services.AddSingleton<ILlmProvider, FakeLlmProvider>();
services.AddSingleton<IProposalParser>(serviceProvider => new StrictJsonProposalParser(allowModelProposedStatus: false));
services.AddSingleton<IProposalGenerator, DefaultProposalGenerator>();

services.AddSingleton<InitiationOrchestrator>();

ServiceProvider serviceProvider = services.BuildServiceProvider();

FieldAlias alias = new FieldAlias("IKOVS_SEC_CONTACTS_ORG_NAME");

FieldDescriptor descriptor = new FieldDescriptor(
    alias: alias,
    fillModes: FillMode.Ai,
    isRequired: true,
    displayName: "Organization name",
    instructions: "Extract the official organization name. Use the exact spelling from the document.",
    valueTypeHint: "string",
    contextRequirement: new ContextRequirement(ContextScope.SurroundingParagraph, maxCharacters: 1500));

InternalModel internalModel = new InternalModel(
    templateVersion: "0.1",
    aliases: new[] { alias },
    bindingsByAlias: null,
    descriptorsByAlias: new Dictionary<FieldAlias, FieldDescriptor> { { alias, descriptor } });

InitiationOrchestrator orchestrator = serviceProvider.GetRequiredService<InitiationOrchestrator>();

string documentContextText =
    "Header: Municipality of Exampleville\n" +
    "This document is issued by the Municipality of Exampleville for local administration.";

var cancellationToken = CancellationToken.None;

var decision = await orchestrator.GenerateAndApplyAsync(
    internalModel,
    alias,
    documentContextText,
    cancellationToken);

FieldNode updatedField = internalModel.GetField(alias);

Console.WriteLine($"Decision allowed: {decision.IsAllowed}");
Console.WriteLine($"Deny reason: {decision.DenyReason}");
Console.WriteLine($"Result status: {updatedField.CurrentValue.Status}");
Console.WriteLine($"Value: {updatedField.CurrentValue.Value}");
Console.WriteLine($"Proposals count: {updatedField.ProposalHistory.Count}");
