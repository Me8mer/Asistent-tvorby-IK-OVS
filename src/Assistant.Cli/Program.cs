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
using Assistant.Dependencies.Context;
using Assistant.Dependencies.Context.Web;
using Assistant.Dependencies.Context.Web.Crawling;
using Assistant.Dependencies.Context.Web.Crawling.Apify;
using Assistant.Dependencies.Context.Web.Processing;
using Assistant.Dependencies.Context.Web.Storage;
using Assistant.Dependencies.Context.Web.Retrieval;
using Assistant.Core.Model;
using Assistant.Pipeline.Initiation;
using Microsoft.Extensions.DependencyInjection;
using Assistant.Parser.OpenXml;


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

// --- Web Context Dependencies ---

services.AddHttpClient();

// Apify options 
services.AddSingleton(new ApifyCrawlerOptions
{
    ApiToken = Environment.GetEnvironmentVariable("APIFY_TOKEN") ?? "",
    ActorId = "apify~website-content-crawler",
    MaxCrawlDepth = 2,
    MaxCrawlPages = 50,
    EnableJavaScriptRendering = false
});

services.AddTransient<IManagedCrawler, ApifyManagedCrawler>();

services.AddSingleton<OfficeCacheStore>(_ =>
{
    string cacheRoot = Path.Combine(
        Environment.CurrentDirectory,
        ".office-cache");

    return new OfficeCacheStore(cacheRoot);
});

services.AddSingleton<WebCorpusBuilder>();
services.AddSingleton<WebChunkCorpusBuilder>();
services.AddSingleton<OfficeWebContextProvider>();
services.AddSingleton<ITemplateSdtParser, OpenXmlTemplateSdtParser>();

services.AddSingleton(new WebSectionPackRetriever.Options
{
    MaximumChunks = 15,
    MaximumTotalCharacters = 5000,
    MaximumChunksPerUrl = 10,
    MinimumTokenLength = 3
});

services.AddSingleton<WebSectionPackRetriever>();

services.AddSingleton<CompositeContextProvider>();
services.AddSingleton<ContextAwareInitiationOrchestrator>();


services.AddSingleton<IRoutingPolicy, DefaultRoutingPolicy>();
services.AddSingleton<IPromptBuilder, MinimalPromptBuilder>();
services.AddSingleton<ILlmProvider>(_ =>
    new OpenAiChatLlmProvider("gpt-5-nano"));
services.AddSingleton<IProposalParser>(_ =>
    new StrictJsonProposalParser());
services.AddSingleton<IProposalGenerator, DefaultProposalGenerator>();

services.AddSingleton<CompositeContextProvider>();
services.AddSingleton<ContextAwareInitiationOrchestrator>();

ServiceProvider provider = services.BuildServiceProvider();

// run ONE experiment
await PrefaceBasicInfoSectionExperiment.RunAsync(provider);
