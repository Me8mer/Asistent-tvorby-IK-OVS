# Domain Kernel Subsystem (`Assistant.Core`)

This document describes the architectural design, aggregate structures, lifecycle state transitions, and deterministic merge rules implemented in `Assistant.Core`.

---

## 1. Architectural Role

`Assistant.Core` is the pure domain heart of the IK OVS Assistant. It is designed with **zero external dependencies** (no NuGet packages, pure .NET 8) so that domain rules remain isolated from external storage formats, OpenXML parsers, LLM providers, and network I/O.

All other libraries in the solution depend directly or indirectly on `Assistant.Core`.

```
┌────────────────────────────────────────────────────────────────────────┐
│                        Consumers of Core                               │
│  Assistant.Parser.OpenXml │ Assistant.Dependencies │ Assistant.AI      │
│  Assistant.Pipeline       │ Assistant.Cli          │ Assistant.Ui      │
└───────────────────────────────────┬────────────────────────────────────┘
                                    │
                                    ▼
┌────────────────────────────────────────────────────────────────────────┐
│                             Assistant.Core                             │
│                                                                        │
│  ┌───────────────────────┐  ┌─────────────────┐  ┌──────────────────┐  │
│  │   Aliases & Enums     │  │ Fields & State  │  │ InternalModel    │  │
│  │ (FieldAlias, Status)  │  │ (FieldNode, ...)│  │ & Section Graph  │  │
│  └───────────────────────┘  └─────────────────┘  └──────────────────┘  │
│  ┌───────────────────────┐  ┌─────────────────┐  ┌──────────────────┐  │
│  │ AI Proposals & Applier│  │   MergeRules    │  │  Dependencies    │  │
│  │ (Proposal, Applier)   │  │ (DecideApply)   │  │ (Snapshot/Track) │  │
│  └───────────────────────┘  └─────────────────┘  └──────────────────┘  │
└────────────────────────────────────────────────────────────────────────┘
```

---

## 2. Key Domain Concepts

### 2.1 Strongly Typed Aliases (`FieldAlias`, `SectionAlias`)
To prevent stringly-typed bugs and enforce consistency with `docs/requirements/template_aliases.md`, identifiers are encapsulated as readonly record structs:
- **Prefix Requirement**: Must start with `IKOVS_`.
- **Character Set**: Uppercase ASCII letters (`A-Z`), numbers (`0-9`), underscores (`_`), and hyphens (`-`).
- **Optional Role Suffix**: May contain at most one `:role` specifier (e.g., `IKOVS_PREFACE_BASIC-INFO:primary`).

### 2.2 Field Architecture: Separation of Concerns
Rather than a monolithic field class, `Assistant.Core` decomposes field concerns into dedicated layers:

```
┌──────────────────────────────────────────────────────────────────┐
│                           FieldNode                              │
│ ┌───────────────────────────┐      ┌───────────────────────────┐ │
│ │       FieldMetadata       │      │        FieldState         │ │
│ │ ┌───────────────────────┐ │      │ ┌───────────────────────┐ │ │
│ │ │    FieldDescriptor    │ │      │ │      FieldValue       │ │ │
│ │ │ (YAML: FillModes, ...)│ │      │ │ (Current: Status, Val)│ │ │
│ │ └───────────────────────┘ │      │ └───────────────────────┘ │ │
│ │ ┌───────────────────────┐ │      │ ┌───────────────────────┐ │ │
│ │ │     FieldBinding      │ │      │ │    ProposalHistory    │ │ │
│ │ │ (DOCX: SDT ID, Pos)   │ │      │ │ (All past suggestions)│ │ │
│ │ └───────────────────────┘ │      │ └───────────────────────┘ │ │
│ └───────────────────────────┘      └───────────────────────────┘ │
│ ┌───────────────────────────┐                                    │
│ │     DependencyTracker     │ ──▶ ParentSectionAlias             │
│ │   (BlockedBy, Reasons)    │                                    │
│ └───────────────────────────┘                                    │
└──────────────────────────────────────────────────────────────────┘
```

1. **`FieldDescriptor`**: Immutable declarative metadata (fill modes, display name, instructions, context requirements, context source hints).
2. **`FieldBinding`**: Physical document binding (OpenXML SDT ID, occurrence index, location hint, content kind).
3. **`FieldValue`**: Immutable value snapshot capturing the current `Value`, `FieldStatus`, `Source`, `Confidence`, and `Timestamp`.
4. **`FieldState`**: Mutable lifecycle manager holding the active `FieldValue` and the complete audit trail of `ProposalHistory`.
5. **`FieldNode`**: The aggregate root linking metadata, state, parent section, and dependency tracking.
6. **`FieldStore`**: Encapsulated collection providing controlled lookup and mutation (`TrySetCurrentValue`, `TryAddProposal`, `TryUpdateDependencySnapshot`).

---

## 3. Internal Model & Section Graph Index

`InternalModel` is the complete, validated structural representation of the document:
- Contains all `FieldNode` instances indexed by `FieldAlias`.
- Contains all `SectionDescriptor` instances indexed by `SectionAlias`.
- Enforces strict single-parent ownership: every field must belong to exactly one section.

### `SectionGraphIndex` (Hierarchy and Cycle Detection)
Sections form a directed acyclic tree (or forest). `SectionGraphIndex` manages:
- Root section discovery (`RootSectionAliases`).
- Fast child section lookup (`GetChildSections`).
- Cycle detection: Validates that no section directly or indirectly references itself as a parent (`ValidateNoParentCycles`).

### `InternalModelRuntime`
Combines the static model (`InternalModel`), controlled field store (`FieldStore`), and proposal applier (`ProposalApplier`) into an active runtime object passed through the generation and merge pipelines.

---

## 4. Proposal Management & Non-Destructive Merge Rules

The assistant strictly follows the principle that **AI proposals must never silently overwrite human or verified data**.

### 4.1 Lifecycle States (`FieldStatus`)

| Status | Meaning | Automatic Overwrite Allowed? |
|--------|---------|------------------------------|
| `Empty` | Field has no value yet. | **Yes** (by proposal) |
| `NeedsValidation` | Candidate data found but requires user review. | **Yes** (by proposal) |
| `AiProposal` | Proposal generated by AI, awaiting review. | **No** (only interactive chat or manual input) |
| `Deterministic` | Verified factual value loaded from public registry. | **No** |
| `AiInteractive` | Derived from interactive user chat. | **No** |
| `UserInput` | Manually edited or imported via section merge. | **No** |
| `Confirmed` | Explicitly approved by user. | **No** |
| `Rejected` | Explicitly rejected by user. | **No** |

### 4.2 Merge Decision Logic (`MergeRules.DecideApply`)
When `ProposalApplier.ApplyProposal` is invoked:
1. The proposal is appended to `FieldNode.ProposalHistory` for auditing.
2. `MergeRules.DecideApply(currentValue, proposal)` evaluates the state transition.
3. If allowed, `FieldValue` is updated with the new status, value, confidence, and source.
4. If denied, a `MergeDecision.Deny` is returned with a descriptive reason, preserving the current value untouched.

---

## 5. Dependency Tracking (`Model/Dependencies/`)

Fields can express preconditions and blocking constraints (`REQ_MODEL_DEPENDENCIES`):
- `DependencySnapshot`: Immutable snapshot recording `IsBlocked`, list of `BlockedBy` aliases, and human-readable `Reasons`.
- `DependencyTracker`: Mutable container on `FieldNode` allowing the pipeline to re-evaluate and update readiness flags as fields are filled.
