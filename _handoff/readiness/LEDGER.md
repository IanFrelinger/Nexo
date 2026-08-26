# Readiness convergence ledger

Append-only cycle log. Timestamps come from the gate JSON `started_at`; never
invented. One entry per cycle, newest last.

## Cycle 0 — baseline (2026-08-26T01:58:20Z)

Layer: `application` · Checkout: worktree `recursing-franklin-cbb828`, branch
`claude/recursing-franklin-cbb828` at `f9e1f4a4` **plus uncommitted changes**
(the BrickAuthoringPublicApiSnapshotTests toolchain-normalization fix and this
scaffolding — not yet committed at the human's discretion).

| Gate | Status | Duration |
| --- | --- | --- |
| application-tier-a | PASS | 52s |
| application-tier-b | PASS | 103s |
| application-tier-c | PASS | 53s |
| application-tests-cli-full | PASS | 57s |

**Verdict: PASS (4/4).** No fixers dispatched — nothing to fix.

Notes:
- `application-tests-cli-full` passes only because this checkout carries the
  uncommitted snapshot-test fix; on bare `f9e1f4a4` (master tip) it fails on
  `BrickAuthoringPublicApiSnapshotTests` under the container toolchain. The
  fix must land before the layer counts as green on master.
- Tier D (agent-server prod dry run) not exercised — needs a Docker daemon,
  matching CI's own full-lane skip. Run `--include-tier-d` from a host with
  Docker for that coverage.

Verification re-run (2026-08-26T02:08:10Z): PASS 4/4 again (warm, ~3.8 min;
tier-a 51s, tier-b 99s, tier-c 26s, tests-cli-full 54s), exercising the
post-review script fixes (guarded JSON write, caller-relative --json paths).
Same checkout state as Cycle 0 — the two-consecutive-green condition is met
for this checkout, pending the baseline commit landing.

Update 2026-08-26: baseline committed — `fcbbcacd` (snapshot fix),
`a61af4ad` (pipeline). Fixer worktrees now share the measured baseline.

Open items for the human:
- Open the PR for `claude/recursing-franklin-cbb828` → master (bare master
  fails application-tests-cli-full until the snapshot fix lands).
- Decide gate lists for `--layer applications` and `--layer apps`
  (which CI workflows own those layers).

## Cycle 1 — applications (2026-08-26T02:30:36Z)

Layer: `applications` · Pipeline v1 (host-git; ran mid-migration to v2) ·
Gate at `95710c8c` in the host worktree `recursing-franklin-cbb828`.

| Gate | Status |
| --- | --- |
| applications-build | PASS |
| applications-tests-full | PASS |
| applications-provenance-unit | PASS |
| applications-dependency-boundary | FAIL → fixed |

**Fix landed:** `d34aefd0` (cherry-picked as `febd7686`) —
applications-dependency-boundary. Root cause: environment, not repo code —
the dev container had only python3-minimal (no `dataclasses` in the stdlib),
so `verify-open-commercial-dependency-boundary.py` crashed at import. Fixer
installed `libpython3.12-stdlib` live in the container AND committed an
idempotent provisioning step to `.devcontainer/post-create.sh`. Verifier
confirmed; integrator regate: **PASS 4/4** (build 32s, tests-full 2s,
provenance-unit 5s, dependency-boundary 34s), final sha `febd7686`.
No parked/deferred/unresolved/dropped gates. Tier D analog
(applications-provenance-integration, Neo4j via Testcontainers) not
exercised — needs a Docker daemon, mirroring CI's dispatch-only lane.

**Convergence:** first fully-green outcome for `applications`; needs one
more consecutive green cycle (which will run container-native in the agent
clone, pipeline v2).

**Pipeline v2 migration (2026-08-26, commits `53352f02`, `241a15e6`):**
container-first — all git and builds in the dev container; agent clone
`/workspaces/nexo-agent` is the integration authority; THIS clone copy of
the ledger is now the authoritative one; the host repo receives commits via
staging ref `container/claude/recursing-franklin-cbb828` only. The .NET 8
runtime the layer's net8.0 test projects need was installed in the container
(and `scripts/readiness-container-setup.sh` reprovisions it).
