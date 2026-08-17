# Testing and quality gates

## Goals

- Default branch merges only when **agreed checks** pass.
- Test layout matches how operators deploy (containers, optional air-gapped).

**Strategy:** layered proof model in [Testing strategy pivot v1](../architecture/TestingStrategyPivot-v1.md) (domain 100%, coverage ratchets, ProdStyle-first, mesh/RC for environment). Track execution in [Testing strategy tracking v1](../architecture/TestingStrategyTracking-v1.md).

## Checklist

### Kernel gate (pre-application)

- [ ] `make kernel-gate-full` passes locally before major application work on `application/src/`.
- [ ] [Kernel Readiness v1](KernelReadiness-v1.md) updated after gate runs.
- [ ] `make kernel-gate-tier-d` or release preflight before publishing NuGet packages.

### Required status checks

- [ ] Branch protection on default branch requires: **`testing-strategy`**, **`kernel-coverage`**, plus path-filtered gates (see `docs/GitHubBranchProtection.md`).
- [x] Check names documented for contributors — `docs/architecture/TestingModel.md`, `docs/GitHubBranchProtection.md`.

### RC workflows (before tag; not every PR)

- [ ] `production-readiness-gate-v1` · `environment-setup-gate-v1` · `runtime-release-gate` · `container-image-gate`
- [ ] Local: `make rc-gate-full` — map in [Testing strategy tracking § RC](../architecture/TestingStrategyTracking-v1.md#release-candidate-checklist--automation)

### Test types

- [ ] Unit and integration tests run in CI on every PR touching relevant paths.
- [ ] Smoke tests after deploy to staging (automated or manual checklist).

### Coverage and quality

- [x] Line coverage thresholds on critical kernel assemblies — see [Coverage gates v1](CoverageGates-v1.md) (`kernel-coverage` composite gate: Domain 100% + Infrastructure/Core.Application floors).
- [ ] Branch coverage thresholds (optional ratchet; line floors enforced in CI today).
- [ ] Static analysis or security scan in CI for appropriate components.

### Performance

- [ ] Optional: perf budget tests on startup or hot API paths; fail CI on regression beyond threshold.

### Special layouts

- [ ] Tests that need copied assemblies, GPU, or cloud credentials are isolated in named jobs with clear docs so they are not skipped silently.

### Package publish (NuGet)

- [ ] **NuGet deploy smoke:** after each publish, run `verify-stable-sdk-host-sample-published-feed` for the released version (manual, scheduled workflow, or post-release job). See `docs/NuGetConsumerVerify.md`.

## Repo pointers

- `CONTRIBUTING.md` — local commands and resource safety.
- `docs/architecture/TestingModel.md` — xUnit vs `UnitTestBase` bridge.
