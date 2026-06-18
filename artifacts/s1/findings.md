# S1 Gate Escape Rate

## Headline

- **Adversary**: `offline` (offline taxonomy = lower bound; not adaptive/LLM)
- **Seeds**: 8
- **Wrong-impl escape rate** (PropertyGate): **0.0%** (0/48 adversarial candidates escaped)
- **Wrong-impl false-reject rate**: 0.0%
- **Weak-test dimension**: skipped:mutation-sample-zero (MutationGate escape rate: n/a)

## Tool availability

- dotnet: available
- dotnet-stryker: available (dotnet stryker --help)

## Wrong-impl per-transform breakdown

| Transform | Total | Escapes | Caught | Escape rate |
| --- | ---: | ---: | ---: | ---: |
| `OffByOne` | 8 | 0 | 8 | 0.0% |
| `BoundaryInclusive` | 8 | 0 | 8 | 0.0% |
| `NegatedCondition` | 8 | 0 | 8 | 0.0% |
| `DroppedBranch` | 8 | 0 | 8 | 0.0% |
| `ConstantReturn` | 8 | 0 | 8 | 0.0% |
| `SwappedOperands` | 8 | 0 | 8 | 0.0% |

## Metric scope

This report measures escape rate for a **fixed offline transform catalog** applied to the S0 CSV inferencer fixtures. It is a deterministic lower bound: adaptive or LLM-generated adversaries may find additional escapes. False-reject counts come from honest no-op baselines run through the same gates.
