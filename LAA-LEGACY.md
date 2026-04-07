# LAA Legacy Features

_Features that exist in LocalAIAssistant but are parked pending migration to the CP API._

Last updated: 2026-04

---

## Context

`LocalAIAssistant` began as a standalone AI assistant app — before the Cognitive Platform
API existed. It contained its own LLM integration, persona management, and intent
routing logic. As the CP API matured and took on structured domain responsibilities
(tasks, journals, knowledge), much of that logic became redundant or out of place in
the client app.

These features have been deliberately left in LAA rather than deleted. They represent
real, designed behavior that will be migrated to the CP API when the roadmap reaches
the appropriate phase. They are **not dead code** — they are **parked features**.

---

## Parked features

### Persona and context engine
**Location:** `PersonaAndContextEngine/`

**What it does:**
Selects an active "persona" and LLM profile based on the intent of the user's message.
The idea: a technical question routes to a technical persona + model, a creative writing
request routes to a different persona + model. The selection is driven by a hybrid
analyzer that combines rule-based, keyword, and (optionally) LLM-based intent detection.

**Key files:**
- `PersonaAndContextEngine.cs` — top-level coordinator
- `HybridIntentAnalyzer.cs` — combines rule-based + keyword + LLM analysis
- `RuleBasedIntentAnalyzer.cs`, `KeywordIntentAnalyzer.cs`, `LlmIntentAnalyzer.cs`
- `Enums/Intent.cs` — intent taxonomy
- `Models/PersonaContextResult.cs`, `IntentAnalysisResult.cs`

**Why it's parked:**
The CP API currently uses a single active LLM profile (Groq, runtime-swappable via
config). Multi-persona routing requires a richer LLM provider model, a persona catalog,
and per-persona system prompts — work that belongs in the API's interpreter layer, not
in a client app.

**Planned home:** CP API — Phase 5 or Phase 5.x, alongside or after the Insight Engine.
It will be implemented as a `PersonaRouter` upstream of the `LlmInterpreter`, with
personas defined in the action catalog or a dedicated config section.

---

### AI memory services
**Location:** `Services/AiMemory/`

**What it does:**
Stores and retrieves conversation memory across sessions. Supports both SQLite and JSONL
backends. Used to inject relevant past context into LLM prompts.

**Key files:**
- `IAiMemoryStore.cs`, `SqliteAiMemoryStore.cs`, `JsonlAiMemoryStore.cs`
- `ConversationMemory.cs`, `MemoryService.cs`
- `MemoryRetrievalOptions.cs`

**Why it's parked:**
The CP API's `ConversationContextStore` handles in-session context. Cross-session
memory is a Phase 4+ concern that belongs server-side in the API, not per-device in
the client. The LAA implementation was built before the API had any persistence layer.

**Planned home:** CP API — Phase 4/5, as part of the Knowledge/Memory domain.
The `IObjectStore` foundation is already in place.

---

### Offline intent queue
**Location:** `Services/OfflineQueueService.cs`, `Data/Models/OfflineQueueItem.cs`,
`Services/QueueReplayCoordinator.cs`

**What it does:**
Queues user intents locally when the CP API is unreachable, then replays them
sequentially when connectivity is restored.

**Why it's parked:**
The feature requires idempotent server-side replay support (request IDs, expiry windows)
that isn't yet implemented in the CP API. The spec is complete — see Master Plan v2,
Side Quests section.

**Planned home:** Collaborative between LAA (queue storage + replay UI) and CP API
(idempotent command acceptance). Phase 4+.

---

## What to do with this code

- **Do not delete** any of the above.
- **Do not actively develop** these features in LAA.
- When a feature's migration phase arrives, design the API-side implementation first,
  then port the LAA code to call the API endpoint.
- Treat these files as a reference implementation, not production code.

---

## Note on `OrchestratorService` and `LlmService`

`Services/OrchestratorService.cs` and `Services/LlmService.cs` are the original LAA
equivalents of the CP API's `ConversationOrchestrator` and `ILlmClient`. They are
still wired in `MauiProgram.cs` for features that don't yet have CP API equivalents.
As migration progresses, these will be replaced by calls to the CP API clients.
