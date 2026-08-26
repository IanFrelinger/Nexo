# Handoff: readiness convergence — orchestrator session

You are taking over as the orchestrator of an autonomous, hierarchical agent
pipeline whose mission is to get this repo's app-facing layers production
ready, layer by layer: `application/` first (done converging — keep it green),
then `applications/`, then `apps/`. Written 2026-08-26 by the session that
built the pipeline. Everything below is committed on branch
`claude/recursing-franklin-cbb828`.

## Start here

1. Work from this checkout:
   `C:\Users\icfre\Downloads\Nexo\.claude\worktrees\recursing-franklin-cbb828`
   (a git worktree; expected branch `claude/recursing-franklin-cbb828`).
2. Read `_handoff/readiness/LEDGER.md` (cycle history, parked items) and
   `_handoff/readiness/README.md` (architecture, one page).
3. Run `/converge-readiness` for one cycle, or `/loop /converge-readiness`
   for autonomous convergence. The command file
   (`.claude/commands/converge-readiness.md`) is the full orchestrator
   contract — cycle steps, ledger format, stop conditions, standing rules.

## Environment facts (authoritative — violating these wastes hours)

- **Builds and tests count only inside the dev container** `elated_satoshi`
  (dotnet SDK 10.0.400, image `devcontainers/dotnet:10.0-noble`, 12 CPUs):
  `docker exec elated_satoshi bash -lc "cd <container-path> && <cmd>"`.
  Host path `C:\Users\icfre\Downloads\Nexo` ⇔ container `/workspaces/Nexo`;
  worktrees under `.claude/worktrees/<name>` map the same way.
- **Git runs only on the host.** A linked worktree's gitdir points at a
  Windows path; inside the container `git` fails with "not a git repository".
- Host-vs-container toolchain drift is real: a PublicApiGenerator snapshot
  test failed only under the container toolchain (fixed on this branch by
  normalizing in the test — commit "toolchain-stable comparison").

## The pipeline (all committed on this branch)

- `scripts/readiness-gate-local.sh --layer application --json <out>` — the
  objective function. Mirrors CI's application-gate (tiers A+B+C, CI env
  parity, tier D behind `--include-tier-d`) plus the full Ashlar.Tests.CLI
  suite. Exit 0 + JSON verdict. ~4.5 min cold, ~4 min warm.
- `.claude/agents/readiness-{gate-runner,fixer,verifier,integrator}.md` —
  the four roles. Your fresh session registers them natively.
- `.claude/workflows/readiness-convergence.js` — one cycle:
  Gate → Fix (isolated worktrees) → Verify (adversarial, refute-by-default,
  one guided retry) → Integrate (cherry-pick + regate). Invoke by ABSOLUTE
  scriptPath (the name registry may not see worktree files):
  `Workflow({scriptPath: '<this-checkout>/.claude/workflows/readiness-convergence.js', args: {layer: 'application', excludeGates: [...]}})`.
  It has a built-in fallback if the custom agent types ever fail to resolve.
- `_handoff/readiness/LEDGER.md` — append-only cycle log. Update it every
  cycle, exactly per the command file's step 3.

## State at handoff

- `application` layer: **gate green 4/4, twice** (cold and warm runs,
  2026-08-26) on this branch. The two-consecutive-green convergence condition
  is satisfied for this checkout; the remaining work for this layer is
  keeping it green and getting the branch PR'd into master (bare master
  FAILS the full-suite gate — it lacks the snapshot-test fix carried here).
- `applications/` and `apps/` layers: **not started.** The gate script
  refuses them by design. Next concrete task: recon which CI workflows own
  those layers (same method as before — read `.github/workflows/*gate*.yml`,
  extract exact commands), add their gate lists to
  `scripts/readiness-gate-local.sh`, then converge with the same pipeline.
- Nothing is pushed. No PR exists. Opening one is the human's call.

## Standing rules (non-negotiable)

- Never push; never touch `master`; integration accumulates on this branch.
- Product decisions (public API shape, user-visible behavior, dependency
  majors) get parked in the ledger with a precise question — never guessed.
- A gate failing 3 consecutive cycles without progress is parked; pass parked
  gate names via `excludeGates` so they are not redispatched.
- Report cycle outcomes to the user briefly; the ledger carries the detail.
