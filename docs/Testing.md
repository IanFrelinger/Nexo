# Nexo Testing Guide

## Test Guard Rails (Anti-Hang)

Nexo uses multiple mechanisms to prevent tests from hanging indefinitely and keep CI/dev loops responsive.

### Timeout Policy

| Scope | blame-hang-timeout | Per-test timeout | Notes |
|-------|--------------------|------------------|-------|
| prime-time | 300s | varies | **`Nexo.PrimeTime.slnf`** — ProdStyle then full (`make test-prime-time` / `make test-prime-time-full`) |
| prod-style | 120s | varies | **Category=ProdStyle** — Infrastructure-only (`make test-prod-style`) |
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

1. Add `[Collection("Integration")]` and `[Trait("Category", "Integration")]` to the test class when appropriate.
2. For suites that exercise production DI graphs or **`Host`** wiring, also add **`[Trait("Category", "ProdStyle")]`** so they participate in **`make test-prod-style`** / **`nexo ci verify`** ordering.
3. Add `[Fact(Timeout = 15000)]` (or appropriate value) to async/I/O tests
4. Integration tests run sequentially (DisableParallelization) to avoid file watcher and temp dir contention

## Running Tests

**Prime-time (whole automated framework slice):**  

```bash
make test-prime-time          # Category=ProdStyle across Nexo.PrimeTime.slnf (nine test projects)
make test-prime-time-full    # ProdStyle gate then full test count on the same slice (ProdStyle runs twice)
```

**Faster Infrastructure-only ProdStyle:** `make test-prod-style`

`nexo` command note:
- Commands shown as `nexo ...` assume the CLI tool is installed on your PATH.
- If you have not installed the global tool, use the equivalent project invocation:
  - `dotnet run --project src/Nexo.CLI -- <subcommand>`
- Example:
  - `nexo validate`
  - `dotnet run --project src/Nexo.CLI -- validate`

```bash
# Local (all tests, 30s blame-hang)
make test

# Or directly (add --blame-hang-dump-type none to avoid 6GB dumps on hangs)
dotnet test --blame-hang-timeout 30s --blame-hang-dump-type none

# nexo validate: high-signal check, but can fail in some host environments
# due to platform-specific test/runtime constraints.
# Prefer running after setup scripts and initial CLI smoke checks.
nexo validate
# equivalent:
dotnet run --project src/Nexo.CLI -- validate

# nexo dogfood: add --verbose to stream build/test output
nexo dogfood block2 --verbose
nexo dogfood all --verbose
# equivalent:
dotnet run --project src/Nexo.CLI -- dogfood block2 --verbose
dotnet run --project src/Nexo.CLI -- dogfood all --verbose

# Integration tests only
dotnet test src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj \
  --filter "Category=Integration" --blame-hang-timeout 60s --blame-hang-dump-type none

# Adaptation tests only (local)
dotnet test src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj \
  --filter "Category=Adaptation" --blame-hang-timeout 90s --blame-hang-dump-type none

# Adaptation tests across all Docker environments (linear)
make test-adaptation-all-envs
# Or: nexo test multi-env --suite adaptation --all
# equivalent:
dotnet run --project src/Nexo.CLI -- test multi-env --suite adaptation --all

# Cross-platform (CI)
make test-cross-platform SCOPE=integration

# Adaptation scope in CI
gh workflow run "Cross-Platform Tests" -f scope=adaptation

# Dogfood (North Star gates)
make dogfood-all
```

## Execution Routing Smoke + Stress Tests

The routing stack (NCR local + peer network + RunPod cloud) has targeted smoke coverage in:

- `CapabilityRoutingBrickTests`
- `PeerToPeerRoutingSmokeTests`

Run them with:

```bash
# Peer smoke + stress scenarios (fallback, timeout, outage, burst concurrency)
dotnet test src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj \
  -f net8.0 --filter "FullyQualifiedName~PeerToPeerRoutingSmokeTests"

# Routing regression (peer smoke + core capability router)
dotnet test src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj \
  --filter "FullyQualifiedName~PeerToPeerRoutingSmokeTests|FullyQualifiedName~CapabilityRoutingBrickTests"
```

What these stress tests simulate:
- concurrent burst traffic to peers
- intermittent HTTP failures and socket resets
- latency spikes that trigger per-peer timeout and failover
- recovery with success-rate thresholds under degraded network conditions

Note: `src/Nexo.Tests.Infrastructure/scripts/copy-assemblies.cs` is lock-tolerant for transient file-copy races during test builds (retry/backoff + safe lock skips when outputs already exist), so transient assembly lock contention should not surface as `MSB3073` copy-script warnings.

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

## Full Platform Readiness Gate

The `full-platform-readiness-gate.yml` workflow runs setup → discovery → dry-run on all target platforms:

- **Native:** Ubuntu, macOS, Windows (setup scripts, CLI build, `nexo doctor`, `nexo bootstrap check`, `ci verify`, smoke tests)
- **Container:** Ubuntu, Alpine, Debian (Docker test images with CLI + test projects pre-built)
- **Docker CLI image:** build from `Dockerfile.cli` + smoke + discover
- **SDK sample:** `docs/samples/StableSdkHostSample/` built as compatibility smoke check

Trigger manually: `make test-readiness-gate` or `gh workflow run "Full Platform Readiness Gate"`.

Weekly schedule: Monday 06:00 UTC.

## Docker Test Images

Three Docker test images are maintained under `.docker/`:

| Image | Base | Purpose |
|-------|------|---------|
| `Dockerfile.test-caching` | Ubuntu + .NET 9 SDK | Framework, trust, and smoke tests |
| `Dockerfile.test-caching-alpine` | Alpine + .NET 9 SDK | Alpine-specific portability tests |
| `Dockerfile.test-caching-debian` | Debian + .NET 9 SDK | Debian-specific portability tests |

All three include the .NET 8 runtime (for CLI `net8.0` target) and pre-build test projects + CLI during image build.

## E2E Test Coverage

E2E tests are tagged with `[Trait("Category", "E2E")]` and must have explicit `[Fact(Timeout = N)]` (enforced by `TimeoutConventionTests`).

Key E2E test files:

| File | Tests | Coverage |
|------|-------|----------|
| `StrictModeE2ETests` | 8 | Strict mode flags, DI, config fail-fast |
| `NexoDefaultsTests` | 13 | Golden tests for all centralized constants |
| `ConfigurationAdapterEdgeCaseTests` | 8 | NEXO_CONFIG_PATH, malformed JSON, strict/permissive |
| `ProviderFactoryEdgeCaseTests` | 11 | Provider selection, mock gating, concurrency |
| `PipelineLifecycleE2ETests` | 9 | Pipeline lifecycle, resume, fan-in, concurrency |
| `HostingDeploymentProfileTests` | 10 | All deployment profiles, env var resolution |
| `BackgroundAgentLifecycleE2ETests` | 10 | Mode transitions, approval gates, audit bounds |
| `HostingE2ESmokeTests` | 8 | AddNexo kernel resolution, validation, observation |
