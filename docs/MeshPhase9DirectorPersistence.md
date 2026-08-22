# Phase 9 — Director persistence (LiteDB)

Phase 9 adds optional **durable** fleet and mesh task storage for the HTTP director so a process restart does not wipe placement state.

**Depends on:** Phase 1 control plane (`IFleetNodeRegistry`, `IMeshTaskRegistry`).

## Configuration

| Key | Default | Description |
|-----|---------|-------------|
| `Ashlar:Mesh:Persistence:Provider` | `InMemory` | `InMemory` or `LiteDb` |
| `Ashlar:Mesh:Persistence:DatabasePath` | `mesh-director.db` | LiteDB file path or `Filename=…` connection string |

Environment (compose / host):

- `Ashlar__Mesh__Persistence__Provider=LiteDb`
- `Ashlar__Mesh__Persistence__DatabasePath=/data/mesh-director.db`

The virtual lab enables LiteDB on **peer-a** with a Docker volume (`mesh_lab_peer_a_data`).

## Behavior

- **Fleet nodes** and **mesh tasks** (including idempotency keys, leases, admission flags) are stored in one LiteDB file (collections `mesh_fleet_nodes`, `mesh_tasks`).
- **peer-b** and **worker** remain in-memory by default (not directors in the lab).
- Same semantics as in-memory registries; only durability differs.

## Lab verification

[`scripts/mesh-lab-verify-persistence.sh`](../scripts/mesh-lab-verify-persistence.sh) registers a peer and task, **restarts peer-a**, and asserts both are still present. Invoked from [`mesh-lab-verify.sh`](../scripts/mesh-lab-verify.sh) unless `MESH_LAB_SKIP_PERSISTENCE_VERIFY=1`.

## Limitations

- **Single-node file** — not HA replication; director failover needs external DB or shared volume strategy.
- **No cross-director sync** — two hubs = two databases (split-brain at the organizational level).
- **LiteDB per process** — scale-out directors require a different store.

## Revision history

| Date | Change |
|------|--------|
| 2026-05-19 | LiteDB fleet + task registries; mesh lab restart verify. |
