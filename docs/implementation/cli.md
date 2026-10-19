# CLI Application & Experiment Runner (`Assistant.Cli`)

This document describes the entry point, dependency injection composition, configuration management, and experiment execution framework implemented in `Assistant.Cli`.

---

## 1. Architectural Role

`Assistant.Cli` is the executable console project (`OutputType=Exe`) for the prototype. It serves as the **Composition Root** of the entire application, registering all dependencies into the Microsoft Dependency Injection container, validating API credentials, and running end-to-end integration experiments.

```
┌────────────────────────────────────────────────────────┐
│                      Assistant.Cli                     │
│                                                        │
│  ┌─────────────────────────┐  ┌──────────────────────┐ │
│  │   ConfigurationLoader   │  │ Program.cs           │ │
│  │   (Keys, Env, INI file) │  │ (DI Composition Root)│ │
│  └─────────────────────────┘  └──────────┬───────────┘ │
│                                          │             │
│                                          ▼             │
│                      ┌───────────────────────┐         │
│                      │ PrefaceBasicInfo-     │         │
│                      │ SectionExperiment     │         │
│                      └───────────┬───────────┘         │
└──────────────────────────────────┼─────────────────────┘
                                   │
                                   ▼
┌────────────────────────────────────────────────────────┐
│                   Underlying Services                  │
│  Assistant.Pipeline │ Assistant.Core │ Assistant.AI    │
│  Assistant.Parser   │ Assistant.Dependencies           │
└────────────────────────────────────────────────────────┘
```

---

## 2. Configuration & Secrets Management

API credentials and operational modes are managed through `ConfigurationLoader`:

### 2.1 Supported Configuration Sources
1. **Local INI File (`config.local.ini`)**: Optional file in the working directory (excluded from Git).
   ```ini
   [OpenAI]
   ApiKey=sk-...

   [Apify]
   ApiToken=apify_api_...
   ```
2. **Environment Variables**:
   - `OPENAI_API_KEY` (or `OpenAI__ApiKey`)
   - `APIFY_API_KEY` (or `Apify__ApiToken`)

`ConfigurationLoader.Validate` ensures both credentials are non-empty before any service execution begins.

### 2.2 Operational Context Modes (`ASSISTANT_CONTEXT_MODE`)
- **`local` (Default)**:
  Uses the local Web RAG engine. Crawls office websites via Apify, indexes text with Lucene Czech NLP, and performs BM25 ranking. Uses lightweight models (e.g. `gpt-5-nano`) with low latency and deterministic reproducibility.
- **`openai-web`**:
  Bypasses local crawling and retrieval. Routes generation to models supporting OpenAI's Responses API with native web search enabled (`gpt-5`), instructing the model to discover missing organizational facts online.

---

## 3. Composition Root (`Program.cs`)

`Program.cs` registers all subsystem services in a unified `ServiceCollection`:

| Service / Interface | Lifetime | Registered Implementation |
|----------------------|----------|---------------------------|
| `ApifyCrawlerOptions` | Singleton | Ingests Apify token and scraping rules (depth=2, max=50 pages). |
| `IManagedCrawler` | Transient | `ApifyManagedCrawler` |
| `OfficeCacheStore` | Singleton | Local disk store configured to `.office-cache/`. |
| `WebCorpusBuilder`, `WebChunkCorpusBuilder` | Singleton | Processing and chunking engines. |
| `WebSectionPackRetriever` | Singleton | BM25 retrieval engine with debug logging support. |
| `ITemplateSdtParser` | Singleton | `OpenXmlTemplateSdtParser` |
| `ITemplateDefinitionProvider` | Singleton | `TemplateDefinitionProvider` (auto-resolves `definitionsTemplate.yaml`). |
| `CompositeContextProvider` | Singleton | Aggregates section-level web context packs. |
| `IRoutingPolicy` | Singleton | `DefaultRoutingPolicy` with `RoutingDefaults`. |
| `IPromptBuilder` | Singleton | `MinimalPromptBuilder` |
| `ILlmProvider` | Singleton | `OpenAiChatLlmProvider` |
| `IProposalParser` | Singleton | `StrictJsonProposalParser` |
| `IProposalGenerator` | Singleton | `DefaultProposalGenerator` |
| `ContextAwareInitiationOrchestrator`| Singleton | Orchestrates context retrieval and proposal generation. |

---

## 4. Experiment Runner (`PrefaceBasicInfoSectionExperiment`)

The experiment provides a complete, verifiable scenario demonstrating document generation for the `IKOVS_PREFACE_BASIC-INFO` section:

### Execution Steps:
1. **Document Loading**: Resolves `working_template.docx` and opens a read-only stream.
2. **SDT Extraction**: `OpenXmlTemplateSdtParser` extracts all content control bindings and records diagnostics.
3. **Definition Matching**: Resolves `TemplateDefinition` by finding the definition whose declared field aliases have the highest overlap with the parsed SDTs.
4. **Runtime Hydration**: `TemplateRuntimeFactory` verifies that no unknown or duplicate aliases exist, instantiating `InternalModelRuntime`.
5. **Context & Proposal Generation**: For each field in `IKOVS_PREFACE_BASIC-INFO`:
   - Retrieves relevant snippets from the target public office website (e.g., `https://www.mmr.gov.cz/`).
   - Slices and injects context into the model prompt.
   - Generates proposals and applies them through `MergeRules`.
6. **Console Output**: Renders a formatted audit table containing the resulting status, proposed value, confidence score, explanation, and provenance source for every field.

---

## 5. Running the CLI

From the root of the repository, execute:

```powershell
dotnet run --project src/Assistant.Cli
```
