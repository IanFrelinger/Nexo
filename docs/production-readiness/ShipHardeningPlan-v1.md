# Ship hardening plan v1

End-to-end **ship readiness** after [Composition & mesh readiness v1](CompositionMeshReadiness-v1.md): production gate CLI flows, CI verify, release preflight, and release bundle.

**Automation:** `make ship-gate-full`

## Prerequisites

```bash
make composition-mesh-gate-full   # or COMPOSITION_MESH_GATE_SKIP_TIER_D=1 for in-process only
```

## Tiers

| Tier | Focus | Command |
|------|--------|---------|
| A | Production Readiness Gate v1 (CLI + LiteDB resume) | `make ship-gate-tier-a` |
| B | ProdStyle + smoke + doctor | `make ship-gate-tier-b` (`SHIP_GATE_RUN_CI_VERIFY=1` for full `ci verify`) |
| C | Release preflight (local feed consumer) | `make ship-gate-tier-c` |
| D | Doctor sign-off (`SHIP_GATE_RUN_RUNTIME_GATE=1` adds release bundle) | `make ship-gate-tier-d` |

## Flags

| Variable | Effect |
|----------|--------|
| `SHIP_GATE_SKIP_PRIOR=1` | Skip `composition-mesh-gate` on full run |
| `SHIP_GATE_SKIP_TIER_B=1` | Skip `ci verify` (heavy ProdStyle) |
| `SHIP_GATE_SKIP_TIER_C=1` | Skip NuGet preflight |
| `SHIP_GATE_SKIP_TIER_D=1` | Skip release bundle |
| `SHIP_GATE_VERSION=x.y.z` | Preflight version (default: canonical `VERSION` file; must be valid semver) |
| `SHIP_GATE_RUN_RUNTIME_GATE=1` | Run `ci release-bundle` (includes runtime SLO gate) |
| `SHIP_GATE_BUNDLE_PROFILE=quick\|default\|full` | Release bundle profile when runtime gate enabled |

## Related

- [Production Readiness Gate v1](../ProductionReadinessGate-v1.md)
- [Release and promotion](ReleaseAndPromotion.md)
