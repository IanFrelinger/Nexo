# Phase 1 — Mesh control plane (MVP)

Phase 1 adds an **in-process director** on any host that runs **`AddNexo()`**: fleet node registry, mesh task store, and **greedy placement** (no migration, no external etcd).

**Depends on:** [MeshPhase0NorthStar.md](MeshPhase0NorthStar.md).

## What is implemented

| Component | Behavior |
|-----------|----------|
| **`IFleetNodeRegistry`** | In-memory register/update/list/remove workers; heartbeat timestamp; **drained** flag excludes node from new placements. |
| **`IMeshTaskRegistry`** | In-memory tasks with `Pending → Assigned → Running → Succeeded/Failed`; server-generated `taskId`. |
| **`IMeshTaskPlacementService`** | Picks a node with **non-empty `ApiBaseUrl`**, not drained, **affinity** label match, and **all `RequiredBrickIds`** present in `AdvertisedBrickIds` (or label value `brick:{id}`). **Retry** skips the previously assigned peer when another candidate exists. |
| **HTTP API** (`Nexo.API`) | Under **`/api/mesh`** — see table below. |

## HTTP routes

Base path: **`/api/mesh`** (same auth middleware as other `/api/*` routes when enabled).

| Method | Path | Purpose |
|--------|------|---------|
| `GET` | `/fleet/nodes` | List registered workers |
| `POST` | `/fleet/nodes` | Register or update worker (`MeshFleetNodeRequest`) |
| `DELETE` | `/fleet/nodes/{peerId}` | Remove worker |
| `POST` | `/fleet/nodes/{peerId}/heartbeat` | Heartbeat |
| `POST` | `/fleet/nodes/{peerId}/drain` | Body `{ "drained": true/false }` |
| `GET` | `/tasks` | List tasks |
| `POST` | `/tasks` | Create task (`MeshTaskCreateRequest`) |
| `GET` | `/tasks/{taskId}` | Get task |
| `POST` | `/tasks/{taskId}/schedule` | Run placement (optional body: `scheduleIdempotencyKey`, `correlationId`) |
| `POST` | `/tasks/{taskId}/retry` | Re-place on different peer when possible (same optional body) |
| `GET` | `/tasks/{taskId}/result` | Phase 3: stream bytes when `resultHandle` is a file path on director |
| `PATCH` | `/tasks/{taskId}/status` | Worker reports `Running` / `Succeeded` / `Failed` / `Pending` (optional `resultSummary`, `resultHandle`) |
| `GET` | `/knowledge/export` | Phase 4: JSON export of adaptations + patterns |
| `POST` | `/knowledge/import` | Phase 4: import payload into local stores |

## Worker registration example

```json
POST /api/mesh/fleet/nodes
{
  "peerId": "worker-01",
  "apiBaseUrl": "https://worker-01.tailnet:8080/",
  "labels": { "region": "us-west" },
  "advertisedBrickIds": ["generation.capability-routing", "my-custom-brick"],
  "drained": false
}
```

## Task + schedule example

```json
POST /api/mesh/tasks
{
  "name": "batch-step-1",
  "steps": 1,
  "requiredBrickIds": ["my-custom-brick"],
  "affinity": { "region": "us-west" },
  "priority": 0,
  "deadlineUtc": null
}
```

Then `POST /api/mesh/tasks/{taskId}/schedule`. Response includes `assignedPeerId` and `assignedApiBaseUrl` for the caller to dispatch work (e.g. HTTP brick execute on that base URL — Phase 0 north star A).

## Explicit limitations (next phases)

- **Transport/auth** — see [MeshPhase2TransportAndAuth.md](MeshPhase2TransportAndAuth.md) for optional mesh tokens, body caps, and rate limits on `/api/mesh` and brick execute.
- **Correlation / idempotency / results** — see [MeshPhase3DistributedExecution.md](MeshPhase3DistributedExecution.md).
- **In-memory only** — restart loses registry; Phase 4+ may persist to LiteDB/SQL.
- **Placement does not invoke bricks** — it only **chooses** a node; the caller must dispatch.
- **No global fairness queue** — simple greedy ordering by heartbeat recency.
- **Affinity** is exact string match on node labels.

## Tests

- `Nexo.Tests.Infrastructure` → `Tests/Fleet/MeshTaskPlacementServiceTests.cs`
