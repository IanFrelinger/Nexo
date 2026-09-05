# Compatibility hardening plan v1

Validates **schema/migration contracts**, **CLI durability across processes**, and **configuration/doctor** surfaces.

**Automation:** `make compat-gate-full`

## Tiers

| Tier | Focus | Command |
|------|--------|---------|
| A | Mesh checkpoint migration (`Ashlar.Commercial.Tests.Fleet`), LiteDB registration, composition validation | `make compat-gate-tier-a` |
| B | Cross-process LiteDB pipeline resume (kernel Tier B) | `make compat-gate-tier-b` |
| C | Configuration binding + kernel phases + doctor smoke | `make compat-gate-tier-c` |

## Flags

| Variable | Effect |
|----------|--------|
| `COMPAT_GATE_SKIP_PRIOR=1` | Skip perf gate (default in `compat-gate-full`) |

## Related

- [Compat readiness v1](CompatReadiness-v1.md)
