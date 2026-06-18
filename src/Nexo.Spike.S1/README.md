# Nexo Spike S1 — Gate Escape Rate Harness

Measures how often deliberately-wrong implementations and deliberately-weak tests **escape** the S0 verification envelope (`PropertyGate` + `MutationGate`).

## Prerequisites

- .NET 8 SDK (repo `global.json` may roll forward to newer SDKs)
- Optional: `dotnet tool install -g dotnet-stryker` for the weak-test (mutation) dimension

## Run (headless, offline default)

From repository root:

```bash
# Full run: wrong-impl sweep + weak-test mutation sample (default mutation-sample=3)
dotnet run --project src/Nexo.Spike.S1 -- --seeds 8 --mutation-sample 3 --budget-minutes 45

# Wrong-impl only (skip mutation dimension)
dotnet run --project src/Nexo.Spike.S1 -- --seeds 8 --mutation-sample 0
```

Outputs:

- `artifacts/s1/escape-rate-report.json` — machine-readable totals and per-transform breakdown
- `artifacts/s1/findings.md` — human-readable headline metrics

## CLI options

| Flag | Default | Description |
| --- | --- | --- |
| `--seeds N` | `8` | Deterministic seed sweep (catalog × seeds for wrong-impl) |
| `--mutation-sample M` | `3` | Weak-test candidates via MutationGate (`0` skips) |
| `--budget-minutes T` | `30` | Wall-clock cap for mutation dimension |
| `--out path` | `artifacts/s1` | Output directory |

## Adversary modes

| `NEXO_S1_ADVERSARY` | Behavior |
| --- | --- |
| unset / `offline` | `DefectInjectionGenerator` (default for CI and cloud agents) |
| `llm` | Seam only — requires local API credentials; **never** run in CI |

## Metric scope

Catalog version `s1.1-v1` includes coarse and **semantic** wrong-impl transforms designed to probe gaps in the frozen property oracle. Escapes are signal — each names a missing property relation. This is not a target of 0%; attributed escapes form the property-authoring backlog. Adaptive or LLM adversaries may find additional escapes.
