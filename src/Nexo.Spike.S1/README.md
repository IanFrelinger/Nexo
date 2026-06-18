# Nexo Spike S1 — Gate Escape Rate Harness

Measures how often deliberately-wrong implementations and deliberately-weak tests **escape** the S0 verification envelope (`PropertyGate` + `MutationGate`).

## Prerequisites

- .NET 8 SDK (repo `global.json` may roll forward to newer SDKs)
- Optional: `dotnet tool install -g dotnet-stryker` for the weak-test (mutation) dimension

## Run (headless, offline default)

From repository root:

```bash
# Cheap dimension only (PropertyGate sweep; deterministic, no API keys)
dotnet run --project src/Nexo.Spike.S1 -- --seeds 8 --mutation-sample 0

# Add bounded mutation sampling when Stryker is installed
dotnet run --project src/Nexo.Spike.S1 -- --seeds 8 --mutation-sample 3 --budget-minutes 30
```

Outputs:

- `artifacts/s1/escape-rate-report.json` — machine-readable totals and per-transform breakdown
- `artifacts/s1/findings.md` — human-readable headline metrics

## CLI options

| Flag | Default | Description |
| --- | --- | --- |
| `--seeds N` | `8` | Deterministic seed sweep (catalog × seeds for wrong-impl) |
| `--mutation-sample M` | `0` | Weak-test candidates via MutationGate (`0` skips) |
| `--budget-minutes T` | `30` | Wall-clock cap for mutation dimension |
| `--out path` | `artifacts/s1` | Output directory |

## Adversary modes

| `NEXO_S1_ADVERSARY` | Behavior |
| --- | --- |
| unset / `offline` | `DefectInjectionGenerator` (default for CI and cloud agents) |
| `llm` | Seam only — requires local API credentials; **never** run in CI |

## Metric scope

The offline transform catalog is a **lower bound** on escape rate. Adaptive or LLM adversaries may find additional escapes. False-reject counts come from honest no-op baselines through the same gates.
