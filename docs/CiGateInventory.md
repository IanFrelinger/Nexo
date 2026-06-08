# CI gate inventory

This inventory documents the workflows currently present under `.github/workflows/`. It is descriptive only: this sprint does **not** delete, disable, or consolidate any workflow.

Tier labels are intentionally conservative:

- **Blocking candidate** — runs on `pull_request` and/or path-gated `push` and may be appropriate for branch protection when its scope matches the changed area.
- **Advisory/manual** — manual, scheduled, optional, self-hosted, exploratory, or specialized signal.
- **Release gate** — release/publish/promotion workflow or reusable release building block.

## Workflow inventory

| Workflow file | Name | Trigger profile | Purpose | Tier |
|---------------|------|-----------------|---------|------|
| `.github/workflows/application-gate.yml` | Application Gate | `pull_request`, `workflow_dispatch` | Application/API tier build and smoke gate with selectable manual tiers. | Blocking candidate |
| `.github/workflows/compat-gate.yml` | compat-gate | `push`, `workflow_dispatch` | Compatibility validation for source/polyfill and package compatibility areas. | Blocking candidate |
| `.github/workflows/compose-gate.yml` | Compose Gate | `push`, `pull_request`, `workflow_dispatch` | Compose configuration and containerized test-lane validation. | Blocking candidate |
| `.github/workflows/composition-mesh-gate.yml` | Composition Mesh Gate | `pull_request`, `workflow_dispatch` | Composition and mesh behavior checks with manual tier expansion. | Blocking candidate |
| `.github/workflows/container-image-gate.yml` | Container Image Gate | `push`, `pull_request`, `workflow_dispatch` | Container image buildability and smoke-run validation. | Blocking candidate |
| `.github/workflows/container-image-publish.yml` | Container Image Publish | `push`, `workflow_dispatch` | Publishes GHCR images outside tag-driven release flow. | Release gate |
| `.github/workflows/core-domain-coverage.yml` | Core domain coverage | `push`, `pull_request` | Core domain coverage ratchet. | Blocking candidate |
| `.github/workflows/cross-platform-tests.yml` | Cross-Platform Tests | `push`, `workflow_dispatch` | Cross-platform test matrix for selected scopes. | Blocking candidate |
| `.github/workflows/devcontainer-gate.yml` | Dev Container Gate | `push`, `pull_request`, `workflow_dispatch` | Dev Container restore/build/help smoke. | Blocking candidate |
| `.github/workflows/devlog-ghost-release.yml` | Devlog Ghost publish | `workflow_dispatch` | Optional manual devlog publishing. | Advisory/manual |
| `.github/workflows/distribution-matrix-gate.yml` | Distribution Matrix Gate | `push`, `pull_request`, `schedule`, `workflow_dispatch` | NuGet, CLI image, API image, client, and pack-graph distribution checks. | Blocking candidate |
| `.github/workflows/docs-link-check.yml` | Docs Link Check | `push`, `pull_request`, `workflow_dispatch` | Lychee link validation for README and docs. | Blocking candidate |
| `.github/workflows/dr-gate.yml` | dr-gate | `push`, `workflow_dispatch` | Disaster recovery readiness/hardening signal. | Blocking candidate |
| `.github/workflows/environment-setup-gate-v1.yml` | Environment Setup Gate v1 | `push`, `workflow_dispatch` | Cross-platform dependency/bootstrap setup gate. | Blocking candidate |
| `.github/workflows/friend-mesh-prefab-gate.yml` | Friend mesh prefab gate | `push`, `pull_request`, `workflow_dispatch` | Friend mesh prefab validation. | Blocking candidate |
| `.github/workflows/full-platform-readiness-gate.yml` | Full Platform Readiness Gate | `push`, `pull_request`, `schedule`, `workflow_dispatch` | Broad platform readiness aggregation across multiple domains. | Blocking candidate |
| `.github/workflows/grpc-transport-gate.yml` | gRPC transport gate | `push`, `pull_request`, `workflow_dispatch` | gRPC transport build/test validation. | Blocking candidate |
| `.github/workflows/installer-bruteforce-gate.yml` | Installer Bruteforce Gate | `workflow_dispatch` | Manual installer robustness matrix. | Advisory/manual |
| `.github/workflows/kernel-coverage-gate.yml` | Kernel coverage gate | `push`, `pull_request` | Kernel coverage ratchet. | Blocking candidate |
| `.github/workflows/kernel-gate.yml` | Kernel Gate | `push`, `pull_request`, `workflow_dispatch` | Kernel tier build/test gate with manual tier expansion. | Blocking candidate |
| `.github/workflows/layer-boundary.yml` | layer-boundary | `pull_request` | Enforces tier/layer boundary rules. | Blocking candidate |
| `.github/workflows/dependency-boundary.yml` | dependency-boundary | `pull_request`, `push`, `workflow_dispatch` | Open-core vs commercial project-reference and licensing boundary scan. | Blocking candidate |
| `.github/workflows/mapbox-tile-helpers-ci.yml` | Mapbox Tile Helpers (optional) | `workflow_dispatch` | Optional Mapbox helper validation. | Advisory/manual |
| `.github/workflows/mesh-lab-gate.yml` | Mesh virtual lab gate | `push`, `pull_request`, `workflow_dispatch` | Virtual mesh lab validation. | Blocking candidate |
| `.github/workflows/mesh-lab-remote-gate.yml` | Mesh lab remote gate (self-hosted / tailnet) | `workflow_dispatch` | Manual self-hosted/tailnet mesh validation. | Advisory/manual |
| `.github/workflows/mesh-lab-stress-gate.yml` | Mesh lab stress gate | `schedule`, `workflow_dispatch` | Scheduled/manual mesh stress validation. | Advisory/manual |
| `.github/workflows/mesh-lab-tls-gate.yml` | Mesh lab TLS gate | `schedule`, `workflow_dispatch` | Scheduled/manual TLS mesh validation. | Advisory/manual |
| `.github/workflows/nuget-consumer-verify.yml` | NuGet consumer verify | `workflow_dispatch` | Manual NuGet consumer verification. | Advisory/manual |
| `.github/workflows/onboarding-docs-guard.yml` | Onboarding Docs Guard | `push`, `pull_request`, `workflow_dispatch` | Guards startup docs and required quick-start command text. | Blocking candidate |
| `.github/workflows/onboarding-quickstart-gate.yml` | onboarding-quickstart-gate | `push`, `pull_request`, `schedule`, `workflow_dispatch` | Runs documented first-run native/container onboarding commands. | Blocking candidate |
| `.github/workflows/ops-gate.yml` | Ops Gate | `workflow_dispatch` | Manual operations readiness gate. | Advisory/manual |
| `.github/workflows/optimize-agent-cluster-gate.yml` | Optimize Agent Cluster Gate | `push`, `pull_request`, `workflow_dispatch` | Runtime Studio/agent-cluster optimization validation. | Blocking candidate |
| `.github/workflows/pack-hosting-graph-alignment.yml` | Pack hosting graph alignment | `push`, `pull_request`, `workflow_dispatch` | Ensures pack allowlist matches `Nexo.Hosting` MSBuild graph. | Blocking candidate |
| `.github/workflows/perf-certification.yml` | perf-certification | `workflow_dispatch` | Manual performance certification. | Advisory/manual |
| `.github/workflows/perf-gate.yml` | perf-gate | `push`, `workflow_dispatch` | Performance gate signal. | Blocking candidate |
| `.github/workflows/prod-dry-run-pr.yml` | Prod dry run (Compose) | `pull_request`, `workflow_dispatch` | Production-shaped compose dry run on PRs. | Blocking candidate |
| `.github/workflows/production-readiness-gate-v1.yml` | Production Readiness Gate v1 | `push`, `pull_request`, `workflow_dispatch` | Production readiness gate procedure. | Blocking candidate |
| `.github/workflows/rc-gate.yml` | RC Gate | `push`, `schedule`, `workflow_dispatch` | Release-candidate readiness signal. | Release gate |
| `.github/workflows/release-nuget.yml` | Release NuGet packages | `workflow_dispatch` | Manual NuGet-only release path. | Release gate |
| `.github/workflows/release.yml` | Release | `push` tags, `workflow_dispatch` | Tag/manual release for GHCR and NuGet. | Release gate |
| `.github/workflows/reusable-container-publish.yml` | Reusable container publish | `workflow_call` | Reusable container publish job invoked by release/publish workflows. | Release gate |
| `.github/workflows/reusable-release-nuget.yml` | Reusable release NuGet | `workflow_call` | Reusable NuGet release job invoked by release workflows. | Release gate |
| `.github/workflows/reusable-verify-nuget-consumer.yml` | Reusable verify NuGet consumer | `workflow_call` | Reusable post-publish/consumer verification job. | Release gate |
| `.github/workflows/runtime-release-gate.yml` | Runtime Release Gate | `push`, `workflow_dispatch` | Runtime release quality gate. | Release gate |
| `.github/workflows/runtime-release-promotion.yml` | Runtime Release Promotion | `workflow_dispatch` | Manual runtime release promotion. | Release gate |
| `.github/workflows/runtime-studio-forge-smoke.yml` | Runtime Studio forge smoke | `pull_request`, `workflow_dispatch` | Runtime Studio forge smoke validation. | Blocking candidate |
| `.github/workflows/runtime-studio-playground.yml` | Runtime Studio Playground | `workflow_dispatch` | Manual playground workflow for Runtime Studio experiments. | Advisory/manual |
| `.github/workflows/security-gate.yml` | Security Gate | `pull_request`, `workflow_dispatch` | Security/trust gate with manual tier expansion. | Blocking candidate |
| `.github/workflows/setup-smoke-suite.yml` | Setup Smoke Suite | `workflow_dispatch` | Manual setup smoke suite for devcontainer/compose/native lanes. | Advisory/manual |
| `.github/workflows/ship-gate.yml` | Ship Gate | `workflow_dispatch` | Manual ship-readiness gate. | Advisory/manual |
| `.github/workflows/test-air-gapped-no-network.yml` | Air-Gapped Multi-Env Tests | `workflow_dispatch` | Manual zero-network air-gapped test validation. | Advisory/manual |
| `.github/workflows/test-caching-multi-env.yml` | Test Caching Multi-Environment | `push`, `pull_request`, `workflow_dispatch` | Multi-environment caching tests. | Blocking candidate |
| `.github/workflows/test-persistence-multi-os.yml` | Persistence Tests (Multi-OS) | `push`, `pull_request`, `workflow_dispatch` | Persistence tests across operating systems. | Blocking candidate |
| `.github/workflows/test-trust-multi-env.yml` | Trust Tests (Multi-Env Docker) | `workflow_dispatch` | Manual trust tests across Docker environments. | Advisory/manual |
| `.github/workflows/testing-strategy-gate.yml` | Testing strategy gate | `pull_request` | Enforces testing-strategy PR rules. | Blocking candidate |
| `.github/workflows/waterproofing-gate.yml` | waterproofing-gate | `workflow_dispatch` | Manual waterproofing/hardening signal. | Advisory/manual |
| `.github/workflows/workflow-regression-gate.yml` | Workflow Regression Gate | `pull_request`, `workflow_dispatch` | Detects workflow regression risk. | Blocking candidate |

## Consolidation candidates (recommendation only)

No workflow is changed by this sprint. These are recommendations for an owner-led follow-up:

1. **Onboarding and setup gates:** consider whether `onboarding-quickstart-gate.yml`, `onboarding-docs-guard.yml`, `devcontainer-gate.yml`, `setup-smoke-suite.yml`, `environment-setup-gate-v1.yml`, `container-image-gate.yml`, and `installer-bruteforce-gate.yml` should share a single required status plus manual deep lanes.
2. **Readiness gates:** consider consolidating overlapping required/advisory boundaries among `kernel-gate.yml`, `application-gate.yml`, `security-gate.yml`, `ops-gate.yml`, `ship-gate.yml`, `rc-gate.yml`, `production-readiness-gate-v1.yml`, and `full-platform-readiness-gate.yml`.
3. **Mesh gates:** consider a single mesh gate with selectable tiers for virtual lab, friend mesh, TLS, stress, and self-hosted/tailnet remote validation.
4. **Coverage and testing policy:** consider aligning `kernel-coverage-gate.yml`, `core-domain-coverage.yml`, `testing-strategy-gate.yml`, `waterproofing-gate.yml`, and perf gates under one branch-protection policy.
5. **Release workflows:** keep release/publish workflows separate from PR branch protection unless the owner explicitly wants release dry-run gates on every PR.

The owner should decide which checks are required branch-protection gates, which are advisory, and which should be manual-only before any workflow changes are made.
