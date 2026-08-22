# Operations & dogfood hardening plan v1

Validates **Ashlar on Ashlar**: dogfood self-improvement blocks, optional mesh chaos/deep lab, and the operator demo script — after [Ship readiness v1](ShipReadiness-v1.md).

**Automation:** `make ops-gate-full`

## Prerequisites

```bash
make ship-gate-full
```

## Tiers

| Tier | Focus | Command |
|------|--------|---------|
| A | Dogfood blocks 1–6 | `make ops-gate-tier-a` |
| B | Dogfood blocks 7–9 + IPC mesh | `make ops-gate-tier-b` |
| C | Closed-loop self-improvement | `make ops-gate-tier-c` |
| D | Mesh deep E2E or chaos-lite | `make ops-gate-tier-d` |
| E | Oh-shit demo (quick) | `make ops-gate-tier-e` |

## Flags

| Variable | Effect |
|----------|--------|
| `OPS_GATE_SKIP_PRIOR=1` | Skip `make ship-gate-full` on full run |
| `OPS_GATE_SKIP_TIER_D=1` | Skip mesh/chaos tier |
| `OPS_GATE_MESH_DEEP=1` | Tier D: `mesh-lab-e2e-deep` one-shot |
| `OPS_GATE_CHAOS_LITE=1` | Tier D: network-negative on `.env.mesh-lab` |
| `OPS_GATE_RUN_PHASE_F=1` | Tier C: also run `dogfood-phasef` |

## Related

- [Kernel chaos drill v1](KernelChaosDrill-v1.md)
- [Release candidate checklist v1](../ReleaseCandidateChecklist-v1.md)
- `make dogfood-all`, `scripts/oh-shit-demo.sh`
