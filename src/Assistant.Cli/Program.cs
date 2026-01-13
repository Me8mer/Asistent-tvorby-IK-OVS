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

ServiceCollection services = new();

// routing
services.AddSingleton(new RoutingDefaults(
    initialPrefill: new ModelRouting(
        LlmProviderType.OpenAi,
        "gpt-5-nano",
        new LlmGenerationParameters(temperature: 0.0, maxOutputTokens: 256)),
    regenerate: new ModelRouting(
        LlmProviderType.OpenAi,
        "gpt-5-nano",
        new LlmGenerationParameters(temperature: 0.2, maxOutputTokens: 256)),
    improve: new ModelRouting(
        LlmProviderType.OpenAi,
        "gpt-5-nano",
        new LlmGenerationParameters(temperature: 0.2, maxOutputTokens: 256))));

services.AddSingleton<IRoutingPolicy, DefaultRoutingPolicy>();
services.AddSingleton<IPromptBuilder, MinimalPromptBuilder>();
services.AddSingleton<ILlmProvider>(_ =>
    new OpenAiChatLlmProvider("gpt-5-nano"));
services.AddSingleton<IProposalParser>(_ =>
    new StrictJsonProposalParser());
services.AddSingleton<IProposalGenerator, DefaultProposalGenerator>();

services.AddSingleton<InitiationOrchestrator>();

ServiceProvider provider = services.BuildServiceProvider();

// run ONE experiment
await PrefaceBasicInfoSectionExperiment.RunAsync(provider);
