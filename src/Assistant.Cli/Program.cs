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
using Assistant.Core.Model.Templates;
using Assistant.Dependencies.Context;
using Assistant.Dependencies.Context.Web;
using Assistant.Dependencies.Context.Web.Crawling;
using Assistant.Dependencies.Context.Web.Crawling.Apify;
using Assistant.Dependencies.Context.Web.Processing;
using Assistant.Dependencies.Context.Web.Storage;
using Assistant.Dependencies.Context.Web.Retrieval;
using Assistant.Dependencies.TemplateDefinitions;
using Assistant.Pipeline.Initiation;
using Microsoft.Extensions.DependencyInjection;
using Assistant.Parser.OpenXml;
using Microsoft.Extensions.Configuration;
using Assistant.Cli.Configuration;
using System.IO;


ServiceCollection services = new();

var apiKeys = ConfigurationLoader.Load();
ConfigurationLoader.Validate(apiKeys);

// Determine mode via environment variable. When ASSISTANT_CONTEXT_MODE is set to
// "openai-web" the LLM will call OpenAI's Responses API with web_search
// enabled and the pipeline will skip retrieving external context.
// TODO. Add to config
string contextMode = Environment.GetEnvironmentVariable("ASSISTANT_CONTEXT_MODE") ?? "local";
bool useOpenAiWebSearch = string.Equals(contextMode, "openai-web", StringComparison.OrdinalIgnoreCase);

// routing
services.AddSingleton(new RoutingDefaults(
    initialPrefill: new ModelRouting(
        LlmProviderType.OpenAi,
        useOpenAiWebSearch ? "gpt-5" : "gpt-5-nano",
        new LlmGenerationParameters(temperature: 0.0, maxOutputTokens: 256)),
    regenerate: new ModelRouting(
        LlmProviderType.OpenAi,
        useOpenAiWebSearch ? "gpt-5" : "gpt-5-nano",
        new LlmGenerationParameters(temperature: 0.2, maxOutputTokens: 256)),
    improve: new ModelRouting(
        LlmProviderType.OpenAi,
        useOpenAiWebSearch ? "gpt-5" : "gpt-5-nano",
        new LlmGenerationParameters(temperature: 0.2, maxOutputTokens: 256))));

// --- Web Context Dependencies ---

services.AddHttpClient();

// Apify options
services.AddSingleton(new ApifyCrawlerOptions
{
    ApiToken = apiKeys.ApifyApiToken,
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
    MinimumTokenLength = 3,
    EnableDebugLogging = string.Equals(
        Environment.GetEnvironmentVariable("ASSISTANT_RETRIEVER_DEBUG"),
        "1",
        StringComparison.Ordinal),
    DebugTopCandidatesToPrint = 8
});

services.AddSingleton<WebSectionPackRetriever>();

services.AddSingleton<CompositeContextProvider>();
services.AddSingleton<ContextAwareInitiationOrchestrator>();

string templateDefinitionsPath = ResolveTemplateDefinitionsPath();

services.AddSingleton(_ => new TemplateDefinitionProvider(templateDefinitionsPath));
services.AddSingleton<ITemplateDefinitionProvider>(sp =>
    sp.GetRequiredService<TemplateDefinitionProvider>());


services.AddSingleton<IRoutingPolicy, DefaultRoutingPolicy>();
services.AddSingleton<IPromptBuilder, MinimalPromptBuilder>();
services.AddSingleton<ILlmProvider>(_ =>
    new OpenAiChatLlmProvider(
        modelName: useOpenAiWebSearch ? "gpt-5" : "gpt-5-nano",
        apiKey: apiKeys.OpenAiApiKey,
        useWebSearch: useOpenAiWebSearch));
services.AddSingleton<IProposalParser>(_ =>
    new StrictJsonProposalParser());
services.AddSingleton<IProposalGenerator, DefaultProposalGenerator>();

ServiceProvider provider = services.BuildServiceProvider();

// run ONE experiment
await PrefaceBasicInfoSectionExperiment.RunAsync(provider);

static string ResolveTemplateDefinitionsPath()
{
    string? currentDirectory = AppContext.BaseDirectory;

    while (!string.IsNullOrWhiteSpace(currentDirectory))
    {
        string candidatePath = Path.Combine(
            currentDirectory,
            "src",
            "templates",
            "definitionsTemplate.yaml");

        if (File.Exists(candidatePath))
        {
            return candidatePath;
        }

        DirectoryInfo? parentDirectory = Directory.GetParent(currentDirectory);
        if (parentDirectory is null)
        {
            break;
        }

        currentDirectory = parentDirectory.FullName;
    }

    throw new FileNotFoundException(
        "Could not locate src/templates/definitionsTemplate.yaml starting from the application base directory.");
}