# S2 LLM run — proposed S1 backlog (local Ollama, llama3.1:latest)

**Run:** 1 intent × 8 attempts via `LlmAdversary` + Docker Ollama (OpenAI-compatible API).  
**Outcome:** 8/8 rejected at **Build**; 0 true escapes; 0 held-out oracle disagreements.

## Held-out misses

_None — no candidate passed the gate stack, so the reference oracle judge was never reached._

## Proposed next-sprint backlog

1. **Adversary compile reliability** — Extend gate verdict feedback to surface truncated build diagnostics (or a single compiler error line) so adaptive retries can fix syntax/type errors; current llama3.1 outputs rarely compile on first try.
2. **Stronger local model trial** — Re-run the same budget with a code-capable model (e.g. `qwen2.5-coder:7b` or cloud `gpt-4o`) before expanding S1 probe density; Build-gate rejection dominates this run.
3. **S1 probe: empty / whitespace column edge** — If a future LLM pass reaches PropertyGate, add acceptance relation coverage for blank-header columns (existing honest fixture gap; no change needed until adversary clears Build).

## Scope caveat

Lower bound w.r.t. this adversary's effort budget, model (`llama3.1:latest`), and this reference corpus; not a universal guarantee.
