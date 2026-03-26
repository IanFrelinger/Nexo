# Nexo Testing Guide

## Test Guard Rails (Anti-Hang)

Nexo uses multiple mechanisms to prevent tests from hanging indefinitely and keep CI/dev loops responsive.

### Timeout Policy

| Scope | blame-hang-timeout | Per-test timeout | Notes |
|-------|--------------------|------------------|-------|
| smoke | 30s | — | BaseFrameworkSmokeTests; local `make test` |
| integration | 60s | 15s (Integration tests) | Category=Integration |
| persistence | 60s | — | InMemoryPersistenceTests |
| adaptation | 90s | — | Category=Adaptation (LiteDB, Roslyn, MSBuild) |
| e2e | 90s | — | Category=E2E |
| trust | 60s | — | Trust tests (Infrastructure + BackgroundAgents) |
| full | 120s | — | All Nexo.Tests.Infrastructure tests |

### Where Timeouts Are Applied

- **Makefile `make test`**: `--blame-hang-timeout 30s --blame-hang-dump-type none` (avoids 6GB+ hang dumps)
- **Makefile `test-all-platforms`**: Local 30s; Docker runs 60s
- **CI (cross-platform-tests.yml)**: Per-scope as above
- **TestCommandRunner**: 10 min global timeout for `nexo test local`
- **TestRunnerAdapter**: 60s per-test for TestBase-based tests
- **MultiPlatformTestCommand**: 5 min for native platform runs
- **WorkflowExecutorIntegrationTests**: `[Fact(Timeout = 15000)]` on each test

### Adding New Integration Tests

1. Add `[Collection("Integration")]` and `[Trait("Category", "Integration")]` to the test class
2. Add `[Fact(Timeout = 15000)]` (or appropriate value) to async/I/O tests
3. Integration tests run sequentially (DisableParallelization) to avoid file watcher and temp dir contention

## Running Tests

```bash
# Local (all tests, 30s blame-hang)
make test

# Or directly (add --blame-hang-dump-type none to avoid 6GB dumps on hangs)
dotnet test --blame-hang-timeout 30s --blame-hang-dump-type none

# nexo validate: high-signal check, but can fail in some host environments
# due to platform-specific test/runtime constraints.
# Prefer running after setup scripts and initial CLI smoke checks.
nexo validate

# nexo dogfood: add --verbose to stream build/test output
nexo dogfood block2 --verbose
nexo dogfood all --verbose

# Integration tests only
dotnet test src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj \
  --filter "Category=Integration" --blame-hang-timeout 60s --blame-hang-dump-type none

# Adaptation tests only (local)
dotnet test src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj \
  --filter "Category=Adaptation" --blame-hang-timeout 90s --blame-hang-dump-type none

# Adaptation tests across all Docker environments (linear)
make test-adaptation-all-envs
# Or: nexo test multi-env --suite adaptation --all

# Cross-platform (CI)
make test-cross-platform SCOPE=integration

# Adaptation scope in CI
gh workflow run "Cross-Platform Tests" -f scope=adaptation

# Dogfood (North Star gates)
make dogfood-all
```

## Safe Validation (Avoid Memory Explosion)

Running multiple validation commands in parallel (e.g. from Cursor's integrated terminal) can cause severe memory pressure and freeze the machine. Each `dotnet test` run buffers output and spawns test hosts; running 5+ in parallel can exceed 100GB RAM.

**Never run** `make ci-verify`, `nexo dogfood all`, and multiple `dotnet test` commands **in parallel** from Cursor's integrated terminal.

**Prefer:**
- Run validation in an external terminal (iTerm, Terminal.app) or CI
- If using Cursor, run **one command at a time** and wait for completion before starting the next

**Quick sanity check:** `make ci-verify` alone (sequential: build → smoke → validate) is the minimal gate. Run it first; if it passes, consider running `nexo dogfood all` in a separate terminal.

**Lightweight alternative:** `make validate-safe` or `bash scripts/validate-safe.sh` — equivalent to ci-verify but via shell script; use when ci-verify causes high memory usage. Run dogfood separately: `make dogfood-all`.

### Memory Mitigations (Built-in)

Test projects use `xunit.runner.json` to limit parallelism and reduce memory usage:

- **maxParallelThreads: 2** — limits concurrent test execution (default: CPU count)
- **parallelAlgorithm: conservative** — starts fewer tests at once, lower memory pressure
- **parallelizeAssembly: false** — prevents cross-assembly parallelism

To further reduce memory (e.g. on very constrained machines), set `parallelizeTestCollections: false` in `xunit.runner.json` to run all tests sequentially within each assembly.

## Test Artifacts & Cleanup

Tests can leave artifacts that consume disk space:

| Artifact | Size | Location |
|----------|------|----------|
| Hang dumps (`.dmp`) | ~6 GB each | `src/*/TestResults/*/` |
| Sequence XML | ~88 KB | `src/*/TestResults/*/` |
| TRX results (`.trx`) | ~20 KB each | `TestResults/`, `test-results/` |
| Coverage (`.coverage`) | varies | `TestResults/` |

**To prevent accumulation:** Local `make test` and CLI use `--blame-hang-dump-type none` so hang dumps are not written.

**To clean existing artifacts:**
```bash
# Via CLI (programmatic)
nexo maintenance clean --strategy test-artifacts

# Or via Makefile
make clean-test-artifacts
```
Removes all `TestResults/` dirs under `src/` and the root `test-results/` folder.

**Procedural cleanup (before/after test runs):** Set `NEXO_CLEAN_BEFORE_TEST=1` or `NEXO_CLEAN_AFTER_TEST=1` to run test-artifacts cleanup automatically when using `nexo test local`.
