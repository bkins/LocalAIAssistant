# RETIRED — PersonaAndContextEngine

**Status:** Retired as of EPIC-05 Phase C (2026-05-19)
**Replaced by:** CP API — `CognitivePlatform.Api.Domains.PersonaEngine`

## What this folder contained

A local implementation of intent analysis and persona resolution for the LocalAIAssistant MAUI client:

| File | Purpose |
|------|---------|
| `PersonaAndContextEngine.cs` | Orchestrated intent analysis and persona selection |
| `HybridIntentAnalyzer.cs` | Combined rule-based, keyword, and LLM intent classifiers |
| `KeywordIntentAnalyzer.cs` | Keyword-driven intent classification |
| `LlmIntentAnalyzer.cs` | LLM-backed intent classification |
| `RuleBasedIntentAnalyzer.cs` | Rule-based intent classification |
| `Interfaces/` | `IPersonaAndContextEngine`, `IIntentAnalyzer`, etc. |
| `Models/` | `PersonaContextResult`, `IntentAnalysisResult`, etc. |
| `Enums/` | `Intent` enum values |

## Migration

This logic was ported to CP API in three phases:

- **EPIC-05 Phase A (PR #34):** `IPersonaEngine`, `RuleBasedPersonaEngine`, `Intent` enum, `PersonaContextResult`
- **EPIC-05 Phase B (PR #35):** `HybridPersonaEngine`, `KeywordIntentAnalyzer`, `LlmIntentAnalyzer`, `ConversationOrchestrator` persona pre-pass
- **EPIC-05 Phase C (this PR):** `PersonaEngineActions` — NL actions exposing persona selection (`SetPersona`, `ListPersonas`, `GetActivePersona`) via FastPath and LLM routing

## How LAA uses it now

LAA calls **`POST /api/persona-engine/resolve`** to obtain a `PersonaContextResult`. The `IPersonaEngine` pre-pass inside `ConversationOrchestrator` applies the resolved persona automatically on every turn. LAA does not need to run any local intent analysis.

## Do not delete

Per LAA-LEGACY.md policy, retired code is marked here and preserved for reference. Do not delete files from this folder without updating `LAA-LEGACY.md`.
