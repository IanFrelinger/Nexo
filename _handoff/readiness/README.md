# Readiness convergence (agent-run)

An autonomous, hierarchical agent pipeline that drives repo layers to a green
readiness gate, layer by layer, starting with `application/`. Set up 2026-08-25
on branch `claude/recursing-franklin-cbb828`.

## The pieces

- `scripts/readiness-gate-local.sh` — the objective function. Runs the same
  tier scripts CI's application-gate runs (tiers A+B+C, tier D optional) plus
  the full Ashlar.Tests.CLI suite, and emits a JSON verdict. Green here ⇒
  green in CI's application gate, and then some. "Production ready" for a
  layer = this gate green on two consecutive cycles.
- `.claude/agents/readiness-{gate-runner,fixer,verifier,integrator}.md` — the
  four roles. Fixers work in isolated worktrees; verifiers adversarially try
  to refute each fix; the integrator cherry-picks confirmed fixes and regates.
- `.claude/workflows/readiness-convergence.js` — one convergence cycle:
  Gate → Fix → Verify → Integrate, pipelined per failure, one bounded retry
  per refuted fix.
- `.claude/commands/converge-readiness.md` — the orchestrator prompt. Run
  `/converge-readiness` for one cycle, or `/loop /converge-readiness` for
  autonomous convergence with stop conditions.
- `LEDGER.md` (this directory) — append-only cycle log: gate tables, fixes
  landed, items parked for a human decision.

## Ground rules baked into the roles

Builds and tests run only in the dotnet-10 dev container (`docker exec`); git
runs only on the host (linked-worktree gitdirs don't resolve in-container).
Agents never push and never touch `master` — integration accumulates on the
local `claude/…` branch until a human opens the PR. Product decisions are
parked in the ledger, not guessed.

## Extending to the other layers

`readiness-gate-local.sh --layer applications|apps` currently refuses: those
layers first need their gate command lists defined in the script (mirroring
whichever CI workflows own them), after which the same pipeline converges them
unchanged.
