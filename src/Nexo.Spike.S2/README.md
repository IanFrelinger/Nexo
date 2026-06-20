# Spike S2 — Adaptive adversary vs independent oracle

Phase 2 opener: measures whether an **adaptive** adversary finds **true escapes** (passes RED + PropertyGate + MutationGate, disagrees with held-out reference oracle).

## Quick start (mock — CI/cloud safe)

```bash
export PATH="$HOME/.dotnet/tools:$PATH"
dotnet run --project src/Nexo.Spike.S2 -- --intents 1 --attempts 3 --out artifacts/s2
```

Canonical headline artifacts (`artifacts/s2/adaptive-escape-report.json`, `findings.md`) update **only** when a **valid mock run** proves non-vacuity. All runs also land under `artifacts/s2/runs/<backend>-<version>/`.

## Local LLM run (never in CI)

Requires `NEXO_S2_ADVERSARY=llm` and a provider API key at **call time** (never logged or persisted).
Unconfigured/stub LLM runs write `runs/llm-*-stub-*/` with `.INVALID` — never the canonical report.

```bash
export NEXO_S2_ADVERSARY=llm
export OPENAI_API_KEY=...   # or ANTHROPIC_API_KEY / NEXO_LLM_API_KEY
export NEXO_S2_LLM_MODEL=gpt-4o
export PATH="$HOME/.dotnet/tools:$PATH"
dotnet run --project src/Nexo.Spike.S2 -- --intents 1 --attempts 8 --out artifacts/s2
```

## Scripted stand-in (offline harness only)

`NEXO_S2_ADVERSARY=scripted-standin` replays hand-authored candidates by attempt index (non-adaptive). Output is confined to `runs/scripted-standin-s2.0-v1/` and is **never** promoted to canonical.

## Integrity model

- **Reference oracle**: `artifacts/s2/reference-oracle.json` — frozen labeled corpus, strict superset of gate-pinned acceptance inputs.
- **True escape**: passes full S1 gate stack **and** disagrees with oracle on ≥1 **held-out** label.
- **Non-vacuity guard**: vacuous runs (`nonVacuityProven=false`) cannot become the canonical headline report.
- **Adversary**: implementer role only; never sees the reference oracle; cannot edit tests/properties.

## Version

`s2.0-v1`
