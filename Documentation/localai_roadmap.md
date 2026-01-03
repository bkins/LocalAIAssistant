# Local AI Assistant Roadmap

This document tracks the features, progress, and planned enhancements for the **Local AI Assistant** project.  
The assistant is designed to run locally, integrate with Ollama, and maintain persistent memory while supporting orchestrated AI behaviors.

---

## 🚀 Priorities (Short-Term Focus)
1. **Memory System Fine-Tuning**
    - Improve Short-Term (SQLite) and Long-Term (JSONL) persistence.
    - Add indexing, cleanup strategies, and retrieval optimization.

2. **AI Orchestrator Foundations**
    - Begin designing how multiple "AI personalities" or "agents" will coordinate.
    - Define initial orchestrator responsibilities (e.g., routing queries, merging results).

3. **Improved Logging & Monitoring**
    - Add structured logs with clear markers for failures and checkpoints.
    - Ensure logs capture critical moments (crashes, stalls, agent transitions).

4. **Dynamic Model Switching**
    - Allow changing Ollama models without recompilation (config or hot-reload).

---

## ✅ Current Features
- **Local LLM Integration (Ollama / llama.cpp)**  
  The app communicates with a locally running LLM for AI-powered responses.

- **Persistent Memory**
    - Short-Term Memory stored in SQLite DB.
    - Long-Term Memory stored in JSONL file.

- **Memory Scoring**  
  `ApplyScoring` logic with importance-based ranking and promotion.

- **Multi-Personality Support (Basic)**  
  Users can switch between predefined AI personalities manually.

- **MVVM Architecture**  
  Proper separation of UI, ViewModels, and Services for maintainability.

- **Basic Error Logging**  
  Application logs errors and some events.

- **Demo/Test Harness**  
  Early framework for evaluating memory scoring, promotion, and summary building.

- **Dynamic Model Switching (basic UI + config).**  
  Allow changing Ollama models without recompilation (config or hot-reload).

---

## 🔄 In Progress
### Memory Enhancements
- Refactoring memory scoring and promotion logic into a unit-test-friendly style.
- Roadmap documentation for ongoing feature tracking.

### Logging Improvements
- Add **strategic logging** around critical flows (e.g., conversation handling, orchestrator decisions, model responses).
- Ensure logs persist outside console/debugger (rotation, file storage).
- Future: structured logs (JSON) for easier parsing/analysis.

### AI Orchestrator (Foundations)
- Formalize an **orchestrator service** that:
    - Routes queries to the right personality/model.
    - Manages memory retrieval/updates.
    - Applies business logic (e.g., context switching, safety checks).

---

## 🛠️ Planned Enhancements
### Memory
- Optimize memory queries with indexes in SQLite.
- Explore embedding-based retrieval for long-term memory.
- Add memory pruning/garbage collection strategies.

### Model Flexibility
- Config-driven Ollama model selection.
- Hot-reload model switching.
- Optionally support multiple concurrent models.
- Query available ollama model list via API to pull dynamically.

### Personality System
- Expand support for **dynamic personalities** (AI can suggest or generate new ones).
- Context-based automatic switching between personalities.

### Developer Experience
- Add **diagnostic dashboard** for viewing logs, memory state, orchestrator activity.
- Configurable settings page for model parameters (temperature, max tokens, etc.).

### User Experience
- CLI options for controlling AI mode and model.
- Configuration file for persistence paths, orchestrator settings, and logging.
- Future mobile/desktop UI integration.

### Sync & Portability
- Consider syncing memories across devices (stretch goal).
- Explore export/import of conversations and memories.

---

## 📅 Long-Term Vision
- A robust **AI companion framework** that:
    - Runs locally with user-controlled data.
    - Supports flexible orchestration across multiple models/personalities.
    - Provides transparency via logs and dashboards.
    - Can evolve into a platform for experimenting with local LLM orchestration.

- Rich orchestrator that dynamically assigns tasks to different agents.
- Multi-modal support (voice synthesis, maybe images).
- Self-updating memory strategies (auto-tuning retrieval methods).
- Expand beyond Ollama: support llama.cpp, LM Studio, and remote APIs.

---

## 📝 Notes
- Logging strategy and orchestration are **critical enablers** for future growth.
- Memory persistence is functional but will need **continuous refinement** as usage grows.
- Documentation should be kept up-to-date with each major feature milestone.  

---

## Actively Working On:
-Update LlmService
  - Inject IOptionsMonitor<OllamaConfig> so it always uses the latest config values when making calls.
  - Add a change callback so that when the user switches models from the UI, you can log it (and potentially reset connections if needed).