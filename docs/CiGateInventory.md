# CI gate inventory

This file describes what CI **actually does** on this repository: which workflow files exist, what triggers each one, and which checks branch protection **really** requires. Workflow YAML controls when checks run; GitHub branch protection (a repository setting, not YAML) controls which check names must be green before merge. Where the two disagree, this file follows the settings and says so.

Snapshot: **59 workflow files** under `.github/workflows/` (`git ls-files ".github/workflows/*.yml"`), verified 2026-09-05. Includes `products-gate.yml`, `ingress-unit-gate.yml`, and the weekly/manual `autonomous-release-manager.yml`.

## Required checks (branch protection) — what is enforced today

`master` branch protection requires **exactly one** status-check context:

| Context | Workflow | Runs on |
| --- | --- | --- |
| `cert-gate` | `.github/workflows/cert-gate.yml` (job `cert-gate`) | every `pull_request`, every push to `master`, `workflow_dispatch` — no path filter |

Verified with `gh api repos/IanFrelinger/Nexo/branches/master/protection` (`required_status_checks.contexts == ["cert-gate"]`, `strict: true`, `enforce_admins: true`). Everything else in this document is **advisory**: a red `layer-boundary / verify`, `Kernel Gate / kernel-gate`, or `Docs Link Check / lychee` does not block a merge. Earlier revisions of this file listed 15 required contexts; that was never the repository setting.

### Why the other gates are not required (and cannot simply be added)

Almost every other PR-triggered gate uses `paths:` filters. GitHub only reports a status for a path-filtered workflow when the filter matches; a PR that does not touch those paths gets **no** status at all, and a required context that never reports blocks the merge forever. Promoting a path-filtered gate to "required" therefore needs one of:

1. an **always-report job** — a lightweight job in the same workflow with no path filter that emits success when the filtered job was skipped (GitHub's documented "handling skipped but required checks" pattern), or
2. dropping the path filter and paying the run cost on every PR (this is what `cert-gate` does), or
3. moving the path filter inside the job (`dorny/paths-filter` or a `git diff` step) so the workflow always runs and always reports.

Until one of those lands per gate, only unfiltered checks are safe to require. `layer-boundary` uses `paths: ["**"]` — effectively unfiltered — and is the one other gate that could be required today without an always-report job; see [`CONTRIBUTING.md`](../CONTRIBUTING.md) ("Layer boundary and what master actually enforces") for why it is not yet.

### Branch protection update snippet

Human runs this; agents cannot change repository settings. The `contexts` array below is the **current** setting. Add contexts only when the workflow behind them always reports on PRs (see above).

```bash
OWNER="IanFrelinger"
REPO="Ashlar"
BRANCH="master"

cat > /tmp/ashlar-required-checks.json <<'JSON'
{
  "required_status_checks": {
    "strict": true,
    "contexts": [
      "cert-gate"
    ]
  }
}
JSON

gh api --method PATCH -H "Accept: application/vnd.github+json" -H "X-GitHub-Api-Version: 2022-11-28" "/repos/$OWNER/$REPO/branches/$BRANCH/protection" --input /tmp/ashlar-required-checks.json
```

## Trigger map

Counts by trigger class (59 files):

| Class | Count | Meaning |
| --- | --- | --- |
| Runs on `pull_request` | 32 | 4 unfiltered (`cert-gate`, `layer-boundary`, `uat-gate`, `composition-mesh-gate`), 27 path-filtered (including `products-gate`, Release Manager validation, compat/dr/perf/production-readiness/ship-gate, `ingress-unit-gate`, `onboarding-docs-guard`, `pack-hosting-graph-alignment`, `onboarding-quickstart-gate`, `environment-setup-gate-v1`, `optimize-agent-cluster-gate`, `runtime-release-gate`, `installer-bruteforce-gate`, `rc-gate`), 1 label-driven (`release-staging-on-label`) |
| Push- and/or schedule-driven, plus `workflow_dispatch` | 8 | Post-merge / scheduled signal; never blocks a PR. |
| `workflow_dispatch` only | 14 | Manual lanes (mesh labs, multi-env Docker suites, ops/perf, release plumbing) |
| Tag / release event | 2 | `release.yml` (`v*.*.*` tags), `devlog-ghost-release.yml` (`release: published`) |
| Reusable (`workflow_call`) | 3 | `reusable-*` |

Six workflows carry a `schedule`: `autonomous-release-manager` (Mon 05:00 UTC), `distribution-matrix-gate` (Mon 10:00 UTC), `full-platform-readiness-gate` (Mon 06:00), `onboarding-quickstart-gate` (Mon 07:00), `rc-gate` (06:00 on the 1st of each month), `mesh-lab-tls-gate` (Tue 07:00). (`mesh-lab-stress-gate` lost its schedule 2026-08-16 and is dispatch-only.)

### PR-triggered workflows

| Workflow file | Name / job(s) | PR trigger | Also |
| --- | --- | --- | --- |
| `cert-gate.yml` | Cert gate / `cert-gate` | **every PR** (no paths) — analyzer 56 + contracts 18 + enrolled conventions 69 counted; main Infra filter excludes convention tests and uses list-tests plus collapse floor **447** | push `master`, dispatch — **required** |
| `layer-boundary.yml` | layer-boundary / `verify` | every PR (`paths: "**"`, types opened/synchronize/reopened/edited) | — |
| `application-gate.yml` | Application Gate / `application-gate` | paths: `application/**`, VirtualProduction tests, `scripts/application-gate*.sh`, `scripts/prod-dry-run.sh`, `Makefile`, … | dispatch |
| `dependency-boundary.yml` | dependency-boundary / `verify` | paths: `**/*.csproj`, `commercial/**`, `application/**`, `src/**`, `LICENSING.md`, boundary scripts | push, dispatch |
| `distribution-matrix-gate.yml` | Distribution Matrix Gate / 7 jobs | paths: same broad list as push (Dockerfiles, pack/verify scripts, Ashlar.API/CLI, Client/Sdk/Hosting.Bundle/Authoring/Brick.Contracts, samples, VirtualProduction tests) — counted IAshlarClient floor 1 | push (broad paths), weekly schedule, dispatch |
| `docs-link-check.yml` | Docs Link Check / `lychee (README + docs)` | paths: `docs/**`, `README.md`, `.lycheeignore` | push, dispatch |
| `kernel-coverage-gate.yml` | Kernel coverage gate / `kernel-coverage` | paths: kernel src + tests, `scripts/ci/kernel-coverage-gate.sh`, `scripts/ci/pr-testing-strategy-gate.sh` | push |
| `kernel-gate.yml` | Kernel Gate / `kernel-gate` | paths: `src/Ashlar.Hosting/**`, Infrastructure, Orchestration, Runtime, Core.Application, kernel tests, `docs/production-readiness/**`, `Makefile` | PR: Tiers A–C; D–E / full dispatch-only; push (narrower paths) |
| `mcp-a2a-gate.yml` | MCP + A2A protocol gate / `scripts/mcp-a2a-gate.sh` | paths: `src/Ashlar.Mcp.*`, `src/Ashlar.Transport.A2A*`, `Ashlar.API`, `scripts/mcp-a2a-gate.sh` | push `master`/`main`/`cursor/**`, dispatch — counted floors 40 / 33 / 39 / 19 / 7 |
| `security-gate.yml` | Security Gate / `security-gate` | paths: Trust/Security sources and tests, `scripts/security-gate*.sh`, `Makefile` | PR: Tiers A–C plus E host suite (52); D and E container dispatch-only (release-manager `security` lane runs full A–E with Docker) |
| `shell-lint.yml` | Shell lint / `shell-lint` | paths: `scripts/**` | dispatch |
| `testing-strategy-gate.yml` | Testing strategy gate / `testing-strategy` | paths: `src/**`, `application/**`, `scripts/**`, `.github/**`, `Makefile`, `docs/architecture/TestingStrategy*.md` | — |
| `release-staging-on-label.yml` | Release staging on label / `dispatch-staging-release` | `types: [labeled]` only | — |
| `uat-gate.yml` | UAT / `uat`, `uat cross-platform` | **every PR** (no paths — deliberate, see file header) | push `master`, dispatch |
| `composition-mesh-gate.yml` | Composition Mesh Gate / `composition-mesh-gate` | **every PR** (no paths) — Tier A–C via `make composition-mesh-gate` | dispatch chooses a single tier |
| `compat-gate.yml` | compat-gate | paths: Fleet checkpoint tests, pipeline/composition/kernel-phase tests, compat scripts — counted Fleet migrate 1 + LiteDB persist 1 + composition 4; Tier C configuration 2 + kernel-phase 4 | push `master`, dispatch |
| `dr-gate.yml` | dr-gate | paths: LiteDB user-knowledge store + DR scripts — counted knowledge-store floor 8; Tier C mesh-lab restart or counted host LiteDB (`LiteDbMeshDirectorPersistenceTests` floor 2) | push `master`, dispatch |
| `perf-gate.yml` | perf-gate | paths: Orchestration/BackgroundAgents tests + perf scripts — PR runs counted Tier A only (3 + 9); B–D + baseline stay push/dispatch | push `master`, dispatch |
| `products-gate.yml` | products-gate / `product scaffolds` | paths: `products/**`, distributed contracts, deployment-profile sources, `ci/test-ownership.tsv` | push `master`/`main`/`cursor/**`, dispatch — **advisory**; runs `products/Ashlar.Products.sln` plus `DistributedContractTests`. Does **not** run the dependency-boundary script (that is `dependency-boundary.yml`). |
| `autonomous-release-manager.yml` | Autonomous Release Manager / `Validate release manager` | paths: coordinator, plan, tests, workflow | weekly schedule + dispatch run the full six-lane audit; PRs run only unit tests and immutable-plan validation |
| `portability-gate.yml` | Portability Gate | paths: `application/src/Ashlar.CLI/**`, `src/Ashlar.Manifest/**`, `scripts/e2e-loop.sh` — 3-OS loop plus e2e-loop collapse floor 143 (Linux) / 137 (otherwise) | dispatch |
| `production-readiness-gate-v1.yml` | Production Readiness Gate v1 / `scripts/production-readiness-gate-v1-tests.sh` | paths: pipelines sources/tests, CLI, readiness docs — counted Pipelines 68 (net8 + net10) + host-DI 2 | push `master`/`main`/`cursor/**`, dispatch |
| `ship-gate.yml` | Ship Gate / `ship-gate` | paths: BaseFramework smoke tests, `scripts/ship-gate-tier-b.sh`, `scripts/ship-gate-tier-d.sh` — PR runs counted Tier B smoke (9) + ProdStyle and always runs `ci release-bundle`; A/C stay dispatch-only | dispatch |
| `ingress-unit-gate.yml` | ingress-unit-gate / `AwsSns + DynamoDb counted units` | paths: ingress sources/tests, `scripts/ingress-unit-gate.sh`, counted wrapper — counted AwsSns 11 + DynamoDb 2 | push `master`/`main`/`cursor/**`, dispatch |
| `onboarding-docs-guard.yml` | Onboarding Docs Guard / `guard` | paths: README, `docs/**/*.md`, `scripts/*.{sh,ps1}`, `Makefile`, `**/*.csproj` — startup-doc greps, referenced-path existence, ProjectTiers census | push `master`/`main`/`cursor/**`, dispatch |
| `pack-hosting-graph-alignment.yml` | Pack hosting graph alignment / `verify` | paths: `src/**/*.csproj`, pack scripts, `Directory.Build.props` — pack list vs `Ashlar.Hosting` MSBuild graph (17 packed) | push `master`/`main`/`cursor/**`, dispatch |
| `onboarding-quickstart-gate.yml` | onboarding-quickstart-gate / `quickstart-native-linux` | paths: README, GettingStarted, setup/install, CLI — PR runs native check/restore/help/doctor; container GHCR pull stays schedule/push/dispatch | weekly schedule, push `master`/`main`/`cursor/**`, dispatch |
| `environment-setup-gate-v1.yml` | Environment Setup Gate v1 / `setup-gate` 3-OS | paths: `scripts/setup/**`, CLI — PR runs native check/restore/build on ubuntu/macOS/Windows; MCR SDK container pull stays push/dispatch | push `master`/`main`, dispatch |
| `optimize-agent-cluster-gate.yml` | Optimize Agent Cluster Gate | paths: `apps/runtime-studio/**`, `scripts/sandbox/**`, CLI — PR runs script-interface, bootstrap, scaffold/optimize, daemon, and flag-combo jobs | push `master`/`main`/`cursor/**`, dispatch |
| `runtime-release-gate.yml` | Runtime Release Gate / `runtime-release-lanes` | paths: CLI runtime/release commands, `docs/runtime/benchmarks/**` — PR runs gating core + visual (`--allow-mock`); chaos stays `continue-on-error` | push `master`/`main`, dispatch |
| `installer-bruteforce-gate.yml` | Installer Bruteforce Gate / `bruteforce-matrix` | paths: `scripts/setup/**`, `scripts/install/**`, CLI — host bash syntax/help/fail cases plus CLI restore/build (12 cases; container bootstrap is `--dry-run`) | push `master`/`main`/`cursor/**`, dispatch |
| `rc-gate.yml` | RC Gate / `rc-gate` | paths: RC docs + `scripts/rc-gate*.sh` — PR/push/schedule produce `ci release-bundle` then fail-close Tier C; A/B/D stay dispatch-only (D needs authenticated `gh`) | monthly schedule, push `master`/`main`, dispatch |

### Push-only (path-filtered) workflows

All of these also accept `workflow_dispatch`. Branch filters are `master`, `main`, `cursor/**` unless noted.

| Workflow file | Name | Notes |
| --- | --- | --- |
| `compose-gate.yml` | Compose Gate | compose test stacks, `.docker/Dockerfile.test-caching*`, CLI, README |
| `container-image-gate.yml` | Container Image Gate | `.docker/Dockerfile.cli`, CLI + spine sources |
| `container-image-publish.yml` | Container Image Publish | **dispatch-only** GHCR `:latest`; versioned tags use `release.yml` + READY |
| `devcontainer-gate.yml` | Dev Container Gate | `.devcontainer/**`, `Ashlar.LocalDevCore.slnf`, CLI |
| `friend-mesh-prefab-gate.yml` | Friend mesh prefab gate | friend-mesh compose, `.docker/Dockerfile.api`, `Ashlar.API` |
| `full-platform-readiness-gate.yml` | Full Platform Readiness Gate | Dockerfiles, setup/install scripts, spine sources, StableSdkHostSample; **weekly schedule** |
| `grpc-transport-gate.yml` | gRPC transport gate / `scripts/grpc-transport-gate.sh` | `src/Ashlar.Transport.Grpc/**`, `src/Ashlar.Tests.Transport/**` — PR + push; counted ProdStyle floor 81 |

### Manual-only workflows (`workflow_dispatch`)

`composition-mesh-gate`, `container-image-publish`, `cross-platform-tests`, `mesh-lab-gate`, `mesh-lab-stress-gate`, `nuget-consumer-verify`, `ops-gate`, `perf-certification`, `prod-dry-run-pr`, `release-nuget`, `runtime-release-promotion`, `setup-smoke-suite`, `test-air-gapped-no-network`, `test-trust-multi-env`, `waterproofing-gate`, `workflow-regression-gate` (16).

Despite their names, **`cross-platform-tests`** and **`prod-dry-run-pr`** do not run on PRs; run them with `gh workflow run "<name>" --ref <branch>`.

### Scheduled-only, tag/release, reusable

| Workflow file | Trigger |
| --- | --- |
| `autonomous-release-manager.yml` | path-filtered PR validation + `schedule` Mon 05:00 UTC + dispatch; six isolated, mandatory audit lanes run only on schedule/dispatch; report uploaded on READY or BLOCKED |
| `mesh-lab-tls-gate.yml` | `schedule` Tue 07:00 UTC + dispatch |
| `release.yml` | push tags `v*.*.*` + dispatch |
| `devlog-ghost-release.yml` | `release: published` + dispatch |
| `reusable-container-publish.yml`, `reusable-release-nuget.yml`, `reusable-verify-nuget-consumer.yml` | `workflow_call` |

## Coverage floors as enforced

`scripts/ci/kernel-coverage-gate.sh` (run by `kernel-coverage-gate.yml`) enforces **Domain 100% / Infrastructure 80% / Core.Application 67%** line coverage (`INFRA_COVERAGE_THRESHOLD` default 80, measured ~80.3%, target 83 — see [`production-readiness/KernelCoverageGate-Findings.md`](production-readiness/KernelCoverageGate-Findings.md)). Neither check is required by branch protection.

## Policy

- The only merge-blocking check is `cert-gate`. Treat every other gate as a review signal and read red checks before merging; that is a process rule, not a setting.
- To promote a gate to required, first make it always report on PRs (always-report job or in-job path filtering), then add its context to branch protection and to the table at the top of this file in the same change.
- Release workflows (`release*`, `runtime-release*`, `rc-gate`, `reusable-*`) are not PR branch-protection checks.
- Branch protection is not represented by YAML; when the setting changes, update this file (and [`GitHubBranchProtection.md`](GitHubBranchProtection.md)) in the same PR.

## Pruning (2026-08-16)

Every workflow file was classified from `gh run list --workflow <file> --limit 15 --json conclusion,createdAt,event` plus its `on:` block (PR `ci/workflow-pruning`; the full 62-row table is in that PR's description). Classes: **active-green**, **active-flaky**, **dead** (no run in 60 days and no `push`/`pull_request`/`schedule` trigger that can fire), **duplicate**, **always-red**. Only `cert-gate` is required by branch protection (verified with `gh api repos/IanFrelinger/Nexo/branches/master/protection`), so none of the changes below affects merges.

**Deleted (7)** — recoverable from git history at `71963059`:

| File | Why |
| --- | --- |
| `core-domain-coverage.yml` | duplicate: identical `dotnet test src/Ashlar.Tests.Domain … /p:Threshold=100` step to the first leg of `scripts/ci/kernel-coverage-gate.sh`, same PR/push paths |
| `runtime-studio-playground.yml` | duplicate of `cross-platform-tests.yml` `scope=playground` (same 3-OS matrix, same filters); 4/4 red, last run 2026-05-11, dispatch-only |
| `test-persistence-multi-os.yml` | duplicate of `cross-platform-tests.yml` `scope=persistence`; 15/15 red — tests pass, the `publish-unit-test-result-action` step 403s on `check-runs` |
| `test-caching-multi-env.yml` | always-red (14 red + 1 cancelled of 15), muted 2026-08-11, dispatch-only; the `Dockerfile.test-caching*` images it built are still validated by `compose-gate.yml` |
| `mapbox-tile-helpers-ci.yml` | always-red (15/15), dispatch-only; the job-level `if: secrets.MAPBOX_ACCESS_TOKEN != ''` is not a valid context there. Tests remain runnable locally with `ASHLAR_TEST_MAPBOX_TILES=1` |
| `runtime-studio-forge-smoke.yml` | dead: dispatch-only, last run 2026-06-14, referenced nowhere |
| `mesh-lab-remote-gate.yml` | dead: never dispatched, needs five repository secrets and a tailnet runner; `scripts/mesh-lab-verify-remote.sh` is the supported path |

**Marked `# DORMANT:` (7)** — top-of-file comment with the reason and date; `workflow_dispatch` stays live:

| File | Why |
| --- | --- |
| `cross-platform-tests.yml` | push trigger commented out 2026-08-11 (15/15 red on a product assertion, issue #252); only Windows/macOS matrix in the repo |
| `mesh-lab-gate.yml` | push trigger commented out 2026-08-11 (15/15 red in the compose environment); mesh-lab entry point |
| `mesh-lab-stress-gate.yml` | **weekly schedule removed** after eight consecutive red runs (2026-06-22 .. 2026-08-10) |
| `runtime-release-promotion.yml` | 11 of last 14 red, last run 2026-05-11; kept because `scripts/rc-gate-tier-d.sh` lists it as an optional RC signal |
| `test-air-gapped-no-network.yml` | never green (11/11 red since 2026-03-08; last failure is MSB1011 from the `ashlar test multi-env` step); cited by hardening plans, so kept as an unproven claim |
| `test-trust-multi-env.yml` | dead by the 60-day rule (last dispatch 2026-05-23, mostly green); cited by `KernelHardeningPlan-v1.md` C1 |
| `workflow-regression-gate.yml` | dead by the 60-day rule (last dispatch 2026-06-14, green); only end-to-end run of `ashlar workflow baseline|report|gate` |

**Kept as-is although rarely run** (all have a live path/manual trigger and a Makefile/script/runbook that names them): `compat-gate`, `dr-gate` (path-triggered on their scripts, one green run each), `waterproofing-gate`, `perf-certification`, `installer-bruteforce-gate` (dispatched by `scripts/rc-gate-tier-d.sh`), `nuget-consumer-verify` (post-publish check, `docs/NuGetConsumerVerify.md`), `setup-smoke-suite` (`docs/CiFirstHardwareSecond.md`), `devlog-ghost-release`, `mesh-lab-tls-gate` (weekly, latest run green).

**Not folded:** the CLI image is built by `container-image-gate` (local + multi-arch cache-only), `distribution-matrix-gate` (build + `--help` smoke), `full-platform-readiness-gate` and `reusable-container-publish` (push to GHCR). Each build differs in platform, output and smoke, and three of the four are on the protected list, so a shared reusable job is deferred to its own PR.

**Fork safety (L10):** `container-image-publish.yml`, `release.yml`, `release-nuget.yml` and `release-staging-on-label.yml` now carry `if: github.repository_owner == 'IanFrelinger'`; every secret and repository variable a workflow reads, and what a fork gets without it, is in [`CiSecrets.md`](CiSecrets.md).
