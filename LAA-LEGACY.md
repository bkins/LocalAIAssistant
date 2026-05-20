# LAA — Legacy / Retired Code Registry

This file tracks folders and modules that have been retired from active use in LocalAIAssistant. Retired code is preserved for reference but is no longer the source of truth — logic lives in CP API.

**Policy:** Do not delete files from retired folders. Mark them here and add a `_RETIRED.md` inside the folder.

---

## Retired Folders

| Folder | Retired | Migrated To | PRs |
|--------|---------|-------------|-----|
| `PersonaAndContextEngine/` | 2026-05-19 | `CognitivePlatform.Api.Domains.PersonaEngine` | EPIC-05 PRs #34, #35, Phase C |

---

## Details

### `PersonaAndContextEngine/`

- **Retired:** 2026-05-19
- **Reason:** EPIC-05 migrated intent analysis and persona routing to CP API. LAA now calls `POST /api/persona-engine/resolve` and the `ConversationOrchestrator` pre-pass handles persona application per-turn automatically.
- **See:** `PersonaAndContextEngine/_RETIRED.md` for full details.
