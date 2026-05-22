# Kernel Readiness v1

**Status: KERNEL READY FOR APPLICATIONS** (Tiers A–E automated gates green, 2026-05-19)

**Plan:** [Kernel Hardening Plan v1](KernelHardeningPlan-v1.md) · **Quarterly ops:** [Kernel Chaos Drill v1](KernelChaosDrill-v1.md)

## One command

```bash
make kernel-gate-full   # A + B + C + D + E (~10–15 min first run; Docker for E)
```

## Gate record

| Date | Gate | Result |
|------|------|--------|
| 2026-05-19 | A–E local | **PASS** |
| 2026-05-19 | mesh-lab-e2e | **PASS** |

## Tier summary

| Tier | Focus | Command | Status |
|------|--------|---------|--------|
| A | DI contracts, profiles | `make kernel-gate` | **PASS** |
| B | CLI + LiteDB resume | `make kernel-gate-tier-b` | **PASS** |
| C | ProdStyle, transport, air-gapped | `make kernel-gate-tier-c` | **PASS** |
| D | NuGet consumer sample | `make kernel-gate-tier-d` | **PASS** |
| E | OTel, perf, Compose dry run | `make kernel-gate-tier-e` | **PASS** |

## Tier E detail

- OpenTelemetry registration test
- 3 orchestration performance tests
- `prod-dry-run.sh --portal`: `/health` + `/api/status` on published API image (uses `linux/amd64` on ARM hosts)

## Next: application layer

After kernel sign-off, run [Application Readiness v1](ApplicationReadiness-v1.md):

```bash
make application-gate-full
```

## Before NuGet publish

```bash
make release-preflight VERSION=x.y.z
```

Post-publish: `nuget-consumer-verify.yml` against released version on nuget.org.

## Application work

You may build on `application/src/` when `make kernel-gate-full` is green. Re-run after kernel (`src/`, `Nexo.Hosting`) changes.

## Sign-off

- [x] Tiers A–E automated (2026-05-19)
- [ ] Quarterly chaos drill checklist completed
- [ ] Post-publish NuGet verify on release tag
