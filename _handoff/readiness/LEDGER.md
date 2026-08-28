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

## Cycle 2 — applications (2026-08-26T02:47:47Z)

Layer: `applications` · Pipeline v2, first fully container-native cycle ·
Gate in the agent clone `/workspaces/nexo-agent` at `bb332f98`.

| Gate | Status |
| --- | --- |
| applications-build | PASS |
| applications-tests-full | PASS |
| applications-provenance-unit | PASS |
| applications-dependency-boundary | PASS |

No failures — no fixers dispatched (cycle wall-clock ~2 min on the clone's
native FS vs ~4.5 min over the bind mount).

**CONVERGED: `applications` is production-ready by the pipeline's definition**
— two consecutive fully-green cycles (cycle 1's post-fix regate at `febd7686`,
cycle 2 at `bb332f98`). The loop keeps it green from here.

## apps layer bring-up (2026-08-26, attended)

Gate list defined, mirroring `optimize-agent-cluster-gate.yml` — the only CI
workflow that owns `apps/` paths: `apps-cli-build`, then five checks via
`scripts/apps-gate-checks.sh` (script interface, bootstrap, scaffold
lifecycle, daemon launch, flag matrix), env `ASHLAR_STRICT_MODE=1
ASHLAR_ALLOW_MOCK=1` as in CI. The optimizer's Ollama preflight failure is
expected and not asserted against, exactly as in CI.

**Parked (product decision, do not guess):** `apps/` holds four config
surfaces; CI covers only `runtime-studio`'s optimizer script. Question for
the human: should `ashlar-forge`, `game-director`, `release-manager` have
readiness gates at all, and what should they assert? Until answered, the
`apps` readiness gate is the runtime-studio lane only.

## Cycle 3 — apps (2026-08-26T02:53:39Z)

Layer: `apps` · Pipeline v2 · Gate in the agent clone at `e89b75fc` —
first-ever run of the apps gate list.

| Gate | Status |
| --- | --- |
| apps-cli-build | PASS |
| apps-script-interface | PASS |
| apps-bootstrap | PASS |
| apps-scaffold-optimize | PASS |
| apps-daemon-launch | PASS |
| apps-flag-combinations | PASS |

No failures — no fixers dispatched (~2.5 min). First fully-green cycle for
`apps`; convergence needs one more consecutive green. The parked product
question about gates for ashlar-forge / game-director / release-manager
stands (see "apps layer bring-up" above).

## Cycle 4 — apps (2026-08-26T02:56:37Z)

Layer: `apps` · Pipeline v2 · Gate in the agent clone at `fc463028`.

| Gate | Status |
| --- | --- |
| apps-cli-build | PASS |
| apps-script-interface | PASS |
| apps-bootstrap | PASS |
| apps-scaffold-optimize | PASS |
| apps-daemon-launch | PASS |
| apps-flag-combinations | PASS |

No failures (~11 min, cold CLI build). **CONVERGED: second consecutive
fully-green cycle for `apps`.**

## Mission status (2026-08-26): all three layers converged

- `application` — converged (two greens 2026-08-26; snapshot-test fix
  `fcbbcacd` carried on this branch).
- `applications` — converged (cycles 1–2; dependency-boundary environment
  fix `febd7686` landed by the pipeline).
- `apps` — converged (cycles 3–4; runtime-studio lane).

The convergence loop stopped itself on this condition. Remaining items for
the human:

1. **Open the PR** `claude/recursing-franklin-cbb828` → `master` (bare
   master still fails `application-tests-cli-full` until `fcbbcacd` lands).
2. **Parked product question:** should `ashlar-forge`, `game-director`,
   `release-manager` (no CI coverage, no csproj) have readiness gates, and
   what should they assert?
3. Tier-D-style lanes (`--include-tier-d`: application prod dry run;
   applications provenance Neo4j integration) need a Docker daemon and were
   not exercised locally — matching CI, where they are dispatch-only.

To keep converged layers green after future changes, run
`/converge-readiness <layer>` for one attended cycle, or restart the loop
with `/loop /converge-readiness`.

## Cycle 5 — applications (2026-08-26T14:36:41Z)

Layer: `applications` · Pipeline v2 · Gate in the agent clone at `550b6a58` —
first run with the new `applications-coverage` ratchet gate
(COVERAGE-AUDIT.md wiring, commit `eb745124`).

| Gate | Status |
| --- | --- |
| applications-build | PASS |
| applications-tests-full | PASS |
| applications-provenance-unit | PASS |
| applications-dependency-boundary | PASS |
| applications-coverage | PASS |

Green 5/5 (~80 s); floors hold at the measured baseline by construction.
Incident this window: a Docker daemon/WSL recycle killed the dev container
(exit 255, state intact on restart); loop hardened to auto-recover
(`550b6a58`).

**Ratchet raise dispatched:** Certification.Physical 80→85, XREAL 29→55
(the audit's targets). The next cycle is EXPECTED to fail
`applications-coverage` and drive the fix pipeline to write the earning
tests (XrealTrackingStateMapper 0%, validation-policy branches, hasher,
fail-closed stubs). First product-code work through fix→verify→integrate.

## Cycle 6 — applications (2026-08-26T14:39:14Z) — first product-code fix cycle

Layer: `applications` · Pipeline v2, native agent roles · Gate in the clone
at `eac117fa` (the ratchet-raise commit — this cycle was DISPATCHED to fail).

| Gate | Status |
| --- | --- |
| applications-build | PASS |
| applications-tests-full | PASS |
| applications-provenance-unit | PASS |
| applications-dependency-boundary | PASS |
| applications-coverage | FAIL → fixed |

**Fix landed:** `87486be3` (cherry-picked as `e6f5153a`) — seven behavioral
test files, 73 tests, 767 insertions, test-only, targeting exactly the
audit's named gaps: XREAL frame→PoseSample mapping driven through the public
provider seam, fail-closed session halves, pose-frame record, the validation
policy's 8 unexercised rejection codes, GeoAnchorValidator bounds/shape
rejections, hasher guards, HTTP router error responses, tag-reference
equality. Android-only NRSDK branches left unsimulated per the audit's
"partially by design" note. **Coverage: Certification.Physical 82.1→91.6%,
XREAL 31.3→86.7%.** Verifier confirmed; integrator regate **PASS 5/5**,
full suite 167/167, sync-pushed. ~19 min, 4 agents, no retry needed.

**Ratchet completed:** floors move to just under the new measurements —
Certification.Physical 85→90, XREAL 55→85 — locking the earned gains.

## Tier-D bring-up — applications (2026-08-26, attended)

Docker lever executed. Container recreated from the BRANCH devcontainer
config (docker-outside-of-docker feature, `30f51d78`); the old container is
parked stopped as `elated_satoshi_old` (safe to remove).
`readiness-container-setup.sh` reprovisioned the new container from scratch
— net8 runtime, python stdlib, git identity, clone — proving the
recreation-resilience design. First integration run FAILED in
Testcontainers' Ryuk reaper (under docker-outside-of-docker its published
port lives on the host daemon, unreachable from the container's localhost);
fixed in the gate script (`c098cc9f`: reaper disabled, host-gateway
override).

**Full gate WITH tier-D at `c098cc9f`: PASS 6/6** — build, tests-full,
provenance-unit (4s), dependency-boundary (10s), coverage (10s),
provenance-integration (15s; Neo4j 4/4). **Provenance.Graph
integration-inclusive coverage: 49.9 → 67.1% line** (56.4 branch) — the
Neo4j query/store async paths are no longer dark; the remaining gap is
mostly their error branches. The always-on coverage floor stays 48 (unit
slice, must pass without Docker); 67.1 is the tier-D-verified figure of
record.

Operational note: gate invocations that pipe output through `tail` must
echo `${PIPESTATUS[0]}` — one masked exit code was caught and corrected
this session.

## Owner decisions (2026-08-27, attended)

No convergence cycle ran, so there is no gate JSON and no gate table; the date is
the commit date of the branch `claude/readiness-audit-handoff-d57n1e`. The
authoritative copy of this ledger lives in the agent clone and has **not** been
reconciled with this entry.

Three decisions previously parked here as "do not guess" were put to the owner
and answered. They are recorded so that documents asserting them have something
to cite:

1. **May a shipped `ashlar` verb build, load and execute code from a user-named
   directory in its own process?** → **Yes, behind an opt-in flag, off by
   default.** Sets the default flag and the sandbox posture for every verb below
   it, including the not-yet-written `ashlar certify`.
2. **One operator identity or two?** → **One.** Certification records sign with
   the operator keypair, implemented by reading `~/.ashlar/keys/operator.key` in
   `Ashlar.Infrastructure` and passing it to the `ed25519PrivateKeyBase64`
   parameter `CertificationRecordSigner` already has — no new project, no
   `Infrastructure -> Manifest` edge, no new package. Reasoning, the two rejected
   options and the sequencing: `DECISION-identity-split.md`. *Scope caveat:*
   `applications/Ashlar.Certification.Physical` is a third live Ed25519 path,
   scoped out and **not yet ratified**, so "one identity" is an overclaim until
   it is.
3. **Is SPEC-006 ACCEPTED?** → **Yes, accepted 2026-08-27.** The banner is
   flipped, the acceptance-conditional language in §6 is removed, S-4 is
   de-conditioned, and S-5 (minimum accepted schema version) is added. Rules
   without a named passing test are now recorded as unmet obligations of an
   accepted spec rather than drafts.

**Still parked (unchanged):** decisions 4–11 of
`STATE-2026-08-27.md` — degrade-to-unsigned vs require-signature as the default,
the v0.1.0 package set, local LLM inference in the export bundle, behavioural
gates for `ashlar-forge` / `game-director` / `release-manager`, the Unity and
`ext-*` commands, commercial terms, whether this repo keeps a numbered-spec
system, and who owns the `Ashlar.*` prefix on nuget.org.

**Newly parked:** should `.github/workflows/security-gate.yml` cover
`Certification/**`? It does not today, which leaves the trust surface outside the
security gate's paths. Changing which gate guards it is a CI/product decision, not
an agent's.
