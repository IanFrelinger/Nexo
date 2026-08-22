# Phase 4 — Domain knowledge sync (adaptation + patterns)

Phase 4 adds a **first-class JSON path** to move **adaptation log** and **observed pattern** records between Ashlar.API hosts, plus an **optional background pull** from configured peers.

**Depends on:** [MeshPhase0NorthStar.md](MeshPhase0NorthStar.md) … [MeshPhase3DistributedExecution.md](MeshPhase3DistributedExecution.md).

## HTTP API (on each node with adaptation enabled)

| Method | Path | Purpose |
|--------|------|---------|
| `GET` | `/api/mesh/knowledge/export` | Query params: `since` (optional ISO), `maxAdaptations`, `maxPatterns` — returns **`MeshKnowledgeExportPayload`** JSON |
| `POST` | `/api/mesh/knowledge/import` | Body: same JSON shape — inserts into local **LiteDB** stores; returns applied/skipped counts |

**Merge rule:** duplicate **adaptation ids** and **pattern ids** are skipped (LiteDB unique `_id` / `PatternId`). Conflicting **frequency** or **timestamps** for the same pattern id are **not** merged (LWW is a future enhancement).

## Optional hub pull (`Ashlar:Mesh:KnowledgeSync`)

When **`Ashlar__Mesh__KnowledgeSync__Enabled=true`** and **`Ashlar__Mesh__KnowledgeSync__PeerBaseUrls__0`** (etc.) list peer API roots, **`MeshPeerKnowledgePullBackgroundService`** periodically:

1. `GET {peer}/api/mesh/knowledge/export?since=...` (lookback = interval × `SinceLookbackMultiplier`)
2. `POST /api/mesh/knowledge/import` locally with the payload

Configure **`Ashlar:Security`** mesh tokens / API auth so the HTTP client can reach peers in production.

## User knowledge log

**Not** included in this phase (different store shape); use existing trust / knowledge APIs or extend in a follow-up.

## Revision history

| Date | Change |
|------|--------|
| 2026-04-22 | Initial Phase 4 export/import + optional peer pull. |
