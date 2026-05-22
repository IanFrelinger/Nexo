# Operations & dogfood readiness v1

**Status: OPS GATE GREEN** (Tiers A–C, E default profile, 2026-05-19)

Track after [Ship readiness v1](ShipReadiness-v1.md). **Plan:** [Ops hardening plan v1](OpsHardeningPlan-v1.md)

## Command

```bash
make ops-gate-full
make nexo-ready-gate   # full stack (use NEXO_READY_SKIP_DOCKER=1 locally for speed)
```

## Record

| Date | Gate | Result | Notes |
|------|------|--------|-------|
| 2026-05-19 | A–C, E local | **PASS** | Tier D mesh/chaos optional |

## Tier summary

| Tier | Focus | Status |
|------|--------|--------|
| A | Dogfood blocks 1–6 | **PASS** |
| B | Dogfood blocks 7–9 + IPC mesh | **PASS** |
| C | Closed-loop self-improvement | **PASS** |
| D | Mesh deep / chaos-lite | optional |
| E | Oh-shit demo (quick) | **PASS** |

## Next: security & trust

```bash
make security-gate-full
```

See [Security readiness v1](SecurityReadiness-v1.md).

## Sign-off

- [x] `ops-gate` default tiers green (2026-05-19)
- [x] Ship gate green same week
