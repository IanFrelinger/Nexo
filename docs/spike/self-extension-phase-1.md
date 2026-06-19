# Self-extension phase 1 — S0 through S1.4 closeout

**Status:** Phase-1 closeout (`s1.4-v1`)  
**Type:** Falsification spike — not a production milestone  
**Autonomy rung:** 1 (human approves merge)

## Arc

| Phase | Version | Intent density | Wrong-impl escapes | Certification | Notes |
| --- | --- | ---: | ---: | --- | --- |
| S1.2 | `s1.2-v1` | 33.3% (4/12) | 73/144 (50.7%) | NotCertifiable | Sparse frozen spec; seed perturbation introduced |
| S1.3 | `s1.3-v1` | 100.0% (12/12) | 8/144 (5.6%) | Certifiable | Densified spec; **metric over-claimed** — density 1.0 but 8 escapes remained |
| S1.4 | `s1.4-v1` | 100.0% (18/18) | 0/144 (0.0%) | Certifiable | Faithful metric: density 1.0 ⇔ escapes 0 (scoped) |

### S1.3 → S1.4 delta

| Change | Detail |
| --- | --- |
| Probe corpus expanded | 12 → **18** probe classes (6 coarse transforms now have dedicated probes) |
| Multi-witness pinning | Pinning requires honest pass + divergent caught/vacuous across **all** seeds (default 8) |
| Behavioral vacuity | Property-gate passes with identical witness outputs reclassified as caught, not escapes |
| Residual escapes closed | `SemanticSamplingWindow` — acceptance `["1","2","hello"] => String` + metamorphic full-column test; `SwappedOperands` odd-seed vacuity |
| Capstone equivalence | `CertificationEquivalence`: within fixed catalog + seed range, density == 1.0 iff escapes == 0 |
| Negative control | Stripped sampling-window relation → both density &lt; 1.0 and escapes &gt; 0 |

## Thesis (carried from S0)

> A single agent that writes both tests and implementation will still produce *honest* tests, because property and mutation gates make hollow tests un-gameable from the inside.

Phase 1 adds a **certification metric** on top of S0 gates: intent density measures how completely the frozen oracle pins adversarial defect classes.

## Components

| Component | Location |
| --- | --- |
| S0 TDD loop + gates | `src/Nexo.Spike.S0/` |
| Escape-rate harness | `src/Nexo.Spike.S1/EscapeRateHarness.cs` |
| Intent density analyzer | `src/Nexo.Spike.S1/IntentDensity/` |
| Probe corpus + mapping | `ProbeCorpus.cs`, `ProbeCatalogMapping.cs` |
| Certification equivalence | `CertificationEquivalence.cs` |
| Negative control | `NegativeControlStripping.cs`, `honest-csv-inferrer-negative-control.json` |
| Artifacts | `artifacts/s1/` |
| CI (manual + weekly) | `.github/workflows/spike-s1-escape-rate.yml` |

## Running the harness

```bash
export PATH="$HOME/.dotnet/tools:$PATH"
dotnet run --project src/Nexo.Spike.S1 -- \
  --seeds 8 \
  --mutation-sample 4 \
  --budget-minutes 45 \
  --negative-control \
  --out artifacts/s1
```

Exit code **0** iff certification equivalence holds on the primary (non–negative-control) run.

## Equivalence scope

The capstone property is **scoped**, not universal:

> Within the fixed offline transform catalog and configured seed range, intent density == 1.0 iff behavioral wrong-impl escape count == 0.

See `CertificationEquivalence.ScopeNote` in code. Adaptive or LLM adversaries may find escapes beyond this taxonomy.

## Phase-1 recommendation

**Merge vs archive:** Phase 1 is **ready to merge** into the spike branch stack as a signed closeout record. The honest CSV inferencer brick and its frozen spec are promotion candidates under `docs/spike/promotion-contract.md`. Do **not** promote to stable SDK surface without human sign-off on the promotion contract.

## Related docs

- [S0 falsification spike](../spikes/S0-Self-Extension.md)
- [Probe catalog mapping](./probe-catalog-mapping.md)
- [Promotion contract](./promotion-contract.md)
