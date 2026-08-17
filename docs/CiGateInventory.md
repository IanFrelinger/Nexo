# CI gate inventory

This file describes what CI **actually does** on this repository: which workflow files exist, what triggers each one, and which checks branch protection **really** requires. Workflow YAML controls when checks run; GitHub branch protection (a repository setting, not YAML) controls which check names must be green before merge. Where the two disagree, this file follows the settings and says so.

Snapshot: **62 workflow files** under `.github/workflows/` (`git ls-files ".github/workflows/*.yml"`), verified 2026-08-16.

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
REPO="Nexo"
BRANCH="master"

cat > /tmp/nexo-required-checks.json <<'JSON'
{
  "required_status_checks": {
    "strict": true,
    "contexts": [
      "cert-gate"
    ]
  }
}
JSON

gh api --method PATCH -H "Accept: application/vnd.github+json" -H "X-GitHub-Api-Version: 2022-11-28" "/repos/$OWNER/$REPO/branches/$BRANCH/protection" --input /tmp/nexo-required-checks.json
```

## Trigger map

Counts by trigger class (62 files):

| Class | Count | Meaning |
| --- | --- | --- |
| Runs on `pull_request` | 14 | 2 unfiltered (`cert-gate`, `layer-boundary`), 11 path-filtered, 1 label-driven (`release-staging-on-label`) |
| Push-only (path-filtered on `master`/`main`/`cursor/**`), plus `workflow_dispatch` | 20 | Post-merge signal; never blocks a PR |
| `workflow_dispatch` only | 21 | Manual lanes (mesh labs, multi-env Docker suites, ship/ops/perf, release plumbing) |
| `schedule` + `workflow_dispatch` only | 2 | `mesh-lab-stress-gate` (Mon 06:00 UTC), `mesh-lab-tls-gate` (Tue 07:00 UTC) |
| Tag / release event | 2 | `release.yml` (`v*.*.*` tags), `devlog-ghost-release.yml` (`release: published`) |
| Reusable (`workflow_call`) | 3 | `reusable-*` |

Six workflows carry a `schedule`: `distribution-matrix-gate` (Mon 10:00 UTC), `full-platform-readiness-gate` (Mon 06:00), `onboarding-quickstart-gate` (Mon 07:00), `rc-gate` (06:00 on the 1st of each month), `mesh-lab-stress-gate` (Mon 06:00), `mesh-lab-tls-gate` (Tue 07:00).

### PR-triggered workflows

| Workflow file | Name / job(s) | PR trigger | Also |
| --- | --- | --- | --- |
| `cert-gate.yml` | Cert gate / `cert-gate` | **every PR** (no paths) | push `master`, dispatch — **required** |
| `layer-boundary.yml` | layer-boundary / `verify` | every PR (`paths: "**"`, types opened/synchronize/reopened/edited) | — |
| `application-gate.yml` | Application Gate / `application-gate` | paths: `application/**`, VirtualProduction tests, `scripts/application-gate*.sh`, `scripts/prod-dry-run.sh`, `Makefile`, … | dispatch |
| `core-domain-coverage.yml` | Core domain coverage / `domain-coverage` | paths: `src/Nexo.Core.Domain/**`, `src/Nexo.Infrastructure/**`, kernel test projects, `Directory.*.props` | push (same paths) |
| `dependency-boundary.yml` | dependency-boundary / `verify` | paths: `**/*.csproj`, `commercial/**`, `application/**`, `applications/**`, `src/**`, `LICENSING.md`, boundary scripts | push, dispatch |
| `distribution-matrix-gate.yml` | Distribution Matrix Gate / 7 jobs | paths: same broad list as push (Dockerfiles, pack/verify scripts, Nexo.API/CLI, Client/Sdk/Hosting.Bundle/Authoring/Brick.Contracts, samples, VirtualProduction tests) | push (broad paths), weekly schedule, dispatch |
| `docs-link-check.yml` | Docs Link Check / `lychee (README + docs)` | paths: `docs/**`, `README.md`, `.lycheeignore` | push, dispatch |
| `kernel-coverage-gate.yml` | Kernel coverage gate / `kernel-coverage` | paths: kernel src + tests, `scripts/ci/kernel-coverage-gate.sh`, `scripts/ci/pr-testing-strategy-gate.sh` | push |
| `kernel-gate.yml` | Kernel Gate / `kernel-gate` | paths: `src/Nexo.Hosting/**`, Infrastructure, Orchestration, Runtime, Core.Application, kernel tests, `docs/production-readiness/**`, `Makefile` | push (narrower paths), dispatch |
| `provenance-graph-gate.yml` | Provenance Graph CI / `unit-tests`, `integration-tests` | paths: `applications/Nexo.Provenance.Graph*/**`, `deploy/compose/docker-compose.provenance.yml` | dispatch |
| `security-gate.yml` | Security Gate / `security-gate` | paths: Trust/Security sources and tests, `scripts/security-gate*.sh`, `Makefile` | dispatch |
| `shell-lint.yml` | Shell lint / `shell-lint` | paths: `scripts/**` | dispatch |
| `testing-strategy-gate.yml` | Testing strategy gate / `testing-strategy` | paths: `src/**`, `application/**`, `scripts/**`, `.github/**`, `Makefile`, `docs/architecture/TestingStrategy*.md` | — |
| `release-staging-on-label.yml` | Release staging on label / `dispatch-staging-release` | `types: [labeled]` only | — |

### Push-only (path-filtered) workflows

All of these also accept `workflow_dispatch`. Branch filters are `master`, `main`, `cursor/**` unless noted.

| Workflow file | Name | Notes |
| --- | --- | --- |
| `compat-gate.yml` | compat-gate | `master` only; `scripts/compat-gate*.sh`, `scripts/kernel-gate-tier-b.sh` |
| `compose-gate.yml` | Compose Gate | compose test stacks, `.docker/Dockerfile.test-caching*`, CLI, README |
| `container-image-gate.yml` | Container Image Gate | `.docker/Dockerfile.cli`, CLI + spine sources |
| `container-image-publish.yml` | Container Image Publish | `master`/`main`; `.docker/**`, hosts, spine sources — publishes GHCR images |
| `devcontainer-gate.yml` | Dev Container Gate | `.devcontainer/**`, `Nexo.LocalDevCore.slnf`, CLI |
| `dr-gate.yml` | dr-gate | `master` only; `scripts/dr-gate*.sh` |
| `environment-setup-gate-v1.yml` | Environment Setup Gate v1 | `master`/`main`; `scripts/setup/**`, CLI |
| `friend-mesh-prefab-gate.yml` | Friend mesh prefab gate | friend-mesh compose, `.docker/Dockerfile.api`, `Nexo.API` |
| `full-platform-readiness-gate.yml` | Full Platform Readiness Gate | Dockerfiles, setup/install scripts, spine sources, StableSdkHostSample; **weekly schedule** |
| `grpc-transport-gate.yml` | gRPC transport gate | `src/Nexo.Transport.Grpc/**`, `src/Nexo.Tests.Transport/**` |
| `mcp-a2a-gate.yml` | MCP + A2A protocol gate | also `application/**` branches; `src/Nexo.Mcp.*`, `src/Nexo.Transport.A2A*`, `Nexo.API` |
| `onboarding-docs-guard.yml` | Onboarding Docs Guard | README, `docs/**/*.md`, `scripts/*.sh`, `scripts/*.ps1`, `Makefile`, `**/*.csproj` (ProjectTiers guard) |
| `onboarding-quickstart-gate.yml` | onboarding-quickstart-gate | README, GettingStarted, setup/install scripts, CLI; **weekly schedule** |
| `optimize-agent-cluster-gate.yml` | Optimize Agent Cluster Gate | `apps/runtime-studio/**`, `scripts/sandbox/**`, CLI |
| `pack-hosting-graph-alignment.yml` | Pack hosting graph alignment | `master`/`main`; `src/**/*.csproj`, pack scripts, NugetOrgRestoreVerify sample |
| `perf-gate.yml` | perf-gate | `master` only; `scripts/perf-gate*.sh`, Orchestration/BackgroundAgents tests |
| `production-readiness-gate-v1.yml` | Production Readiness Gate v1 | pipelines sources/tests, CLI, readiness docs |
| `rc-gate.yml` | RC Gate | `master`/`main`; RC docs + scripts; **monthly schedule** |
| `runtime-release-gate.yml` | Runtime Release Gate | `master`/`main`; CLI runtime/release commands, `docs/runtime/benchmarks/**` |
| `test-persistence-multi-os.yml` | Persistence Tests (Multi-OS) | `master`/`main`; persistence sources/tests, Windows Dockerfile |

### Manual-only workflows (`workflow_dispatch`)

`composition-mesh-gate`, `cross-platform-tests`, `installer-bruteforce-gate`, `mapbox-tile-helpers-ci`, `mesh-lab-gate`, `mesh-lab-remote-gate`, `nuget-consumer-verify`, `ops-gate`, `perf-certification`, `prod-dry-run-pr`, `release-nuget`, `runtime-release-promotion`, `runtime-studio-forge-smoke`, `runtime-studio-playground`, `setup-smoke-suite`, `ship-gate`, `test-air-gapped-no-network`, `test-caching-multi-env`, `test-trust-multi-env`, `waterproofing-gate`, `workflow-regression-gate` (21).

Despite their names, **`cross-platform-tests`** and **`prod-dry-run-pr`** do not run on PRs; run them with `gh workflow run "<name>" --ref <branch>`.

### Scheduled-only, tag/release, reusable

| Workflow file | Trigger |
| --- | --- |
| `mesh-lab-stress-gate.yml` | `schedule` Mon 06:00 UTC + dispatch |
| `mesh-lab-tls-gate.yml` | `schedule` Tue 07:00 UTC + dispatch |
| `release.yml` | push tags `v*.*.*` + dispatch |
| `devlog-ghost-release.yml` | `release: published` + dispatch |
| `reusable-container-publish.yml`, `reusable-release-nuget.yml`, `reusable-verify-nuget-consumer.yml` | `workflow_call` |

## Coverage floors as enforced

`scripts/ci/kernel-coverage-gate.sh` (run by `kernel-coverage-gate.yml`) enforces **Domain 100% / Infrastructure 80% / Core.Application 67%** line coverage (`INFRA_COVERAGE_THRESHOLD` default 80, measured ~80.3%, target 83 — see [`production-readiness/KernelCoverageGate-Findings.md`](production-readiness/KernelCoverageGate-Findings.md)). `core-domain-coverage.yml` enforces Domain 100%. Neither check is required by branch protection.

## Policy

- The only merge-blocking check is `cert-gate`. Treat every other gate as a review signal and read red checks before merging; that is a process rule, not a setting.
- To promote a gate to required, first make it always report on PRs (always-report job or in-job path filtering), then add its context to branch protection and to the table at the top of this file in the same change.
- Release workflows (`release*`, `runtime-release*`, `rc-gate`, `reusable-*`) are not PR branch-protection checks.
- Branch protection is not represented by YAML; when the setting changes, update this file (and [`GitHubBranchProtection.md`](GitHubBranchProtection.md)) in the same PR.
