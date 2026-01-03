# Centralize intelligence + memory in a web service.

- Expose APIs for conversation, memory management, personality switching, etc.

- Keep clients lightweight, just handling UI and user interaction.

## This gives you some big benefits:

✅ Consistency: The same memory + personalities follow you across devices.

✅ Extensibility: Add new clients (VS/Rider plugin, mobile, web) without rewriting the AI logic.

✅ Flexibility: You can upgrade the orchestrator (e.g., swap Ollama with another LLM, add embeddings/vector search) without touching clients.

✅ Observability: Central place to expand logging, tracing, and even usage metrics.

## High-level sketch of what the service might look like:

### Core Service Layers

1. API Layer (ASP.NET Core Web API or Minimal APIs)
   - Endpoints like /chat/send, /memory/query, /personality/list
   - Handles authentication (maybe just a token for now)
2. Orchestrator Layer
   - Directs requests to the right personality, model, or memory pipeline
   - Applies system prompts / role instructions
   - Manages context window + truncation strategies
3. Memory Layer
   - Long-term memory store (SQL + maybe embeddings later)
   - User-specific histories
   - Knowledge injection into prompts
4. AI Connector Layer
   - Abstracts out Ollama / local models / future LLM APIs
   - Could use strategy pattern or provider-based setup
5. Logging + Observability
   - Central structured logs
   - Possibly a dashboard to inspect conversations, configs, errors

### Then clients (MAUI app, Rider extension, web chat UI) would just:

- Authenticate with the service
- Call APIs (e.g., send user message, get AI response)
- Display results