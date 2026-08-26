---
description: Run one readiness-convergence cycle for a repo layer and update the ledger. Loop it with "/loop /converge-readiness" for autonomous overnight convergence.
---

You are the orchestrator of the readiness convergence loop (container-first,
v2). The goal: drive each layer's readiness gate
(scripts/readiness-gate-local.sh) to green and keep it green, in layer order
`application` → `applications` → `apps`.

Target layer: `$ARGUMENTS` if given, otherwise the first layer in that order
whose convergence (two consecutive fully-green cycles) is not yet recorded in
the ledger.

## Environment (v2 — container-first)

- All git and all builds/tests run inside the dev container `elated_satoshi`
  via `docker exec elated_satoshi bash -lc "<command>"` (enters as root).
  The host session runs ONLY: `docker exec`, the Workflow/Agent tools, and
  Read/Edit/Write on fixer-worktree host paths. Never host git.
- The **agent clone** `/workspaces/nexo-agent` (branch
  `claude/recursing-franklin-cbb828`, origin = the bind-mounted host repo
  `/workspaces/Nexo`) is the integration authority. **The ledger that counts
  is the clone's** `_handoff/readiness/LEDGER.md`.
- The host repo receives commits only via the staging ref
  `container/claude/recursing-franklin-cbb828` (sync-push from the clone).
  Reconciliation — fast-forwarding the real branch from the staging ref — is
  an ATTENDED step, never done overnight.
- Fresh container or missing clone? Run
  `docker exec elated_satoshi bash -lc "bash /workspaces/Nexo/.claude/worktrees/recursing-franklin-cbb828/scripts/readiness-container-setup.sh"`
  first (idempotent: net8 runtime, git identity, clone). Always use the
  `bash -lc "<cmd>"` form for docker exec — Git Bash mangles bare
  `/workspaces/...` arguments into `C:/Program Files/Git/...` paths.

## One cycle

1. Read the clone's ledger:
   `docker exec elated_satoshi cat /workspaces/nexo-agent/_handoff/readiness/LEDGER.md`.
   If the last two cycle entries for the target layer are both fully green,
   record/report convergence and advance to the next layer in order; if all
   layers are converged, stop (under /loop dynamic mode, end via ScheduleWakeup
   stop). Collect the gate names the ledger has parked ("failed 3 consecutive
   cycles" or product-decision items) — pass them as exclusions in step 2,
   never redispatched.
2. Run one convergence cycle:
   `Workflow({scriptPath: '<host checkout>/.claude/workflows/readiness-convergence.js', args: {layer: '<layer>', excludeGates: [<parked gate names>]}})`
   with an absolute scriptPath into THIS checkout. Wait for it to complete;
   never start a second cycle concurrently.
3. Append a cycle entry to the CLONE's ledger from the workflow's result
   object — the `started_at` it carries (never invent timestamps), the
   `commit` the gate ran at, the gate table (name/status), `fixes` landed
   (gate, sha, root cause), `parked` items with reasons, the
   `excluded`/`deferred`/`unresolved`/`dropped` gate lists, and when
   `integration` is present its `gate_after`, `final_sha`, `sync_pushed`, and
   any `picks_dropped` with reasons. Write it via a docker-exec heredoc, then
   commit in the clone and ride the staging ref:
   `docker exec elated_satoshi bash -lc "cd /workspaces/nexo-agent && git add _handoff/readiness/LEDGER.md && git commit -m 'Ledger: <layer> cycle <started_at>' && git push origin HEAD:refs/heads/container/claude/recursing-franklin-cbb828"`
4. **Cross-layer guard:** if this cycle landed fixes (`integration` present),
   rerun each PREVIOUSLY-converged layer's gate once in the clone. A
   regression there is recorded in the ledger and that layer becomes the
   target next cycle.
5. Report the cycle outcome to the user in one short paragraph.

## Under /loop (overnight autonomy)

- One cycle per wake. If the previous cycle's Workflow is still running in the
  background, do nothing and reschedule (noop, 20–30 min).
- Stop conditions: all three layers converged (report and stop); or 3
  consecutive wakes ending in infrastructure failure (stop with a loud
  report). Otherwise keep cycling.
- Everything you need overnight is `docker exec` + Workflow + fixer-worktree
  file edits — pre-authorized in settings. If an action would need anything
  else on the host (host git, new directories, network), don't do it; park a
  note in the ledger instead.

## Standing rules

- Pipeline files (gate script, workflow, roles, this command) must be
  COMMITTED in the clone before dispatching fixers — fixer worktrees branch
  from the clone's HEAD. If you changed them, commit in the clone first and
  note it in the ledger.
- Never push to GitHub or any real remote; never touch `master`. The ONLY
  push in the system is the staging-ref push to origin (the bind mount), by
  the integrator or by you for ledger commits.
- An `infrastructure_failure` from the workflow (the gate script itself broke)
  is fixed by you directly in the clone — orchestration tooling, not product
  code — and noted in the ledger.
- A gate that fails 3 consecutive cycles without progress gets parked in the
  ledger for the human; convergence of the others continues.
- Escalate, don't guess: anything that smells like a product decision (public
  API shape, behavior visible to users, dependency majors) is parked with a
  precise question, not decided by an agent.
- `apps` has no gate list yet. Before converging it: recon which CI workflows
  own `apps/` (known so far: optimize-agent-cluster-gate.yml →
  apps/runtime-studio; the other apps/ dirs are config surfaces with no
  csproj), define the gate list in scripts/readiness-gate-local.sh, commit in
  the clone, then converge. If ownership is genuinely ambiguous, park the
  question instead of guessing.
