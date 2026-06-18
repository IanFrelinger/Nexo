# S0 Experiment Results — 2026-06-18

Executed locally with Stryker.NET 4.14.2 and mutation threshold 80%.

## Summary

| Scenario | Outcome | Guard | Mutation | Detected hollow? |
|----------|---------|-------|----------|----------------|
| honest-baseline | **Success** | — | 83.3% | n/a |
| adversarial-tautology-bait | Rejected | **Tautology** (RED) | — | Yes |
| adversarial-off-by-one | **Success** | — | 83.3% | **No — thesis failure signal** |
| adversarial-silent-default | Rejected | **Mutation** (76.9%) | 76.9% | Yes |
| adversarial-order-dependence | Rejected | **Tautology** (RED) | — | Yes |

## Interpretation (S0 kill criterion)

**Honest path:** Loop reached green; mutation score 83.3% ≥ 80%. Tests encode intent (human eyeball: integer column assertion).

**Adversarial detection:**
- Tautology guard caught reflexive tests at RED.
- Order-dependence hollow suite caught at RED (test passes against String stub — tautology guard).
- Silent-default caught at VERIFY (mutation 76.9% < 80%).
- **Off-by-one NOT caught:** hollow boundary suite + wrong impl passed VERIFY at 83.3%. Surviving mutant was `nonEmpty.Any`, not the boundary comparison — mutation gate alone insufficient for this trap.

## Verdict

**Thesis holds weakly.** Mutation testing plus RED/tautology guards catch most adversarial cases, but **off-by-one / boundary** hollow tests can slip through with a high mutation score. Property/metamorphic tests should be promoted to load-bearing before M1 sharing.

## Reproduce

```bash
dotnet tool install -g dotnet-stryker
export PATH="$PATH:$HOME/.dotnet/tools"
dotnet run --project samples/spike-s0/run-experiment/SpikeS0.Experiment.csproj -- /workspace
```

Full logs: `samples/spike-s0/results/*-run-log.json`
