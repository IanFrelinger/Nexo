# Application Readiness v1

**Status: APPLICATION GATE GREEN** (Tiers A–D, 2026-05-19)

Track after [Kernel Readiness v1](KernelReadiness-v1.md). **Plan:** [Application Hardening Plan v1](ApplicationHardeningPlan-v1.md)

## Command

```bash
make application-gate-full   # after make kernel-gate-full (~2–3 min without Docker rebuild)
```

## Record

| Date | Gate | Result | Notes |
|------|------|--------|-------|
| 2026-05-19 | A–D local | **PASS** | agent-server `/health` + `/api/status` |

## Tier summary

| Tier | Focus | Command | Status |
|------|--------|---------|--------|
| A | Product build + CLI validate | `make application-gate-tier-a` | **PASS** |
| B | CLI tests + doctor | `make application-gate-tier-b` | **PASS** |
| C | In-process API HTTP | `make application-gate-tier-c` | **PASS** |
| D | Agent-server Compose | `make application-gate-tier-d` | **PASS** |

## Next: composition & mesh

```bash
make composition-mesh-gate-full
```

See [Composition & mesh readiness v1](CompositionMeshReadiness-v1.md).

## Sign-off

- [x] `application-gate-full` green (2026-05-19)
- [x] Kernel gate green same day
