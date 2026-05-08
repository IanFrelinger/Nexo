# Testing model

Nexo uses **two complementary styles** of automated tests.

## xUnit (VSTest)

Projects such as `Nexo.Tests.Application` and `Nexo.Tests.Infrastructure` use **xUnit** directly. They are the primary targets for `dotnet test` in CI matrices (`FullyQualifiedName` filters, TRX loggers, etc.).

## Framework tests (`UnitTestBase` + `ITestRunner`)

Some suites inherit **`UnitTestBase`** (which extends **`TestBase`**) and implement **`ExecuteAsync`**. The infrastructure **`TestRunnerAdapter`** discovers those types by reflection and runs them; the **CLI** uses **`ITestRunner`** for the same pipeline.

**`UnitTestFrameworkBridge`** (in `Nexo.Infrastructure`) runs each concrete `UnitTestBase` through that runner. Test assemblies **`Nexo.Tests.Domain`**, **`Nexo.Tests.Application`**, **`Nexo.Tests.Infrastructure`**, and **`Nexo.Tests.CLI`** each expose **`UnitTestBridgeTests`** (xUnit theory) so `dotnet test` on those projects executes the same framework suites as the CLI. Infrastructure discovery skips **`SimpleTestForRunner`** (helper for **`TestRunnerAdapterTests`**), **`DependencyWrappingArchitectureTests`** (requires the **`copy-assemblies`** layout; run it via the full **`Nexo.Tests.Infrastructure`** test flow or filtered `dotnet test` after that target), and **`DoctorCommandTests`** (expects repo layout / host health; run via **`dotnet test`** with filter **`FullyQualifiedName~DoctorCommandTests`** or the CLI integration lane).

## Production-like tests (`Category=ProdStyle`)

Use **`[Trait("Category", "ProdStyle")]`** on xUnit classes that mirror production wiring: **`AddNexo`**, **`AddRunPodCapabilityRouting`**, adaptation/composition stacks, Forge HTTP surfaces, capability routing, barriers, etc. These are intended to run **before** lighter smoke (`BaseFrameworkSmokeTests`) and full matrices — see **`make test-prod-style`**, **`make test-framework-prod-first`**, and **`nexo ci verify`** (ProdStyle runs after Infrastructure build, before smoke).

## Merge policy (GitHub)

To block merges when domain line coverage regresses, in GitHub go to **Settings → Branches → Branch protection rule**, enable **Require status checks to pass**, and add **`domain-coverage`** (the job id from **Core domain coverage**). If the UI only shows the workflow name, pick the check that corresponds to that workflow’s latest green run.

## Local commands

- **Framework suites via xUnit:**  
  `dotnet test src/Nexo.Tests.Domain/Nexo.Tests.Domain.csproj`  
  `dotnet test src/Nexo.Tests.Application/Nexo.Tests.Application.csproj`  
  `dotnet test src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj`  
  `dotnet test src/Nexo.Tests.CLI/Nexo.Tests.CLI.csproj`
- **Broader local bar:** `make test` (see `Makefile`; uses blame-hang options)
- **Production-like integration first:** `make test-prod-style` then optionally `make test-framework-prod-first`
- **CI-style verification:** `make ci-verify` or `dotnet run --project src/Nexo.CLI -- ci verify`
