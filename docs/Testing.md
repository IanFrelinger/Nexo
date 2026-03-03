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

- **Makefile `make test`**: `--blame-hang-timeout 30s`
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

# Or directly
dotnet test --blame-hang-timeout 30s

# Integration tests only
dotnet test src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj \
  --filter "Category=Integration" --blame-hang-timeout 60s

# Adaptation tests only (local)
dotnet test src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj \
  --filter "Category=Adaptation" --blame-hang-timeout 90s

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
