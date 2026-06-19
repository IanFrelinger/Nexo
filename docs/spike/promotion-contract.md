# Promotion contract — honest CSV inferencer (phase 1)

**Brick:** `honest-csv-inferrer`  
**Catalog version:** `s1.4-v1`  
**Probe corpus version:** `s1.4-v1`  
**Signed by:** _(human approver — fill on promotion)_

## What may be promoted

| Artifact | Path | Frozen? |
| --- | --- | --- |
| Intent spec | `samples/spike-s0/intents/honest-csv-inferrer.json` | Yes — acceptance + invariants |
| Implementation template | `samples/spike-s0/template/CsvColumnInferrer/` | Yes — honest reference impl |
| Property / metamorphic tests | `samples/spike-s0/template/CsvColumnInferrer.Properties/` | Yes — agents cannot edit |
| RED tests | `samples/spike-s0/template/CsvColumnInferrer.Tests/` | Per-run (test-author role) |

## Certification record (S1.4 harness)

| Metric | Required | Observed |
| --- | --- | --- |
| Intent density | ≥ 95% threshold | 100% (18/18 probe classes, all seeds) |
| Wrong-impl escape rate | 0% (equivalence) | 0% (0/144) |
| Wrong-impl false-reject rate | 0% | 0% |
| Certification equivalence | `true` | _(from `intent-density-report.json` → `equivalence.equivalenceHolds`)_ |
| Negative control equivalence broken | `true` | _(from `negativeControl.equivalenceBroken`)_ |

Artifacts: `artifacts/s1/intent-density-report.json`, `artifacts/s1/escape-rate-report.json`, `artifacts/s1/findings.md`.

## Preconditions (all must hold)

1. **Corpus subsumes catalog** — `ProbeCatalogMapping.UnmappedTransformTags()` is empty; 18 transforms ↔ 18 probes.
2. **Multi-witness pinning** — every probe class pinned across configured seeds (default 8).
3. **Equivalence holds** — density == 1.0 ⇔ escapes == 0 within catalog scope.
4. **Negative control** — removing `["1","2","hello"] => String` and the full-column metamorphic test breaks equivalence (proves metric is not vacuous).
5. **CI ratchet** — `.github/workflows/spike-s1-escape-rate.yml` passes with `--negative-control` and equivalence check.
6. **S0 gates green** — `dotnet test src/Nexo.Tests.Spike.S0` passes.

## Explicit non-goals (out of scope for promotion)

- Adaptive / LLM adversarial generation (`NEXO_S1_ADVERSARY=llm`)
- Bricks beyond the CSV column type inferencer
- Daemon, catalog sharing, or convergence infrastructure
- Scientific-notation acceptance gap (`scientific-notation` probe documents the gap; not blocking phase-1 closeout)

## Promotion steps

1. Human reviewer confirms artifact numbers match this contract.
2. Sign below (name + date).
3. Open promotion PR from spike branch to target integration branch with:
   - Frozen spec + template paths above
   - Link to S1.4 harness artifacts
   - This contract attached or referenced

## Sign-off

| Role | Name | Date | Notes |
| --- | --- | --- | --- |
| Spike owner | | | |
| Reviewer | | | |

## Rollback

If a later catalog version regresses escape rate or intent density below baselines in `artifacts/s1/escape-rate-baseline.json`, **do not promote** until the regression is resolved or the baseline is intentionally revised with documented rationale.
