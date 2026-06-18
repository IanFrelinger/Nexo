# Spike S0 — Self-Extension Falsification

**Status:** Draft v0 (scaffold)  
**Type:** Falsification spike — not a milestone  
**Autonomy rung:** 1 (human approves merge)

## Thesis

> A single agent that writes both tests and implementation will still produce *honest* tests, because a mutation-testing gate makes hollow tests un-gameable from the inside.

This spike exists to falsify or support that claim cheaply, before building daemon, catalog, sharing, or convergence infrastructure.

## What ships in this scaffold

| Component | Location |
|-----------|----------|
| TDD loop runner (SPEC/RED/GREEN/VERIFY) | `src/Nexo.Spike.S0/` |
| CLI entry (`nexo build`, synchronous) | `application/src/Nexo.CLI/Commands/BuildCommand.cs` |
| Target brick template (CSV inferencer) | `samples/spike-s0/template/` |
| Adversarial intents | `samples/spike-s0/intents/` |
| Gate unit + integration tests | `src/Nexo.Tests.Spike.S0/` |

## Loop contract

```
intent → SPEC → RED → GREEN → VERIFY
```

- **SPEC:** structured intent JSON → `spec.frozen.json` (reject if open questions)
- **RED:** test-author role; tests must fail for assertion reasons, not missing symbols; tautology guard rejects tests that pass against stub
- **GREEN:** implementer role; tests are read-only after RED commit
- **VERIFY:** Stryker.NET mutation score ≥ threshold (default 80%)

Each stage can optionally commit (`--commit`).

## Role separation

| Role | Sees | Produces |
|------|------|----------|
| test-author | frozen spec | tests only |
| implementer | spec + frozen tests | implementation only |

Enforced structurally via `RoleScope` and separate `IStageAgent` instances.

## Running the spike

```bash
nexo build \
  --intent samples/spike-s0/intents/honest-csv-inferrer.json \
  --workspace /tmp/spike-s0-run \
  --skip-mutation   # omit once Stryker is installed

dotnet test src/Nexo.Tests.Spike.S0/Nexo.Tests.Spike.S0.csproj
```

With Stryker installed (`dotnet tool install -g dotnet-stryker`), drop `--skip-mutation` for full VERIFY.

## Kill criterion

**Fail (thesis killed):** agent writes tautological or implementation-mirroring tests AND mutation score stays high.

**Pass:** honest intent reaches green with human-confirmed test honesty; at least one adversarial intent is caught by tautology guard, RED-reason check, or surviving mutants.

## Instrumentation

`spike-run-log.json` per run records:

- rejecting guard and stage
- RED-reason classification
- mutation score and surviving mutants
- RED↔GREEN bounce count
- human verdict note (manual field)

## Deferred (out of S0 scope)

- Daemon / async / job IDs
- Catalog / discovery / sharing
- Sandbox / network isolation (pure-function target defers this)
- Autonomy > rung 1
