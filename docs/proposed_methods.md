# Methods Overview

---

## 1. General Methods

These methods are used by the assistant as part of its default behaviour.  
They are not evaluated as standalone variants.

### 1.1 External Context Injection (AI Prefill)

When available, the assistant adds external information (website text, registries, cached values) into LLM prompts.  
This helps the model base its proposals on real content instead of assumptions.  
External context can be combined with any AI Prefill evaluation method.

### 1.2 Dynamic Question Wording (Chatbot)

For chatbot interactions, the LLM generates natural phrasing of questions using field metadata.  
Only the wording changes; the intent and the mapping to the field remain fixed.  
This is a general conversational improvement shared across all chatbot methods.

### 1.3 Flow Controlled Interview (Chatbot)

The chatbot uses the LLM to decide the order and grouping of questions in a section.  
It can skip fields already answered implicitly, group related fields, or finish early when a section is complete.  
This adaptive flow is part of the final assistant design and not a separate evaluation method.

---

## 2. AI Prefill – Evaluation Methods

These methods define how the LLM generates content for fields in the internal model.  
They will be compared in the thesis.

### 2.1 Method A: Field Level Prefill

**What it does**

The model fills fields one at a time.  
Each field is generated independently in a separate model call.  
The method focuses on simple, isolated generation without section-wide reasoning.

**How it is evaluated**

Compared directly with Section Level Prefill.  
Also used as the single pass baseline for Multi Pass Prefill.

---

### 2.2 Method B: Section Level Prefill (Structured Multi Field)

**What it does**

The model fills all missing fields in one section in a single model call.  
The output is a structured multi field proposal where the model reasons about the whole section at once.  
This method emphasizes internal coherence between related fields.

**How it is evaluated**

Compared directly with Field Level Prefill.  
Also used as the single pass baseline for Multi Pass Prefill.

---

### 2.3 Method C: Multi Pass Prefill

**What it does**

The model generates content in two steps.  
First, an initial draft is produced using the normal single pass approach.  
Then a second model pass reviews the draft, checks for problems and inconsistencies, and produces a refined version.  
The method aims to increase correctness by separating creation from verification.

**How it is evaluated**

Compared by running both single pass and multi pass variants for Field Level and Section Level Prefill.

---

## 3. Chatbot Prefill – Evaluation Methods

These methods define how the chatbot collects values for chatbot_fill fields using the LLM.

### 3.1 Method D: Per Field Question Answer

**What it does**

The chatbot fills fields one at a time.  
For each field, the LLM formulates a focused question and then interprets the user’s answer into the correct field value.  
This is the baseline turn by turn behaviour.

**How it is evaluated**

Compared directly with Section Level Freeform Extraction.

---

### 3.2 Method E: Section Level Freeform Extraction

**What it does**

Instead of many narrow questions, the chatbot asks one broad question for a section or a field group.  
The user answers in freeform text, and the LLM extracts the relevant values for all fields that can be inferred.  
Remaining fields stay empty and can be filled later.

**How it is evaluated**

Compared directly with Per Field Question Answer.

---

## 3. Chatbot Prefill – Evaluation Methods

These methods define how the chatbot collects values for `chatbot_fill` fields using the LLM.

---

### 3.1 Method D: Per Field Question Answer

#### What it does

The chatbot fills fields one at a time.
At any moment there is a single active field. For that field, the LLM formulates a focused question based on field metadata and then interprets the user’s answer into the correct field value.

If the interpreted value fails validation or is ambiguous, the chatbot asks a short follow up question for the same field until the value is valid or the field is explicitly left unresolved.

This represents a strictly field driven interview flow and serves as the baseline conversational method. The orchestration remains aligned with field level processing and does not require section level coordination.

#### Evaluation

This method is used as the baseline chatbot strategy.
It is compared directly with Transcript Assisted Per Field Extraction.

Primary metrics:

* total number of model calls required to fill the same set of `chatbot_fill` fields
* completion rate and validation success rate
* number of clarification turns per field
* total user interaction steps

---

### 3.2 Method F: Transcript Assisted Per Field Extraction

#### What it does

This method preserves the same field driven interview flow as Method D, but adds a transcript based pre step before asking a question.

Whenever a new field becomes active, the system first attempts to infer the field value from the conversation transcript collected so far.

For each active field:

1. The LLM receives the field specification and the current transcript.
2. If the value can be inferred with sufficient confidence, the system either fills the field directly or asks for confirmation.
3. If the LLM returns `UNKNOWN` or low confidence, the chatbot proceeds with the normal question answer step as defined in Method D.

The key constraint is that this method does not attempt global multi field extraction.
It only attempts to resolve the currently active field.

This keeps the orchestration simple and limits unintended side effects while still allowing reuse of previously stated information.

#### Evaluation

This method is compared directly with Method D in order to quantify efficiency gains versus additional model cost.

Primary metrics:

* total number of model calls

  * calls used for transcript extraction
  * calls used for asking questions and interpreting answers
* number of user questions avoided due to successful transcript inference
* number of confirmations replacing full questions
* completion rate and validation success rate
* total user interaction steps

The goal is to measure the tradeoff between additional extraction prompts and reduction of interactive questioning, and determine whether transcript assisted inference provides measurable benefit for non generic fields.
