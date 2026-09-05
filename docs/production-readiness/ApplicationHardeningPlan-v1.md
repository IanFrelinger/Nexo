# Application hardening plan v1

Validation for **`application/src/`** (API, CLI) after [Kernel Readiness v1](KernelReadiness-v1.md).

**Automation:** `make application-gate-full`

## Tiers

| Tier | Focus | Command |
|------|--------|---------|
| A | Build + CLI smoke | `make application-gate-tier-a` |
| B | CLI tests + doctor (non-zero `doctor --json` fails the tier) | `make application-gate-tier-b` |
| C | In-process API HTTP | `make application-gate-tier-c` |
| D | Agent-server Compose dry run | `make application-gate-tier-d` |

## Prerequisites

- `make kernel-gate` green (Tier A runs it unless `APPLICATION_GATE_SKIP_KERNEL=1`)
- Docker for Tier D agent-server dry run

## Flags

| Variable | Effect |
|----------|--------|
| `APPLICATION_GATE_SKIP_KERNEL=1` | Skip `make kernel-gate` in Tier A |

## Related

- [runtime-vs-application.md](../architecture/runtime-vs-application.md)
- [Kernel Hardening Plan v1](KernelHardeningPlan-v1.md)
