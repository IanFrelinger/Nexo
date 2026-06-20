# S2 Adaptive Adversary Escape Rate

## Headline

- **Report version**: `s2.0-v1`
- **Reference oracle version**: `s2.0-v1`
- **Adversary backend**: `cursor-standin`
- **Effort budget**: 1 intent(s) × 8 attempt(s)
- **True-escape rate**: **25.0%** (2/8)
- **Benign-pass rate**: **37.5%** (3/8)
- **Rejection rate**: **37.5%** (3/8)
- **Non-vacuity proven**: True

## Scope caveat

Lower bound w.r.t. this adversary's effort budget and this reference corpus; not a universal guarantee of correctness or exhaustive adaptive search.

## Attempts-to-first-true-escape

- Intent 0: 3

## New-defect backlog (held-out oracle disagreements)

| Inputs | Expected | Actual | Attempt | Candidate |
| --- | --- | --- | ---: | --- |
| `["999"]` | Integer | String | 3 | `cursor-attempt-03` |
| `["1e3"]` | String | Decimal | 6 | `cursor-attempt-06` |

## Per-attempt outcomes

| Intent | Attempt | Candidate | Outcome | Rejected by |
| ---: | ---: | --- | --- | --- |
| 0 | 1 | `cursor-attempt-01` | Rejected | RED |
| 0 | 2 | `cursor-attempt-02` | Rejected | RED |
| 0 | 3 | `cursor-attempt-03` | TrueEscape | — |
| 0 | 4 | `cursor-attempt-04` | BenignPass | — |
| 0 | 5 | `cursor-attempt-05` | Rejected | RED |
| 0 | 6 | `cursor-attempt-06` | TrueEscape | — |
| 0 | 7 | `cursor-attempt-07` | BenignPass | — |
| 0 | 8 | `cursor-attempt-08` | BenignPass | — |

## Local LLM command

```bash
export NEXO_S2_ADVERSARY=llm
export OPENAI_API_KEY=...   # or ANTHROPIC_API_KEY / NEXO_LLM_API_KEY
export NEXO_S2_LLM_MODEL=gpt-4o   # optional metadata recorded in report
export PATH="$HOME/.dotnet/tools:$PATH"
dotnet run --project src/Nexo.Spike.S2 -- --intents 1 --attempts 8 --out artifacts/s2
```
