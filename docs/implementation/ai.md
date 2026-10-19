# AI Subsystem (`Assistant.AI`)

This document describes the architecture, prompt engineering strategy, schema validation, and model routing of the `Assistant.AI` library.

---

## 1. Architectural Role

`Assistant.AI` is the generative foundation of the Assistant. It is responsible for transforming a field generation request—complete with field metadata, fill instructions, and retrieved context—into a concrete, strongly typed `Proposal` for the internal document model (`Assistant.Core`).

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                           Assistant.Pipeline                                    │
│                    (ContextAwareInitiationOrchestrator)                         │
└────────────────────────────────────────┬────────────────────────────────────────┘
                                         │ GenerationRequest
                                         ▼
┌─────────────────────────────────────────────────────────────────────────────────┐
│                               Assistant.AI                                      │
│                                                                                 │
│   ┌─────────────────────┐   ┌───────────────────────┐   ┌───────────────────┐   │
│   │   IRoutingPolicy    │──▶│    IPromptBuilder     │──▶│   ILlmProvider    │   │
│   │ (Model / Temp / Tk) │   │ (Schema / Context Tr) │   │ (OpenAI SDK/Resp) │   │
│   └─────────────────────┘   └───────────────────────┘   └─────────┬─────────┘   │
│                                                                   │             │
│                                                                   ▼ Raw String  │
│                                                         ┌───────────────────┐   │
│                                                         │  IProposalParser  │   │
│                                                         │ (Strict JSON V1)  │   │
│                                                         └─────────┬─────────┘   │
└───────────────────────────────────────────────────────────────────┼─────────────┘
                                                                    │ Proposal
                                                                    ▼
┌─────────────────────────────────────────────────────────────────────────────────┐
│                               Assistant.Core                                    │
│                 (FieldNode: Value, Confidence, Provenance)                      │
└─────────────────────────────────────────────────────────────────────────────────┘
```

---

## 2. Proposal Generation Pipeline

The entire generation cycle is coordinated by `DefaultProposalGenerator` implementing `IProposalGenerator`:

```csharp
public async Task<Proposal> GenerateAsync(GenerationRequest request, CancellationToken cancellationToken)
{
    ModelRouting routing = routingPolicy.ResolveRouting(request);
    LlmPrompt prompt = promptBuilder.BuildPrompt(request);
    LlmRawResponse response = await llmProvider.GenerateAsync(routing, prompt, cancellationToken);
    return proposalParser.ParseProposal(request, routing, response);
}
```

### Step 1: Model Routing (`IRoutingPolicy`)
The routing policy determines which model, provider, temperature, and token limit to use based on the `GenerationMode`:
- **`InitialPrefill`**: Deterministic initial filling (e.g. `gpt-5-nano` or `gpt-5`, temperature `0.0`, max tokens `256`).
- **`Regenerate`**: User-triggered regeneration (temperature `0.2`).
- **`Improve`**: Refinement or self-correction passes (temperature `0.2`).

`DefaultRoutingPolicy` supports per-alias overrides (`perAliasOverrides`), allowing specific complex fields to be routed to larger reasoning models if needed.

### Step 2: Prompt Construction (`IPromptBuilder`)
`MinimalPromptBuilder` prepares a two-part prompt (`LlmPrompt`):
1. **System Prompt**:
   - Instructs the model that it is filling exactly one field in a structured document.
   - Embeds `Schemas/proposal.v1.schema.json`.
   - Prohibits conversational fluff, markdown fences, and extra properties.
2. **User Prompt**:
   - Injects the field's alias, display name, expected data type hint, and fill mode.
   - Injects field-specific domain instructions (if defined in the template YAML).
   - Injects retrieved context text (`DocumentContextText`). Context length is capped by `ContextRequirement.MaxCharacters` (default 3,000 characters) and cleanly truncated with `[Context truncated]`.
   - Attaches `ContextReferences` for end-to-end provenance propagation.

### Step 3: LLM Invocation (`ILlmProvider`)
- **`OpenAiChatLlmProvider`**:
  - **Structured Outputs Mode (Default)**: Uses the official OpenAI .NET client (`ChatClient`). Employs `ChatResponseFormat.CreateJsonSchemaFormat` with `jsonSchemaIsStrict: true` so the OpenAI backend natively guarantees output conforming to `proposal.v1.schema.json`.
  - **Web Search Mode (`ASSISTANT_CONTEXT_MODE="openai-web"`)**: Bypasses local context retrieval and directly queries the OpenAI Responses API (`/v1/responses`) with the `web_search` tool enabled.
- **`FakeLlmProvider`**:
  - Offline mock provider returning predefined JSON responses (`FAKE_VALUE`, confidence `0.5`). Used for unit tests, pipeline smoke tests, and offline runs.

### Step 4: Strict Parsing and Validation (`IProposalParser`)
`StrictJsonProposalParser` processes the raw response string:
1. **JSON Extraction**: Slices between the first `{` and the last `}` to tolerate minor surrounding whitespace.
2. **Deserialization**: Parses into an internal DTO `ProposalJsonV1`.
3. **Field Validation**: Verifies that `value` and `confidence` are present.
4. **Confidence Normalization**: Clamps confidence to the range $[0.0, 1.0]$. Invalid numbers (`NaN`, `Infinity`, negative values) are normalized to `0.0`.
5. **Provenance Assembly**: Records the provider and model in `Proposal.Source` (e.g. `OpenAi:gpt-5-nano (req:resp-123)`), preserves `ContextReferences`, and creates an immutable `Proposal` with status `FieldStatus.AiProposal`.

---

## 3. Output Schema Specification (`proposal.v1.schema.json`)

The model output is governed by JSON Schema Draft 2020-12:

```json
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "$id": "assistant.ai/proposal.v1.schema.json",
  "title": "Assistant AI Proposal Output v1",
  "type": "object",
  "additionalProperties": false,
  "required": ["value", "confidence", "explanation"],
  "properties": {
    "value": {
      "type": "string",
      "description": "Proposed field value. May be empty string if unknown."
    },
    "confidence": {
      "type": "number",
      "minimum": 0.0,
      "maximum": 1.0,
      "description": "Confidence score from 0.0 to 1.0."
    },
    "explanation": {
      "type": "string",
      "description": "Optional short justification of how the value was derived."
    }
  }
}
```

---

## 4. Alignment with Thesis & Analytical Requirements

- **`REQ_02` (AI Fill from Public Sources)**:
  Every proposal generated includes a confidence score, rationale (`Explanation`), and full provenance (`ContextReferences`), ensuring proposals never silently overwrite confirmed user values.
- **Method A (Field-Level Prefill)**:
  `MinimalPromptBuilder` implements Method A as defined in `docs/EvaluationMethods.md`: isolated, single-field generation with exact constraint and context injection.
- **Evaluation Metrics**:
  The presence of numeric confidence and structured output allows empirical benchmarking of fill rate, accuracy, and hallucination reduction across different office datasets.
