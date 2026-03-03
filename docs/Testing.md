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
