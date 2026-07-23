# Phase 5 — Elastic mesh scheduling (MVP)

Phase 5 improves **where** new work lands when multiple workers qualify: **lower reported queue depth first**, plus an **optional background loop** that re-invokes placement for **stale pending** tasks when the fleet changes.

**Depends on:** [MeshPhase1ControlPlane.md](MeshPhase1ControlPlane.md) … [MeshPhase4KnowledgeSync.md](MeshPhase4KnowledgeSync.md).

## Worker queue signal

- **`MeshFleetNodeState.ReportedQueueDepth`** — last value from registration or heartbeat.
- **`POST /api/mesh/fleet/nodes/{peerId}/heartbeat`** — optional JSON body `{ "queueDepth": 3 }` (or omit to keep last value).
- **`POST /api/mesh/fleet/nodes`** — optional **`reportedQueueDepth`** on register/update.

Placement orders eligible nodes by **`ReportedQueueDepth` ascending**, then fresher heartbeat, then `peerId` (stable tie-break).

## Elastic status

- **`GET /api/mesh/elastic/status`** — task counts by status + non-drained workers with **`reportedQueueDepth`** and last heartbeat (operator / autoscaler signal).
- To **create/destroy worker replicas** from that signal, use the swappable [`IWorkloadScaler`](WorkloadScaling.md) (Kubernetes-first). Phase 5 places work; WorkloadScaling changes capacity.

## Background rebalancer

When **`Nexo:Mesh:Elastic:Enabled`** is true, **`MeshPendingTaskRebalancerBackgroundService`** every **`IntervalMinutes`** calls **`TryScheduleAsync`** for each **`Pending`** task whose **`CreatedAtUtc`** is older than **`PendingStaleSeconds`** (re-tries placement when new workers appear or load shifts).

This does **not** migrate **Assigned** or **Running** tasks with **active** leases; see [MeshPhase6LeasesAndCheckpoints.md](MeshPhase6LeasesAndCheckpoints.md) for lease expiry, sweep, and migrate-for-checkpoint.

## Configuration (`Nexo__Mesh__Elastic__*`)

| Key | Default | Description |
|-----|---------|-------------|
| `Enabled` | `false` | Run rebalancer |
| `IntervalMinutes` | `2` | Loop interval |
| `PendingStaleSeconds` | `120` | Minimum age before a pending task is re-scheduled |

See **`docs/Configuration.md`**.

## Revision history

| Date | Change |
|------|--------|
| 2026-04-22 | Initial Phase 5: queue-aware placement, heartbeat body, elastic status, rebalancer. |
