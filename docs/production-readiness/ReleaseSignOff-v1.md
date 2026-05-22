# Release sign-off v1

Complete before tagging a release candidate.

## Checklist

- [x] Product: scope and known limitations accepted (RC gate stack; strict GH workflows pending green on `master`)
- [x] Engineering: all automated gates green for target SHA (local: `waterproofing-gate-full`, `ship-gate-tier-d` with runtime gate)
- [x] Security: exceptions file reviewed (`docs/exceptions.yaml` — no High/Critical entries)
- [x] Operations: rollback drill recorded ([Rollback drill v1](RollbackDrill-v1.md))

## Sign-off record

| Role | Name | Date | SHA |
|------|------|------|-----|
| Product | Ian Frelinger | 2026-05-22 | `bec2a6ed` |
| Engineering | Nexo readiness gates (automated) | 2026-05-22 | `bec2a6ed` |
| Security | Nexo security-gate (automated) | 2026-05-22 | `bec2a6ed` |
| Operations | DR gate + mesh-lab persistence | 2026-05-22 | `bec2a6ed` |

## Local gate evidence (2026-05-22)

| Gate | Command | Result |
|------|---------|--------|
| Waterproofing | `make waterproofing-gate-full` | PASS |
| Ship runtime | `SHIP_GATE_RUN_RUNTIME_GATE=1 make ship-gate-tier-d` | PASS |
| DR | `make dr-gate-full` | PASS (mesh director persistence verified) |

GitHub RC workflows: triggered on `master` after merge #114; run `RC_GATE_TRIGGER_GH=1 make rc-gate-tier-d` for strict verification.
