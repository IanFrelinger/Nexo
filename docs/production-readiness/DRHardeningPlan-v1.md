# Disaster recovery hardening plan v1

Backup → wipe → restore for **pipeline LiteDB**, **user knowledge log**, and optional **mesh director** persistence.

**Automation:** `make dr-gate-full`

## Tiers

| Tier | Focus | Command |
|------|--------|---------|
| A | Pipeline LiteDB backup/restore + resume | `make dr-gate-tier-a` |
| B | User knowledge LiteDB tests | `make dr-gate-tier-b` |
| C | Mesh director restart (`.env.mesh-lab`) or advisory skip | `make dr-gate-tier-c` |

## Flags

| Variable | Effect |
|----------|--------|
| `DR_GATE_SKIP_PRIOR=1` | Skip compat gate (default) |
| `DR_GATE_SKIP_MESH=1` | Skip mesh persistence even if lab is up |

## Related

- [DR readiness v1](DRReadiness-v1.md)
- [Rollback drill v1](RollbackDrill-v1.md)
