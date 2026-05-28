# Testing strategy pivot — tracking v1

Living checklist for [Testing strategy pivot v1](TestingStrategyPivot-v1.md). Update checkboxes when items complete.

---

## Phase 0 — Documentation

- [x] `TestingStrategyPivot-v1.md` linked from `docs/Testing.md`
- [x] Linked from `docs/architecture/TestingModel.md`
- [x] Linked from `docs/production-readiness/CoverageGates-v1.md`
- [x] Linked from `docs/production-readiness/README.md`
- [x] PR template includes blast-radius testing table
- [x] CONTRIBUTING.md references pivot (Phase 2.1 partial — ProdStyle rule added)

## Phase 1 — Coverage policy

- [x] `kernel-coverage-gate.yml` + `scripts/ci/kernel-coverage-gate.sh`
- [x] `core-domain-coverage` at 100% threshold
- [x] `CoverageGates-v1.md` exclusions documented
- [ ] GitHub branch protection: `domain-coverage` required
- [ ] GitHub branch protection: `kernel-coverage` required
- [ ] Doc audit: no “global 100% line” goal stated

## Phase 2 — ProdStyle-first

- [ ] CONTRIBUTING.md rule for new kernel features
- [ ] Review guide: reject gap-only megaclass PRs
- [ ] Path → `needs-prod-style` documented

## Phase 3 — CI path map

- [ ] Path → workflow table (below) reviewed quarterly

### Path → minimum CI (draft — verify against `.github/workflows/`)

| Paths | Workflows / commands |
|-------|----------------------|
| `src/Nexo.Core.Domain/**` | `domain-coverage`, `kernel-coverage` |
| `src/Nexo.Infrastructure/**`, `src/Nexo.Runtime/**` | `kernel-coverage`, `kernel-gate.yml` (if exists), ProdStyle on routing/hosting |
| `src/Nexo.Core.Application/**` | `kernel-coverage`, `Nexo.Tests.Application` |
| `application/src/Nexo.API/**` | `application-gate` paths, ProdStyle WAF |
| `**/Mesh/**`, `docker-compose*fleet*` | `mesh-lab-gate.yml` |
| `application/src/Nexo.CLI/**` | CLI tests, `application-gate-tier-a` |
| `docs/**` only | Docs link-check; skip full test matrix |

## Phase 4 — Gap hygiene

- [ ] Team agreement: freeze new `*GapCoverageTests` files
- [ ] Megaclass allow list agreed (see below)
- [ ] Optional: PR Coverlet diff script

### Megaclass allow list (extend in place only; prefer ProdStyle)

- `Execution/ProviderFactory.cs`
- `Testing/Docker/DockerService.cs`
- `Persistence/PostgresDatabaseProvisioner.cs`
- `Execution/BehaviorExecutor.cs`
- `Execution/ClusterExecutor.cs`
- `Execution/Routing/NexoPeerBrickExecutor.cs`
- `Knowledge/KnowledgeQueryService.cs`

## Phase 5 — RC linkage

- [ ] RC checklist §1 mapped to workflows
- [ ] `rc-gate-full` in RC readiness doc
- [ ] Monthly `rc-gate-tier-c` green on `master`

---

## Coverage floor history

| Date | Domain | Infrastructure | Core.Application | Notes |
|------|--------|----------------|------------------|-------|
| 2026-05-28 | 100% | 84% | 67% | Initial `kernel-coverage` floors (PR #130) |
