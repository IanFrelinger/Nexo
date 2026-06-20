# S2 Adaptive Adversary Escape Rate

## CORRECTION

A prior artifact labeled "real LLM adaptive-adversary run" was an empty stub/Ollama run (`backend=llm`, `trueEscapeRate=0`, `nonVacuityProven=false`) with **no API key** and **no adaptive behavior**. That vacuous-zero report was misleading and is superseded by this canonical mock baseline. A real keyed LLM escape rate is still pending a local run.

## Headline

- **Report version**: `s2.0-v1`
- **Reference oracle version**: `s2.0-v1`
- **Adversary backend**: `mock`
- **Effort budget**: 1 intent(s) × 3 attempt(s)
- **True-escape rate**: **33.3%** (1/3)
- **Benign-pass rate**: **33.3%** (1/3)
- **Rejection rate**: **33.3%** (1/3)
- **Non-vacuity proven**: True

## Scope caveat

Lower bound w.r.t. this adversary's effort budget and this reference corpus; not a universal guarantee of correctness or exhaustive adaptive search.

## Attempts-to-first-true-escape

- Intent 0: 2

## New-defect backlog (held-out oracle disagreements)

| Inputs | Expected | Actual | Attempt | Candidate |
| --- | --- | --- | ---: | --- |
| `["999"]` | Integer | String | 2 | `mock-true-escape-held-out-999` |

## Per-attempt outcomes

| Intent | Attempt | Candidate | Outcome | Rejected by |
| ---: | ---: | --- | --- | --- |
| 0 | 1 | `mock-rejected-constant-return` | Rejected | RED |
| 0 | 2 | `mock-true-escape-held-out-999` | TrueEscape | — |
| 0 | 3 | `mock-benign-pass-honest` | BenignPass | — |

## Local LLM command

```bash
export NEXO_S2_ADVERSARY=llm
export OPENAI_API_KEY=...   # or ANTHROPIC_API_KEY / NEXO_LLM_API_KEY
export NEXO_S2_LLM_MODEL=gpt-4o   # optional metadata recorded in report
export PATH="$HOME/.dotnet/tools:$PATH"
dotnet run --project src/Nexo.Spike.S2 -- --intents 1 --attempts 8 --out artifacts/s2
```
