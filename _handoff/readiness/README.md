# Readiness convergence (agent-run)

An autonomous, hierarchical agent pipeline that drives repo layers to a green
readiness gate, layer by layer: `application/` (converged 2026-08-26), then
`applications/`, then `apps/`. Set up 2026-08-25 on branch
`claude/recursing-franklin-cbb828`; migrated to a container-first (v2)
architecture 2026-08-26 for unattended overnight runs.

## The pieces

- `scripts/readiness-gate-local.sh` — the objective function. Per layer, runs
  the same commands the layer-owning CI workflows run, plus strictly stronger
  local suites CI lacks. Green here ⇒ green in the layer's CI gates, and then
  some. "Production ready" for a layer = this gate green on two consecutive
  cycles.
- `scripts/readiness-container-setup.sh` — idempotent container provisioning:
  .NET 8 runtime (test projects target net8.0), git identity, and the agent
  clone.
- `.claude/agents/readiness-{gate-runner,fixer,verifier,integrator}.md` — the
  four roles. Fixers work in isolated worktrees; verifiers adversarially try
  to refute each fix; the integrator cherry-picks confirmed fixes and regates.
- `.claude/workflows/readiness-convergence.js` — one convergence cycle:
  Gate → Fix → Verify → Integrate, pipelined per failure, one bounded retry
  per refuted fix.
- `.claude/commands/converge-readiness.md` — the orchestrator prompt. Run
  `/converge-readiness` for one cycle, or `/loop /converge-readiness` for
  autonomous convergence with stop conditions.
- `STATE-2026-08-27.md` — completion audit: how done the project is against
  six definitions of "done", the critical path in dependency order, the work
  that looks urgent but is not, and the decisions only the owner can make.
  Start here if you are picking the project up cold.
- `DECISION-identity-split.md` — the one-identity-or-two decision (open decision 2),
  researched against the code: the recommendation, the two rejected options and why, and
  the finding that the security fix never depended on this decision at all.
- `LEDGER.md` — append-only cycle log: gate tables, fixes landed, items
  parked for a human decision. **The authoritative copy lives in the agent
  clone** (`/workspaces/nexo-agent/_handoff/readiness/LEDGER.md`); the host
  copy is only as fresh as the last attended reconciliation.

## Ground rules baked into the roles (v2, container-first)

Everything repo-related — git and builds/tests — runs inside the dotnet-10
dev container (`docker exec elated_satoshi …`, as root). The integration
authority is the container-native agent clone `/workspaces/nexo-agent`
(origin = the bind-mounted host repo). Fixer worktrees are created from the
clone onto the bind mount so harness file tools work on them from the host
while git/dotnet run in-container. Agents never touch `master` and never push
anywhere except the machine-local staging ref
`container/claude/recursing-franklin-cbb828` on the bind mount; a human
fast-forwards the real branch from it when attended and opens any PR.
Product decisions are parked in the ledger, not guessed.

Why v2: the host session previously had to run git (linked-worktree gitdirs
don't resolve in-container) and create fixer worktrees, so unattended runs
tripped over host permission prompts. With the clone, container git works,
and the whole pipeline reduces to `docker exec` + file edits — both
pre-authorizable.

## Extending to the other layers

`readiness-gate-local.sh --layer apps` currently refuses: that layer first
needs its gate command list defined in the script (known CI owner:
`optimize-agent-cluster-gate.yml` → `apps/runtime-studio`; the other `apps/`
dirs are config surfaces with no csproj), after which the same pipeline
converges it unchanged.
