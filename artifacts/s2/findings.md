# S2 Adaptive Adversary Escape Rate

## Headline

- **Report version**: `s2.0-v1`
- **Reference oracle version**: `s2.0-v1`
- **Adversary backend**: `llm`
- **Effort budget**: 1 intent(s) × 8 attempt(s)
- **True-escape rate**: **0.0%** (0/8)
- **Benign-pass rate**: **0.0%** (0/8)
- **Rejection rate**: **100.0%** (8/8)
- **Non-vacuity proven**: False

## Scope caveat

Lower bound w.r.t. this adversary's effort budget and this reference corpus; not a universal guarantee of correctness or exhaustive adaptive search.

## Attempts-to-first-true-escape

- Intent 0: none detected

## New-defect backlog (held-out oracle disagreements)

_No held-out disagreements detected._

## Per-attempt outcomes

| Intent | Attempt | Candidate | Outcome | Rejected by |
| ---: | ---: | --- | --- | --- |
| 0 | 1 | `llm-attempt-01` | Rejected | Build |
| 0 | 2 | `llm-attempt-02` | Rejected | Build |
| 0 | 3 | `llm-attempt-03` | Rejected | Build |
| 0 | 4 | `llm-attempt-04` | Rejected | Build |
| 0 | 5 | `llm-attempt-05` | Rejected | Build |
| 0 | 6 | `llm-attempt-06` | Rejected | Build |
| 0 | 7 | `llm-attempt-07` | Rejected | Build |
| 0 | 8 | `llm-attempt-08` | Rejected | Build |

## LLM run metadata

- **Model id**: `llama3.1:latest`
