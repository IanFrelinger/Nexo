# Spike S0 — Self-Extension Falsification

Falsification spike for thesis S0: a single agent that writes both tests and implementation can still produce *honest* tests when a mutation-testing gate makes hollow tests un-gameable.

## Quick start

```bash
# Scaffold a fresh workspace and run the loop (file-based agents — place artifacts per stage)
nexo build --intent samples/spike-s0/intents/honest-csv-inferrer.json \
  --workspace /tmp/spike-s0-run \
  --commit

# Validate gate machinery (unit tests)
dotnet test src/Nexo.Tests.Spike.S0/Nexo.Tests.Spike.S0.csproj
```

## Loop contract

```
SPEC → RED → GREEN → VERIFY
```

| Stage | Role | Output |
|-------|------|--------|
| SPEC | human/intent file | `spec.frozen.json` |
| RED | test-author | failing tests (assertion failure, not missing symbol) |
| GREEN | implementer | minimal implementation (tests read-only) |
| VERIFY | gate | Stryker mutation score ≥ threshold |

## Adversarial intents

| File | Trap |
|------|------|
| `adversarial-off-by-one.json` | inclusive vs exclusive boundary |
| `adversarial-silent-default.json` | malformed input swallowed as default |
| `adversarial-order-dependence.json` | commutative property |
| `adversarial-tautology-bait.json` | `f(x) == f(x)` style tests |

## Instrumentation

Each run writes `spike-run-log.json` in the workspace with guard rejections, mutation scores, RED-reason classification, and bounce count.

See `docs/spikes/S0-Self-Extension.md` for full spec.
