---
description: Run one readiness-convergence cycle for a repo layer and update the ledger. Loop it with "/loop /converge-readiness" for autonomous convergence.
---

You are the orchestrator of the readiness convergence loop. The goal: drive the
target layer's readiness gate (scripts/readiness-gate-local.sh) to green and
keep it green, using the hierarchical agent pipeline defined in this repo.

Target layer: `$ARGUMENTS` if given, otherwise `application`.

## One cycle

1. Read `_handoff/readiness/LEDGER.md`. If the last two cycle entries are both
   fully green, report convergence and stop (if running under /loop dynamic
   mode, end the loop via ScheduleWakeup stop). Collect the gate names the
   ledger has parked for the human ("same gate failed 3 consecutive cycles" or
   product-decision items) — they are passed as exclusions in step 2, never
   redispatched.
2. Run one convergence cycle:
   `Workflow({scriptPath: '<repo>/.claude/workflows/readiness-convergence.js', args: {layer: '<layer>', excludeGates: [<parked gate names>]}})`
   with an absolute scriptPath into THIS checkout. (`{name: 'readiness-convergence'}`
   may also resolve, but the name registry does not always see worktree files —
   scriptPath is the reliable form.) Wait for it to complete; do not start a
   second cycle concurrently.
3. Append a cycle entry to `_handoff/readiness/LEDGER.md` from the workflow's
   result object: the `started_at` it carries (never invent timestamps), the
   `commit` the gate ran at, the gate table (name/status), `fixes` landed
   (gate, sha, root cause), `parked` items with reasons, and the
   `excluded`/`deferred`/`unresolved`/`dropped` gate lists. When `integration`
   is present, also record its `gate_after` regate status, `final_sha`, and
   any `picks_dropped` with reasons.
4. Report the cycle outcome to the user in one short paragraph.

## Standing rules

- Run from inside the integration checkout, with the pipeline files
  (gate script, agents, workflow, command) COMMITTED on the integration
  branch — fixer worktrees branch from the session repo's HEAD, and an
  uncommitted baseline means fixers reproduce against a different tree than
  the gate measured. If anything relevant is uncommitted, commit it on the
  integration branch first and note that in the ledger.
- Never push, never touch `master`. Integration stays on the local `claude/…`
  branch until the human opens a PR.
- An `infrastructure_failure` from the workflow (the gate script itself broke)
  is fixed by you directly in the integration checkout — it is orchestration
  tooling, not product code — and noted in the ledger.
- A gate that fails 3 consecutive cycles without progress gets parked in the
  ledger for the human; convergence of the others continues.
- Escalate, don't guess: anything that smells like a product decision (public
  API shape, behavior changes visible to users, dependency major bumps) is
  parked with a precise question, not decided by an agent.
