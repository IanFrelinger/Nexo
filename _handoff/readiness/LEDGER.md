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
