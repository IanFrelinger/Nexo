# Handoff: readiness convergence — orchestrator session

You are taking over as the orchestrator of an autonomous, hierarchical agent
pipeline whose mission is to get this repo's app-facing layers production
ready, layer by layer: `application/` (converged — keep it green), then
`applications/`, then `apps/`. Pipeline v1 written 2026-08-26; migrated to the
container-first v2 architecture the same day so the loop can run overnight
unattended. Everything below is committed on branch
`claude/recursing-franklin-cbb828`.

## Start here

1. Start the session in this checkout:
   `C:\Users\icfre\Downloads\Nexo\.claude\worktrees\recursing-franklin-cbb828`
   (a git worktree; expected branch `claude/recursing-franklin-cbb828`).
2. Ensure the container side is provisioned (idempotent, safe to re-run):
   `docker exec elated_satoshi bash -lc "bash /workspaces/Nexo/.claude/worktrees/recursing-franklin-cbb828/scripts/readiness-container-setup.sh"`
   (always the `bash -lc "<cmd>"` form — Git Bash on the host mangles bare
   `/workspaces/...` arguments into `C:/Program Files/Git/...` paths).
3. Read the CLONE's ledger (`docker exec elated_satoshi cat /workspaces/nexo-agent/_handoff/readiness/LEDGER.md`)
   and `_handoff/readiness/README.md` (architecture, one page).
4. Run `/converge-readiness` for one cycle, or `/loop /converge-readiness`
   for autonomous overnight convergence. The command file
   (`.claude/commands/converge-readiness.md`) is the full orchestrator
   contract — cycle steps, ledger format, stop conditions, standing rules.

## Environment facts (v2 — authoritative; violating these wastes hours)

- **Everything repo-related runs inside the dev container** `elated_satoshi`
  (dotnet SDK 10.0.400 + .NET 8 runtime, image `devcontainers/dotnet:10.0-noble`,
  12 CPUs, docker exec enters as root):
  `docker exec elated_satoshi bash -lc "<cmd>"`. That includes **git**.
- The **agent clone** `/workspaces/nexo-agent` (container-native FS, branch
  `claude/recursing-franklin-cbb828`, origin = bind-mounted `/workspaces/Nexo`)
  is the integration authority. The gate runs there; fixes cherry-pick there;
  the ledger lives there. Container git works in the clone (the old
  "git only on the host" rule was a linked-worktree artifact and applies only
  to the HOST repo's worktrees).
- **Fixer worktrees** are created from the clone ONTO the bind mount
  (`/workspaces/Nexo/.claude/worktrees/agent-*` ⇔
  `C:\Users\icfre\Downloads\Nexo\.claude\worktrees\agent-*`): file edits use
  harness tools on the host path; all git/dotnet uses docker exec on the
  container path. Host git cannot see these worktrees at all.
- **Sync-back:** the clone pushes `HEAD:refs/heads/container/claude/recursing-franklin-cbb828`
  to origin (the bind mount) — the only push in the system; nothing ever
  leaves the machine. The human (or an attended orchestrator) fast-forwards
  the real branch from that staging ref:
  `git -C <host checkout> merge --ff-only container/claude/recursing-franklin-cbb828`.
- Host-vs-container toolchain drift is real: a PublicApiGenerator snapshot
  test failed only under the container toolchain (fixed on this branch,
  commit "toolchain-stable comparison"). Container results are the only ones
  that count.
- **Tier-D lanes are runnable (2026-08-26):** the container is created from
  the BRANCH devcontainer config (docker-outside-of-docker feature → host
  docker socket inside). Recreate with
  `devcontainer up --workspace-folder <repo> --config <this-checkout>/.devcontainer/devcontainer.json --remove-existing-container`,
  rename to `elated_satoshi`, run the setup script. Testcontainers under
  DooD needs `TESTCONTAINERS_RYUK_DISABLED=true` and
  `TESTCONTAINERS_HOST_OVERRIDE=host.docker.internal` — already baked into
  the gate script's tier-D command. Run with `--include-tier-d`.

## The pipeline (all committed on this branch)

- `scripts/readiness-gate-local.sh --layer application|applications --json <out>`
  — the objective function per layer (CI-mirroring gate lists + strictly
  stronger local suites). `apps` still refuses: define its gate list after
  `applications` converges (known CI owner: optimize-agent-cluster-gate.yml).
- `scripts/readiness-container-setup.sh` — idempotent container provisioning
  (net8 runtime, git identity/safe.directory, the agent clone).
- `.claude/agents/readiness-{gate-runner,fixer,verifier,integrator}.md` — the
  four roles, container-first.
- `.claude/workflows/readiness-convergence.js` — one cycle:
  Gate (in clone) → Fix (clone worktrees on the mount) → Verify (adversarial,
  refute-by-default, one guided retry) → Integrate (cherry-pick in clone +
  regate + staging-ref sync-push). Invoke by ABSOLUTE scriptPath:
  `Workflow({scriptPath: '<this-checkout>/.claude/workflows/readiness-convergence.js', args: {layer: '<layer>', excludeGates: [...]}})`.
- `_handoff/readiness/LEDGER.md` — append-only cycle log **in the clone**
  (the host copy is stale between attended reconciliations).
- `.claude/settings.local.json` (untracked, machine-local) — pre-authorizes
  docker exec, Workflow, and Nexo-tree file edits so overnight runs never
  prompt.

## Standing rules (non-negotiable)

- Never push to GitHub or any real remote; never touch `master`; integration
  accumulates in the clone and reaches the host repo only via the staging ref.
- Product decisions (public API shape, user-visible behavior, dependency
  majors) get parked in the ledger with a precise question — never guessed.
- A gate failing 3 consecutive cycles without progress is parked; pass parked
  gate names via `excludeGates` so they are not redispatched.
- Report cycle outcomes to the user briefly; the ledger carries the detail.
- Opening the PR into master is the human's call.
