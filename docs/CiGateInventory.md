# CI gate inventory

This file is the authoritative source for Nexo's CI branch-protection policy. Workflow YAML controls when checks run; GitHub branch protection controls which check names are required before merge.

Current repository snapshot: **55 workflow files** under `.github/workflows/` (62 before the 2026-08-16 pruning, see [Pruning](#pruning-2026-08-16)). After Sprint 3 right-sizing, **10 workflows** still trigger on `pull_request` and the rest are advisory, scheduled, manual, push-only, or release-only.

## Required checks (branch protection)

Require these exact status check contexts on `master` / `main` branch protection. The distribution matrix is listed by job because it intentionally proves several consumer channels in parallel.

| Workflow file | Workflow name | Required check context(s) |
| --- | --- | --- |
| `.github/workflows/kernel-gate.yml` | Kernel Gate | `Kernel Gate / kernel-gate` |
| `.github/workflows/application-gate.yml` | Application Gate | `Application Gate / application-gate` |
| `.github/workflows/kernel-coverage-gate.yml` | Kernel coverage gate | `Kernel coverage gate / kernel-coverage` |
| `.github/workflows/dependency-boundary.yml` | dependency-boundary | `dependency-boundary / verify` |
| `.github/workflows/layer-boundary.yml` | layer-boundary | `layer-boundary / verify` |
| `.github/workflows/docs-link-check.yml` | Docs Link Check | `Docs Link Check / lychee (README + docs)` |
| `.github/workflows/testing-strategy-gate.yml` | Testing strategy gate | `Testing strategy gate / testing-strategy` |
| `.github/workflows/security-gate.yml` | Security Gate | `Security Gate / security-gate` |
| `.github/workflows/distribution-matrix-gate.yml` | Distribution Matrix Gate | `Distribution Matrix Gate / NuGet local pack → StableSdkHostSample`<br>`Distribution Matrix Gate / CLI image build + smoke`<br>`Distribution Matrix Gate / API image + curl /health + /api/status`<br>`Distribution Matrix Gate / Nexo.Client ↔ in-process Nexo.API (net9)`<br>`Distribution Matrix Gate / Pack script vs Nexo.Hosting graph`<br>`Distribution Matrix Gate / Standalone brick authoring scaffold` |


### Branch protection update snippet

Human runs this; agents cannot change repository settings. Replace `OWNER`, `REPO`, and `BRANCH` if needed.

```bash
OWNER="IanFrelinger"
REPO="Nexo"
BRANCH="master"

cat > /tmp/nexo-required-checks.json <<'JSON'
{
  "required_status_checks": {
    "strict": true,
    "contexts": [
          "Kernel Gate / kernel-gate",
          "Application Gate / application-gate",
          "Kernel coverage gate / kernel-coverage",
          "dependency-boundary / verify",
          "layer-boundary / verify",
          "Docs Link Check / lychee (README + docs)",
          "Testing strategy gate / testing-strategy",
          "Security Gate / security-gate",
          "Distribution Matrix Gate / NuGet local pack → StableSdkHostSample",
          "Distribution Matrix Gate / CLI image build + smoke",
          "Distribution Matrix Gate / API image + curl /health + /api/status",
          "Distribution Matrix Gate / Nexo.Client ↔ in-process Nexo.API (net9)",
          "Distribution Matrix Gate / Pack script vs Nexo.Hosting graph",
          "Distribution Matrix Gate / Standalone brick authoring scaffold"
]
  }
}
JSON

gh api   --method PATCH   -H "Accept: application/vnd.github+json"   -H "X-GitHub-Api-Version: 2022-11-28"   "/repos/$OWNER/$REPO/branches/$BRANCH/protection"   --input /tmp/nexo-required-checks.json
```

## Advisory / scheduled / manual workflows

Every workflow not in the required table remains available. Demotion means removing `pull_request` from specialized/exploratory gates; no workflow was deleted and no gate implementation was weakened.

| Workflow file | Name | Trigger profile | Classification |
| --- | --- | --- | --- |
| `.github/workflows/application-gate.yml` | Application Gate | pull_request, workflow_dispatch | Required (branch protection) |
| `.github/workflows/compat-gate.yml` | compat-gate | push, workflow_dispatch | Advisory / scheduled / manual |
| `.github/workflows/compose-gate.yml` | Compose Gate | push, workflow_dispatch | Advisory / scheduled / manual |
| `.github/workflows/composition-mesh-gate.yml` | Composition Mesh Gate | workflow_dispatch | Advisory / scheduled / manual |
| `.github/workflows/container-image-gate.yml` | Container Image Gate | push, workflow_dispatch | Advisory / scheduled / manual |
| `.github/workflows/container-image-publish.yml` | Container Image Publish | push, workflow_dispatch | Advisory / scheduled / manual |
| `.github/workflows/cross-platform-tests.yml` | Cross-Platform Tests | workflow_dispatch (dormant) | Advisory / scheduled / manual |
| `.github/workflows/dependency-boundary.yml` | dependency-boundary | pull_request, push, workflow_dispatch | Required (branch protection) |
| `.github/workflows/devcontainer-gate.yml` | Dev Container Gate | push, workflow_dispatch | Advisory / scheduled / manual |
| `.github/workflows/devlog-ghost-release.yml` | Devlog Ghost publish | workflow_dispatch | Advisory / scheduled / manual |
| `.github/workflows/distribution-matrix-gate.yml` | Distribution Matrix Gate | pull_request, push, schedule, workflow_dispatch | Required (branch protection) |
| `.github/workflows/docs-link-check.yml` | Docs Link Check | pull_request, push, workflow_dispatch | Required (branch protection) |
| `.github/workflows/dr-gate.yml` | dr-gate | push, workflow_dispatch | Advisory / scheduled / manual |
| `.github/workflows/environment-setup-gate-v1.yml` | Environment Setup Gate v1 | push, workflow_dispatch | Advisory / scheduled / manual |
| `.github/workflows/friend-mesh-prefab-gate.yml` | Friend mesh prefab gate | push, workflow_dispatch | Advisory / scheduled / manual |
| `.github/workflows/full-platform-readiness-gate.yml` | Full Platform Readiness Gate | push, schedule, workflow_dispatch | Advisory / scheduled / manual |
| `.github/workflows/grpc-transport-gate.yml` | gRPC transport gate | push, workflow_dispatch | Advisory / scheduled / manual |
| `.github/workflows/installer-bruteforce-gate.yml` | Installer Bruteforce Gate | workflow_dispatch | Advisory / scheduled / manual |
| `.github/workflows/kernel-coverage-gate.yml` | Kernel coverage gate | pull_request, push | Required (branch protection) |
| `.github/workflows/kernel-gate.yml` | Kernel Gate | pull_request, push, workflow_dispatch | Required (branch protection) |
| `.github/workflows/layer-boundary.yml` | layer-boundary | pull_request | Required (branch protection) |
| `.github/workflows/mesh-lab-gate.yml` | Mesh virtual lab gate | workflow_dispatch (dormant) | Advisory / scheduled / manual |
| `.github/workflows/mesh-lab-stress-gate.yml` | Mesh lab stress gate | workflow_dispatch (dormant) | Advisory / scheduled / manual |
| `.github/workflows/mesh-lab-tls-gate.yml` | Mesh lab TLS gate | schedule, workflow_dispatch | Advisory / scheduled / manual |
| `.github/workflows/nuget-consumer-verify.yml` | NuGet consumer verify | workflow_dispatch | Advisory / scheduled / manual |
| `.github/workflows/onboarding-docs-guard.yml` | Onboarding Docs Guard | push, workflow_dispatch | Advisory / scheduled / manual |
| `.github/workflows/onboarding-quickstart-gate.yml` | onboarding-quickstart-gate | push, schedule, workflow_dispatch | Advisory / scheduled / manual |
| `.github/workflows/ops-gate.yml` | Ops Gate | workflow_dispatch | Advisory / scheduled / manual |
| `.github/workflows/optimize-agent-cluster-gate.yml` | Optimize Agent Cluster Gate | push, workflow_dispatch | Advisory / scheduled / manual |
| `.github/workflows/pack-hosting-graph-alignment.yml` | Pack hosting graph alignment | push, workflow_dispatch | Advisory / scheduled / manual |
| `.github/workflows/perf-certification.yml` | perf-certification | workflow_dispatch | Advisory / scheduled / manual |
| `.github/workflows/perf-gate.yml` | perf-gate | push, workflow_dispatch | Advisory / scheduled / manual |
| `.github/workflows/prod-dry-run-pr.yml` | Prod dry run (Compose) | workflow_dispatch | Advisory / scheduled / manual |
| `.github/workflows/production-readiness-gate-v1.yml` | Production Readiness Gate v1 | push, workflow_dispatch | Advisory / scheduled / manual |
| `.github/workflows/rc-gate.yml` | RC Gate | push, schedule, workflow_dispatch | Release gate / reusable |
| `.github/workflows/release-nuget.yml` | Release NuGet packages | workflow_dispatch | Release gate / reusable |
| `.github/workflows/release.yml` | Release | push, workflow_dispatch | Release gate / reusable |
| `.github/workflows/reusable-container-publish.yml` | reusable-container-publish | workflow_call | Release gate / reusable |
| `.github/workflows/reusable-release-nuget.yml` | reusable-release-nuget | workflow_call | Release gate / reusable |
| `.github/workflows/reusable-verify-nuget-consumer.yml` | reusable-verify-nuget-consumer | workflow_call | Release gate / reusable |
| `.github/workflows/runtime-release-gate.yml` | Runtime Release Gate | push, workflow_dispatch | Release gate / reusable |
| `.github/workflows/runtime-release-promotion.yml` | Runtime Release Promotion | workflow_dispatch (dormant) | Release gate / reusable |
| `.github/workflows/security-gate.yml` | Security Gate | pull_request, workflow_dispatch | Required (branch protection) |
| `.github/workflows/setup-smoke-suite.yml` | Setup Smoke Suite | workflow_dispatch | Advisory / scheduled / manual |
| `.github/workflows/shell-lint.yml` | Shell lint | pull_request (paths: `scripts/**`), workflow_dispatch | Advisory / scheduled / manual |
| `.github/workflows/ship-gate.yml` | Ship Gate | workflow_dispatch | Advisory / scheduled / manual |
| `.github/workflows/test-air-gapped-no-network.yml` | Air-Gapped Multi-Env Tests | workflow_dispatch (dormant) | Advisory / scheduled / manual |
| `.github/workflows/test-trust-multi-env.yml` | Trust Tests (Multi-Env Docker) | workflow_dispatch (dormant) | Advisory / scheduled / manual |
| `.github/workflows/testing-strategy-gate.yml` | Testing strategy gate | pull_request | Required (branch protection) |
| `.github/workflows/waterproofing-gate.yml` | waterproofing-gate | workflow_dispatch | Advisory / scheduled / manual |
| `.github/workflows/workflow-regression-gate.yml` | Workflow Regression Gate | workflow_dispatch (dormant) | Advisory / scheduled / manual |


## Policy

- Required checks are limited to boundary, coverage, security, testing-strategy, docs, kernel/application, and distribution safety signals.
- Specialized gates remain manual/scheduled/push/advisory unless they have narrow `paths:` filters and are promoted into the required table above.
- Release workflows (`release*`, `runtime-release*`, `rc-gate`, `reusable-*`) are not PR branch-protection checks.
- Branch protection is not represented by YAML; keep this document and repository settings in sync.

## Pruning (2026-08-16)

Every workflow file was classified from `gh run list --workflow <file> --limit 15 --json conclusion,createdAt,event` plus its `on:` block (PR `ci/workflow-pruning`; the full 62-row table is in that PR's description). Classes: **active-green**, **active-flaky**, **dead** (no run in 60 days and no `push`/`pull_request`/`schedule` trigger that can fire), **duplicate**, **always-red**. Only `cert-gate` is required by branch protection (verified with `gh api repos/IanFrelinger/Nexo/branches/master/protection`), so none of the changes below affects merges.

**Deleted (7)** — recoverable from git history at `71963059`:

| File | Why |
| --- | --- |
| `core-domain-coverage.yml` | duplicate: identical `dotnet test src/Nexo.Tests.Domain … /p:Threshold=100` step to the first leg of `scripts/ci/kernel-coverage-gate.sh`, same PR/push paths |
| `runtime-studio-playground.yml` | duplicate of `cross-platform-tests.yml` `scope=playground` (same 3-OS matrix, same filters); 4/4 red, last run 2026-05-11, dispatch-only |
| `test-persistence-multi-os.yml` | duplicate of `cross-platform-tests.yml` `scope=persistence`; 15/15 red — tests pass, the `publish-unit-test-result-action` step 403s on `check-runs` |
| `test-caching-multi-env.yml` | always-red (14 red + 1 cancelled of 15), muted 2026-08-11, dispatch-only; the `Dockerfile.test-caching*` images it built are still validated by `compose-gate.yml` |
| `mapbox-tile-helpers-ci.yml` | always-red (15/15), dispatch-only; the job-level `if: secrets.MAPBOX_ACCESS_TOKEN != ''` is not a valid context there. Tests remain runnable locally with `NEXO_TEST_MAPBOX_TILES=1` |
| `runtime-studio-forge-smoke.yml` | dead: dispatch-only, last run 2026-06-14, referenced nowhere |
| `mesh-lab-remote-gate.yml` | dead: never dispatched, needs five repository secrets and a tailnet runner; `scripts/mesh-lab-verify-remote.sh` is the supported path |

**Marked `# DORMANT:` (7)** — top-of-file comment with the reason and date; `workflow_dispatch` stays live:

| File | Why |
| --- | --- |
| `cross-platform-tests.yml` | push trigger commented out 2026-08-11 (15/15 red on a product assertion, issue #252); only Windows/macOS matrix in the repo |
| `mesh-lab-gate.yml` | push trigger commented out 2026-08-11 (15/15 red in the compose environment); mesh-lab entry point |
| `mesh-lab-stress-gate.yml` | **weekly schedule removed** after eight consecutive red runs (2026-06-22 .. 2026-08-10) |
| `runtime-release-promotion.yml` | 11 of last 14 red, last run 2026-05-11; kept because `scripts/rc-gate-tier-d.sh` lists it as an optional RC signal |
| `test-air-gapped-no-network.yml` | never green (11/11 red since 2026-03-08; last failure is MSB1011 from the `nexo test multi-env` step); cited by hardening plans, so kept as an unproven claim |
| `test-trust-multi-env.yml` | dead by the 60-day rule (last dispatch 2026-05-23, mostly green); cited by `KernelHardeningPlan-v1.md` C1 |
| `workflow-regression-gate.yml` | dead by the 60-day rule (last dispatch 2026-06-14, green); only end-to-end run of `nexo workflow baseline|report|gate` |

**Kept as-is although rarely run** (all have a live path/manual trigger and a Makefile/script/runbook that names them): `compat-gate`, `dr-gate` (path-triggered on their scripts, one green run each), `composition-mesh-gate`, `waterproofing-gate`, `perf-certification`, `installer-bruteforce-gate` (dispatched by `scripts/rc-gate-tier-d.sh`), `nuget-consumer-verify` (post-publish check, `docs/NuGetConsumerVerify.md`), `setup-smoke-suite` (`docs/CiFirstHardwareSecond.md`), `devlog-ghost-release`, `mesh-lab-tls-gate` (weekly, latest run green).

**Not folded:** the CLI image is built by `container-image-gate` (local + multi-arch cache-only), `distribution-matrix-gate` (build + `--help` smoke), `full-platform-readiness-gate` and `reusable-container-publish` (push to GHCR). Each build differs in platform, output and smoke, and three of the four are on the protected list, so a shared reusable job is deferred to its own PR.

**Fork safety (L10):** `container-image-publish.yml`, `release.yml`, `release-nuget.yml` and `release-staging-on-label.yml` now carry `if: github.repository_owner == 'IanFrelinger'`; every secret and repository variable a workflow reads, and what a fork gets without it, is in [`CiSecrets.md`](CiSecrets.md).
