# Ship readiness v1

**Status: SHIP GATE GREEN** (Tiers A–D default profile, 2026-05-19)

Track after [Composition & mesh readiness v1](CompositionMeshReadiness-v1.md). **Plan:** [Ship hardening plan v1](ShipHardeningPlan-v1.md)

## Command

```bash
make ship-gate-full
```

## Record

| Date | Gate | Result | Notes |
|------|------|--------|-------|
| 2026-05-19 | A–D local | **PASS** | Runtime bundle opt-in (`SHIP_GATE_RUN_RUNTIME_GATE=1`) |

## Tier summary

| Tier | Focus | Status |
|------|--------|--------|
| A | Production Readiness Gate v1 CLI + LiteDB resume | **PASS** |
| B | ProdStyle + smoke + doctor | **PASS** |
| C | Release preflight (`0.0.0-ship-gate-local`) | **PASS** |
| D | Doctor sign-off | **PASS** |

## Next: operations & dogfood

```bash
make ops-gate-full
```

See [Ops readiness v1](OpsReadiness-v1.md). Full stack: `make nexo-ready-gate`.

## Sign-off

- [x] `ship-gate-full` green (2026-05-19)
- [x] Composition + mesh gates green same week
