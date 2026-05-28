# Testing and quality gates

## Goals

- Default branch merges only when **agreed checks** pass.
- Test layout matches how operators deploy (containers, optional air-gapped).

## Checklist

### Kernel gate (pre-application)

- [ ] `make kernel-gate-full` passes locally before major application work on `application/src/`.
- [ ] [Kernel Readiness v1](KernelReadiness-v1.md) updated after gate runs.
- [ ] `make kernel-gate-tier-d` or release preflight before publishing NuGet packages.

### Required status checks

- [ ] Branch protection on default branch requires: build, primary test workflow, and any coverage or security gate you adopt.
- [ ] Check names documented for contributors (see `docs/architecture/TestingModel.md` if you use the `domain-coverage` workflow).

### Test types

- [ ] Unit and integration tests run in CI on every PR touching relevant paths.
- [ ] Smoke tests after deploy to staging (automated or manual checklist).

### Coverage and quality

- [x] Line coverage thresholds on critical kernel assemblies — see [Coverage gates v1](CoverageGates-v1.md) (`domain-coverage` 100%, `kernel-coverage` composite gate).
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
