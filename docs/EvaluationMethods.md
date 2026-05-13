# AI Prefill Methods

This document defines three LLM based AI prefill methods for the assistant and how they will be evaluated.

---

## Method 1: Field Level Prefill

**What it does**

AI generates a proposal for a single field at a time.  
Input to the LLM includes:

- field alias, name and description  
- type and constraints (text, date, number)  
- selected context from the same section or dependency chain  
- optionally external snippets if available  

The model returns one value for that field. The value is stored as `ai_proposal` in the internal model and never overwrites deterministic or confirmed values.

**How we evaluate**

- Per field accuracy  
  - For each field, compare proposal with ground truth in completed IK OVS documents.  
  - Count correct, partially correct, incorrect, empty.  

- Fill rate  
  - Percentage of AI fillable fields that receive a non empty proposal.  

- Consistency issues  
  - Count obvious contradictions between proposals of related fields in one section.  

---

## Method 2: Section Level Structured Prefill

**What it does**

AI generates proposals for multiple related fields in one section in a single call.  
Input to the LLM includes:

- section description and purpose  
- list of empty fields in that section with aliases, names, descriptions and types  
- confirmed values from the same section and relevant dependencies  
- optionally external snippets if available  


---

## Method 3: Multi Pass Self Verifying Prefill

**What it does**

AI performs prefill in at least two passes.

- Pass 1  
  - Use Method 1 or Method 2 to generate a draft proposal for one field or a whole section.  
- Pass 2  
  - Call the LLM again with the draft, context and constraints.  
  - Ask it to check correctness, internal consistency and compliance with instructions.  
  - Either confirm the draft, mark problems or produce a revised proposal.  


# Chatbot Prefill Methods

This document defines three LLM-based chatbot prefill methods for interactive filling of `chatbot_fill` fields.  
All methods map responses into field aliases in the internal JSON model as `ai_interactive` proposals.

---

## Method 1: Per-Field Question–Answer Chatbot

**What it does in the assistant**

- For each field marked `chatbot_fill`, the system asks exactly one focused question.  
- The LLM generates the natural wording of the question based on field alias, label, and description.  
- The user answers in free text.  
- The LLM parses the answer into the correct value (text/number/date/category).  
- The value is stored into the field as an `ai_interactive` proposal.

No flow control, no grouping.  
Strict 1 question → 1 answer → 1 field mapping.


---

## Method 2: Section-Level / Freeform Extraction

**What it does in the assistant**

- Instead of asking separate micro-questions, the chatbot asks *one broad question* per section.  
  Examples:  
  - “Describe briefly how this office operates and what its main responsibilities are.”  
  - “Provide all contact information for the main person.”  
- The user writes one or multiple paragraphs.  
- The LLM extracts all values for multiple fields (aliases) from the freeform text.  
- Any fields that cannot be inferred remain empty and fall back to Method 1 or UI editing.

This allows "one answer → many fields".

## Method 3: Flow-Controlled Interview (LLM Decides Question Order / Grouping)

**What it does in the assistant**

- The LLM receives:  
  - the list of `chatbot_fill` fields still empty in the section  
  - their aliases, descriptions, and types  
  - current conversation history  
  - known fields and dependency order  
- The LLM decides dynamically:  
  - which field to ask next  
  - whether to skip fields already implicitly answered  
  - whether to group several fields into a single question  
  - when the section is sufficiently complete  
- The system still maps answers to explicit field aliases, but the LLM controls *the flow*.

This turns the chatbot into a small agent that optimizes questioning.


