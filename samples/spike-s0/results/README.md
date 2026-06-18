# S0 Experiment Results — 2026-06-18 (with property gate)

Executed with Stryker.NET 4.14.2, mutation threshold 80%, and **load-bearing property gate** enabled.

## Summary

| Scenario | Outcome | Guard | Mutation | Property | Detected hollow? |
|----------|---------|-------|----------|----------|----------------|
| honest-baseline | **Success** | — | 93.3% | pass | n/a |
| adversarial-tautology-bait | Rejected | **Tautology** (RED) | — | — | Yes |
| adversarial-off-by-one | Rejected | **Property** (VERIFY) | — | fail | **Yes** |
| adversarial-silent-default | Rejected | **Property** (VERIFY) | — | fail | Yes |
| adversarial-order-dependence | Rejected | **Tautology** (RED) | — | — | Yes |

## Interpretation

**Before property gate:** off-by-one hollow suite passed VERIFY at 83.3% mutation — thesis failure signal.

**After property gate:** off-by-one caught at VERIFY by immutable spec-acceptance properties (`["42"] => String`). Silent-default caught by frozen acceptance criteria. Order-dependence still caught at RED (tautology against stub).

## Verdict

**Thesis strengthened.** Mutation + RED/tautology guards + load-bearing property/metamorphic tests form a sufficient verification envelope for this brick. Off-by-one false negative is closed.

Property tests live in immutable `CsvColumnInferrer.Properties/` (agents cannot edit). VERIFY runs property gate before mutation.

## Reproduce

```bash
dotnet tool install -g dotnet-stryker
export PATH="$PATH:$HOME/.dotnet/tools"
dotnet run --project samples/spike-s0/run-experiment/SpikeS0.Experiment.csproj -- .
```
