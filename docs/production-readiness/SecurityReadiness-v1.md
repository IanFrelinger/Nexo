# Security & trust readiness v1

**Status: SECURITY GATE GREEN** (Tiers A–E, 2026-05-19)

Track after [Ops readiness v1](OpsReadiness-v1.md). **Plan:** [Security hardening plan v1](SecurityHardeningPlan-v1.md)

## Command

```bash
make security-gate-full
```

## Record

| Date | Gate | Result | Notes |
|------|------|--------|-------|
| 2026-05-19 | A–E local | **PASS** | Supply-chain: app + Hosting + Infrastructure; xunit deprecated warning |

## Tier summary

| Tier | Focus | Command | Status |
|------|--------|---------|--------|
| A | Trust core | `make security-gate-tier-a` | **PASS** |
| B | API security middleware | `make security-gate-tier-b` | **PASS** |
| C | Trust CLI | `make security-gate-tier-c` | **PASS** |
| D | Supply chain | `make security-gate-tier-d` | **PASS** |
| E | Air-gapped + safety | `make security-gate-tier-e` | **PASS** |

## Next: release candidate

```bash
make rc-gate-full
```

See [RC hardening plan v1](RCHardeningPlan-v1.md) and [Release candidate checklist v1](../ReleaseCandidateChecklist-v1.md).

## Sign-off

- [x] `security-gate-full` green (2026-05-19)
- [x] Tier D reports in `.ashlar/security-gate/` (no High/Critical in scanned surfaces)
- [x] Air-gapped tier verified (in-process safety + profile tests)
