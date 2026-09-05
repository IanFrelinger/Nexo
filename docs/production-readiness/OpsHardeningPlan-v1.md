# Operations & dogfood hardening plan v1

Validates **Ashlar on Ashlar**: dogfood self-improvement blocks, optional mesh chaos/deep lab, and the operator demo script — after [Ship readiness v1](ShipReadiness-v1.md).

**Automation:** `make ops-gate-full`. Pull requests that touch dogfood tests, the A/B/C/E scripts, or `scripts/oh-shit-demo.sh` run counted Tier A (Blocks 1–6, unique-identity floor 6), Tier B (Blocks 7–9 + IPC, floor 4), Tier C closed-loop (floor 1), and the quick operator demo. D still requires Docker plus `OPS_GATE_MESH_DEEP=1` or `OPS_GATE_CHAOS_LITE=1`.

## Prerequisites

```bash
make ship-gate-full
```

## Tiers

| Tier | Focus | Command |
|------|--------|---------|
| A | Dogfood blocks 1–6 (counted wrapper, floor 6) | `make ops-gate-tier-a` |
| B | Dogfood blocks 7–9 + IPC mesh (counted wrapper, floor 4) | `make ops-gate-tier-b` |
| C | Closed-loop self-improvement (counted wrapper, floor 1; Phase F opt-in floor 2) | `make ops-gate-tier-c` |
| D | Mesh deep E2E or chaos-lite | `OPS_GATE_MESH_DEEP=1 make ops-gate-tier-d` (no-flag / `make ops-gate-full` default skips D; the script exits 2 instead of PASS) |
| E | Oh-shit demo (quick; bootstrap + chat + orchestrate + dogfood block1) | `make ops-gate-tier-e` |

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
