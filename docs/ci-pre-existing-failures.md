# CI: pre-existing failures (historical — resolved 2026-08-16)

> **Historical (marked 2026-08-16).** The Full Platform Readiness Gate described below went **green on `master`** with PRs #317–#320 (skipped ≠ failed in `nexo validate`, per-project target framework, prebuilt CLI in the smoke, Docker `/health` smoke instead of the 90-minute hang) and the flake fixes in #335. It is path-filtered and has no PR trigger, so a red run there is now a real regression, not background noise. `docs/planning/LAND-STATUS.md` and `docs/planning/MERGE-READINESS.md` still cite this file for the 2026-06/07 landings; the text below is kept unchanged as that record.

The certification tower (atom gate, generation safety, composition gate, dogfood) is gated by **`Cert gate`** (`.github/workflows/cert-gate.yml`). That workflow is independent of the jobs below.

## Full Platform Readiness Gate — `setup · discover · dry-run`

Workflow: `.github/workflows/full-platform-readiness-gate.yml`

Jobs named **`Linux — setup · discover · dry-run`**, **`macOS — setup · discover · dry-run`**, and **`Windows — setup · discover · dry-run`** (and container variants) were **RED on `master`** as of 2026-06-21 (see the banner above for the resolution). This predated the certification work (PRs #186–#191) and was unrelated to cert-gate.

Example failing runs on `master`:

- [Full Platform Readiness Gate (PR #173 merge)](https://github.com/IanFrelinger/Nexo/actions/runs/27722763291) — `conclusion: failure`

### Recommendation (not performed here)

If these jobs block merge noise during the next phase, consider marking them **non-required** in branch protection until the underlying CLI dry-run/discover issues are fixed. Do **not** confuse their failure with certification regressions — always check **`cert-gate`** separately.

## How to distinguish certification failures

| Check | What it proves |
|-------|----------------|
| `cert-gate` | Atom/composition/generation/dogfood certification tests (`bash scripts/run-cert-gate.sh`) |
| Full platform readiness | CLI bootstrap/doctor dry-run on Linux/macOS/Windows |

When triaging a PR from the certification tower, a green **`cert-gate`** check is the authoritative signal for certification merge-readiness.
