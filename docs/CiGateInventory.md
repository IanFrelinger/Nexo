# CI gate inventory

This file is the authoritative source for Nexo's CI branch-protection policy. Workflow YAML controls when checks run; GitHub branch protection controls which check names are required before merge.

Current repository snapshot: **57 workflow files** under `.github/workflows/`. After Sprint 3 right-sizing, **10 workflows** still trigger on `pull_request` and the rest are advisory, scheduled, manual, push-only, or release-only.

## Required checks (branch protection)

Require these exact status check contexts on `master` / `main` branch protection. The distribution matrix is listed by job because it intentionally proves several consumer channels in parallel.

| Workflow file | Workflow name | Required check context(s) |
| --- | --- | --- |
| `.github/workflows/kernel-gate.yml` | Kernel Gate | `Kernel Gate / kernel-gate` |
| `.github/workflows/application-gate.yml` | Application Gate | `Application Gate / application-gate` |
| `.github/workflows/core-domain-coverage.yml` | Core domain coverage | `Core domain coverage / domain-coverage` |
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
          "Core domain coverage / domain-coverage",
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
| `.github/workflows/core-domain-coverage.yml` | Core domain coverage | pull_request, push | Required (branch protection) |
| `.github/workflows/cross-platform-tests.yml` | Cross-Platform Tests | push, workflow_dispatch | Advisory / scheduled / manual |
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
| `.github/workflows/mapbox-tile-helpers-ci.yml` | Mapbox Tile Helpers (optional) | workflow_dispatch | Advisory / scheduled / manual |
| `.github/workflows/mesh-lab-gate.yml` | Mesh virtual lab gate | push, workflow_dispatch | Advisory / scheduled / manual |
| `.github/workflows/mesh-lab-remote-gate.yml` | Mesh lab remote gate (self-hosted / tailnet) | workflow_dispatch | Advisory / scheduled / manual |
| `.github/workflows/mesh-lab-stress-gate.yml` | Mesh lab stress gate | schedule, workflow_dispatch | Advisory / scheduled / manual |
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
| `.github/workflows/runtime-release-promotion.yml` | Runtime Release Promotion | workflow_dispatch | Release gate / reusable |
| `.github/workflows/runtime-studio-forge-smoke.yml` | Runtime Studio forge smoke | workflow_dispatch | Advisory / scheduled / manual |
| `.github/workflows/runtime-studio-playground.yml` | Runtime Studio Playground | workflow_dispatch | Advisory / scheduled / manual |
| `.github/workflows/security-gate.yml` | Security Gate | pull_request, workflow_dispatch | Required (branch protection) |
| `.github/workflows/setup-smoke-suite.yml` | Setup Smoke Suite | workflow_dispatch | Advisory / scheduled / manual |
| `.github/workflows/shell-lint.yml` | Shell lint | pull_request (paths: `scripts/**`), workflow_dispatch | Advisory / scheduled / manual |
| `.github/workflows/ship-gate.yml` | Ship Gate | workflow_dispatch | Advisory / scheduled / manual |
| `.github/workflows/test-air-gapped-no-network.yml` | Air-Gapped Multi-Env Tests | workflow_dispatch | Advisory / scheduled / manual |
| `.github/workflows/test-caching-multi-env.yml` | Test Caching Multi-Environment | push, workflow_dispatch | Advisory / scheduled / manual |
| `.github/workflows/test-persistence-multi-os.yml` | Persistence Tests (Multi-OS) | push, workflow_dispatch | Advisory / scheduled / manual |
| `.github/workflows/test-trust-multi-env.yml` | Trust Tests (Multi-Env Docker) | workflow_dispatch | Advisory / scheduled / manual |
| `.github/workflows/testing-strategy-gate.yml` | Testing strategy gate | pull_request | Required (branch protection) |
| `.github/workflows/waterproofing-gate.yml` | waterproofing-gate | workflow_dispatch | Advisory / scheduled / manual |
| `.github/workflows/workflow-regression-gate.yml` | Workflow Regression Gate | workflow_dispatch | Advisory / scheduled / manual |


## Policy

- Required checks are limited to boundary, coverage, security, testing-strategy, docs, kernel/application, and distribution safety signals.
- Specialized gates remain manual/scheduled/push/advisory unless they have narrow `paths:` filters and are promoted into the required table above.
- Release workflows (`release*`, `runtime-release*`, `rc-gate`, `reusable-*`) are not PR branch-protection checks.
- Branch protection is not represented by YAML; keep this document and repository settings in sync.
