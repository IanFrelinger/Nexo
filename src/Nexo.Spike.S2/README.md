# Spike S2 — Adaptive adversary vs independent oracle

Phase 2 opener: measures whether an **adaptive** adversary finds **true escapes** (passes RED + PropertyGate + MutationGate, disagrees with held-out reference oracle).

## Quick start (mock — CI/cloud safe)

```bash
export PATH="$HOME/.dotnet/tools:$PATH"
dotnet run --project src/Nexo.Spike.S2 -- --intents 1 --attempts 3 --out artifacts/s2
```

## Local LLM run (never in CI)

```bash
export NEXO_S2_ADVERSARY=llm
export OPENAI_API_KEY=...   # or ANTHROPIC_API_KEY / NEXO_LLM_API_KEY
export NEXO_S2_LLM_MODEL=gpt-4o
export PATH="$HOME/.dotnet/tools:$PATH"
dotnet run --project src/Nexo.Spike.S2 -- --intents 1 --attempts 8 --out artifacts/s2
```

Commit the resulting `artifacts/s2/adaptive-escape-report.json` and `findings.md` from local LLM runs.

## Integrity model

- **Reference oracle**: `artifacts/s2/reference-oracle.json` — frozen labeled corpus, strict superset of gate-pinned acceptance inputs.
- **True escape**: passes full S1 gate stack **and** disagrees with oracle on ≥1 **held-out** label.
- **Adversary**: implementer role only; never sees the reference oracle; cannot edit tests/properties.

## Version

`s2.0-v1`
