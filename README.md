# Assistant Prototype

This repository contains a prototype of the **Assistant system for IK
OVS templates**. The goal of the project is to automate and assist with
filling structured government document templates by combining:

1. A structured internal document model (fields, sections,
    dependencies)
2. AI assisted text generation and suggestions
3. External context retrieval from public office websites

The current prototype focuses mainly on:

- Parsing structured DOCX templates
- Representing templates as an internal model
- Retrieving contextual information from office websites
- Using OpenAI to generate or assist with field content

The project currently runs through a CLI entry point.

------------------------------------------------------------------------

# Requirements

You need the following installed:

1. .NET SDK (recommended .NET 8 or newer)
2. OpenAI API key
3. Apify API key

------------------------------------------------------------------------

# Environment Variables

Before running the project you must export two API keys.

## Linux / macOS

``` bash
export OPENAI_API_KEY="your_openai_api_key"
export APIFY_API_KEY="your_apify_api_key"
```

## Windows PowerShell

``` powershell
$env:OPENAI_API_KEY="your_openai_api_key"
$env:APIFY_API_KEY="your_apify_api_key"
```

Currently the keys are read from environment variables. They are not yet
configured through a configuration file.

------------------------------------------------------------------------

# Running the Project

From the repository root run:

``` bash
dotnet run --project src/Assistant.Cli
```

This will start the CLI prototype.

------------------------------------------------------------------------

# Project Structure

    src/
      Assistant.Cli/           CLI entry point
      Assistant.Core/          Core assistant logic and models
      Assistant.Dependencies/  Context retrieval and processing

Key concepts in the model include:

- **FieldDescriptor**\
    Defines metadata for template fields.

- **FieldNode**\
    Runtime representation of a field instance.

- **SectionDescriptor / SectionAlias**\
    Structural grouping of fields.

- **DependencySnapshot**\
    Represents dependency relationships between fields.

These components allow the assistant to understand template structure
and determine how fields should be filled.

------------------------------------------------------------------------

# Current State

This is an early prototype. Current limitations include:

1. Manual API key setup
2. CLI only interface
3. Limited configuration

Future iterations will include:

- configuration based key loading
- UI integration
- improved template parsing
- richer AI assisted filling strategies
