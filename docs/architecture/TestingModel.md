# Testing model

Nexo uses **two complementary styles** of automated tests.

## xUnit (VSTest)

Projects such as `Nexo.Tests.Application` and `Nexo.Tests.Infrastructure` use **xUnit** directly. They are the primary targets for `dotnet test` in CI matrices (`FullyQualifiedName` filters, TRX loggers, etc.).

## Framework tests (`UnitTestBase` + `ITestRunner`)

Some suites inherit **`UnitTestBase`** (which extends **`TestBase`**) and implement **`ExecuteAsync`**. The infrastructure **`TestRunnerAdapter`** discovers those types by reflection and runs them; the **CLI** uses **`ITestRunner`** for the same pipeline.

**`UnitTestFrameworkBridge`** (in `Nexo.Infrastructure`) runs each concrete `UnitTestBase` through that runner. Test assemblies **`Nexo.Tests.Domain`**, **`Nexo.Tests.Application`**, **`Nexo.Tests.Infrastructure`**, and **`Nexo.Tests.CLI`** each expose **`UnitTestBridgeTests`** (xUnit theory) so `dotnet test` on those projects executes the same framework suites as the CLI. Infrastructure discovery skips **`SimpleTestForRunner`** (helper for **`TestRunnerAdapterTests`**), **`DependencyWrappingArchitectureTests`** (requires the **`copy-assemblies`** layout; run it via the full **`Nexo.Tests.Infrastructure`** test flow or filtered `dotnet test` after that target), and **`DoctorCommandTests`** (expects repo layout / host health; run via **`dotnet test`** with filter **`FullyQualifiedName~DoctorCommandTests`** or the CLI integration lane).

## Production-like tests (`Category=ProdStyle`)

Use **`[Trait("Category", "ProdStyle")]`** on xUnit classes that mirror production wiring: **`AddNexo`**, **`AddRunPodCapabilityRouting`**, adaptation/composition stacks, Forge HTTP surfaces, capability routing, barriers, etc. **`UnitTestBridgeTests`** in Application / Domain / Infrastructure / CLI is tagged so every **`UnitTestBase`** suite participates. **`Nexo.Tests.GameDomain`** and **`Nexo.Tests.Transport`** use **`[assembly: AssemblyTrait("Category", "ProdStyle")]`** so the full assembly runs under **`Category=ProdStyle`**.

**Virtual API stack (`FrameworkVirtualProdDemosTests`):** **`WebApplicationFactory&lt;Program&gt;`** spins up **`Nexo.API`** in-process (environment **`Testing`** → **`appsettings.Testing.json`**, background-agent **`IHostedService`** off). Requests hit the **same** minimal API endpoints as production — no fake route handlers. This matches **`docs/demos/`** (`GET /api/status`, **`NexoClient`**). These sources compile **only for `net8.0`** in **`Nexo.Tests.Infrastructure`** (`Nexo.API` is net8; ASP.NET Core TestHost + **`WriteAsJsonAsync`** on **`net9.0`** test TFMs can hit known **`PipeWriter`** incompatibilities).

Run **before** lighter smoke (`BaseFrameworkSmokeTests`) and full matrices — see **`make test-prod-style`**, **`make test-prime-time`** (**`Nexo.PrimeTime.slnf`**), **`make test-framework-prod-first`**, and **`nexo ci verify`** (ProdStyle runs after Infrastructure build, before smoke).

## Merge policy (GitHub)

To block merges when domain line coverage regresses, in GitHub go to **Settings → Branches → Branch protection rule**, enable **Require status checks to pass**, and add **`domain-coverage`** (the job id from **Core domain coverage**). If the UI only shows the workflow name, pick the check that corresponds to that workflow’s latest green run.

## Local commands

- **Framework suites via xUnit:**  
  `dotnet test src/Nexo.Tests.Domain/Nexo.Tests.Domain.csproj`  
  `dotnet test src/Nexo.Tests.Application/Nexo.Tests.Application.csproj`  
  `dotnet test src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj`  
  `dotnet test src/Nexo.Tests.CLI/Nexo.Tests.CLI.csproj`
- **Broader local bar:** `make test` (see `Makefile`; uses blame-hang options)
- **Production-like integration first (Infrastructure only):** `make test-prod-style` then optionally `make test-framework-prod-first`
- **Prime-time gate (all test projects in `Nexo.PrimeTime.slnf`):** `make test-prime-time` — **`Category=ProdStyle`** across Application, Domain, Infrastructure, CLI, Orchestration, BackgroundAgents, GameDomain, Transport; then **`make test-prime-time-full`** for the full slice.
- **CI-style verification:** `make ci-verify` or `dotnet run --project src/Nexo.CLI -- ci verify`
