# CI: pre-existing failures (unrelated to certification)

The certification tower (atom gate, generation safety, composition gate, dogfood) is gated by **`Cert gate`** (`.github/workflows/cert-gate.yml`). That workflow is independent of the jobs below.

## Full Platform Readiness Gate — `setup · discover · dry-run`

Workflow: `.github/workflows/full-platform-readiness-gate.yml`

Jobs named **`Linux — setup · discover · dry-run`**, **`macOS — setup · discover · dry-run`**, and **`Windows — setup · discover · dry-run`** (and container variants) are **RED on `master`** as of 2026-06-21. This predates the certification work (PRs #186–#191) and is unrelated to cert-gate.

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
