# Testing strategy pivot — tracking v1

Living checklist for [Testing strategy pivot v1](TestingStrategyPivot-v1.md). Update checkboxes when items complete.

**Reviewers:** [Testing review guide v1](TestingReviewGuide-v1.md)

---

## Phase 0 — Documentation

- [x] `TestingStrategyPivot-v1.md` linked from `docs/Testing.md`
- [x] Linked from `docs/architecture/TestingModel.md`
- [x] Linked from `docs/production-readiness/CoverageGates-v1.md`
- [x] Linked from `docs/production-readiness/README.md`
- [x] PR template includes blast-radius testing table
- [x] CONTRIBUTING.md references pivot
- [x] [Testing review guide v1](TestingReviewGuide-v1.md)

## Phase 1 — Coverage policy

- [x] `kernel-coverage-gate.yml` + `scripts/ci/kernel-coverage-gate.sh`
- [x] `core-domain-coverage` at 100% threshold
- [x] `CoverageGates-v1.md` exclusions documented
- [x] Branch protection documented (`docs/GitHubBranchProtection.md`) — enable in GitHub UI
- [x] Doc audit: no spurious “global 100% line” goal (domain-only 100% is explicit)

## Phase 2 — ProdStyle-first

- [x] CONTRIBUTING.md rule for new kernel features
- [x] Review guide: reject gap-only megaclass PRs
- [x] `testing-strategy-gate.yml` enforces ProdStyle test delta or `[skip-prod-style]`
- [x] `make test-prod-style` documented in kernel-gate-tier-c (existing) + pivot doc

## Phase 3 — CI path map

- [x] Path → workflow table (verified 2026-05-28)
- [x] `testing-strategy-gate.yml` on PRs touching `src/` / `application/`
- [x] Required checks documented in [Testing and quality gates](../production-readiness/TestingAndQualityGates.md)

### Path → minimum CI

| Paths | PR workflows / local commands |
|-------|------------------------------|
| `src/Nexo.Core.Domain/**` | `domain-coverage`, `kernel-coverage` · `dotnet test src/Nexo.Tests.Domain` |
| `src/Nexo.Core.Application/**` | `kernel-coverage` · `Nexo.Tests.Application` |
| `src/Nexo.Infrastructure/**`, `src/Nexo.Runtime/**`, `src/Nexo.Hosting/**` | `kernel-coverage`, `kernel-gate`, `cross-platform-tests` (path-filtered) |
| Production wiring (routing, barriers, API host) | `testing-strategy` · `make test-prod-style` · `kernel-gate-tier-c` |
| `application/src/Nexo.API/**`, `application/src/Nexo.CLI/**` | `application-gate`, `testing-strategy` |
| `src/**/Mesh/**`, `src/**/Fleet/**`, `deploy/compose/docker-compose.mesh*` | `composition-mesh-gate`, `mesh-lab-gate` |
| `src/Nexo.Ingress.*`, middleware ingress | `cross-platform-tests`, trust workflows |
| `commercial/src/Nexo.Commercial.GameDirector.*`, `commercial/tests/Nexo.Commercial.Tests.GameDirector` | `application-gate`, relevant app tests |
| Trust / security / barriers policy | `security-gate`, `test-trust-multi-env` |
| Distribution / CLI packaging | `distribution-matrix-gate`, `ship-gate` |
| `docs/**` only | `docs-link-check` |
| Release tag | `rc-gate`, `production-readiness-gate-v1`, `runtime-release-gate` (manual/scheduled) |

## Phase 4 — Gap hygiene

- [x] Freeze: `testing-strategy-gate` fails on new `*GapCoverageTests.cs` without `gap-coverage-justify:` in PR body
- [x] Megaclass allow list (below)
- [x] Review guide: prefer ProdStyle over gap megaclass edits
- [x] Redundant gap suite reduction (JWT/middleware/domain/barriers; ProdStyle dedup in Makefile; `WorkflowExecutorEdgeCaseTests`)
- [x] Dogfood Block 8 matrix tests skip on CI (`[NotOnCiFact]` in `Nexo.Tests.Infrastructure.Helpers`, reported as Skipped rather than a silent pass; nested `dotnet test` is flaky on runners)
- [ ] Optional backlog: PR Coverlet diff script (planned as `coverage-changed-files.sh` under `scripts/ci/`; not yet written)
- [ ] Quarterly ratchet: bump `INFRA_COVERAGE_THRESHOLD` / `APP_COVERAGE_THRESHOLD` when justified

### Megaclass allow list (extend in place only; prefer ProdStyle)

- `Execution/ProviderFactory.cs`
- `Testing/Docker/DockerService.cs`
- `Persistence/PostgresDatabaseProvisioner.cs`
- `Execution/BehaviorExecutor.cs`
- `Execution/ClusterExecutor.cs`
- `Execution/Routing/NexoPeerBrickExecutor.cs`
- `Knowledge/KnowledgeQueryService.cs`

## Phase 5 — RC linkage

- [x] RC checklist §1 mapped to workflows (below)
- [x] `rc-gate-full` in [RC readiness v1](../production-readiness/RCReadiness-v1.md)
- [x] Monthly `rc-gate` schedule on `master` (workflow_dispatch still available)

### Release candidate checklist → automation

| RC checklist item | Workflow / command |
|------------------|-------------------|
| Production readiness | `production-readiness-gate-v1.yml` · `make ship-gate-full` |
| Environment setup | `environment-setup-gate-v1.yml` |
| Runtime release | `runtime-release-gate.yml` · `dotnet run … release gate` |
| Runtime promotion | `runtime-release-promotion.yml` |
| Installer brute-force | `installer-bruteforce-gate.yml` |
| Container images | `container-image-gate.yml`, `container-image-publish.yml` |
| Onboarding docs | `onboarding-docs-guard.yml` |
| Cross-platform | `cross-platform-tests.yml`, `full-platform-readiness-gate.yml` |
| Local RC stack | `make rc-gate-full` · `NEXO_READY_SKIP_DOCKER=1 make nexo-ready-gate` |
| Kernel coverage evidence | `make kernel-coverage-gate` |

---

## Coverage floor history

| Date | Domain | Infrastructure | Core.Application | Notes |
|------|--------|----------------|------------------|-------|
| 2026-05-28 | 100% | 83% | 67% | `kernel-coverage` floors after gap-suite reduction (CI ~83.5% infra line) |

---

## Manual follow-up (org settings)

- [ ] GitHub **branch protection** on `master`: require `testing-strategy`, `domain-coverage`, `kernel-coverage`
- [ ] Optional: label `needs-prod-style` for reviewer triage (manual label)
