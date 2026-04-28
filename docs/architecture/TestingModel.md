# Testing model

Nexo uses **two complementary styles** of automated tests.

## xUnit (VSTest)

Projects such as `Nexo.Tests.Application` and `Nexo.Tests.Infrastructure` use **xUnit** directly. They are the primary targets for `dotnet test` in CI matrices (`FullyQualifiedName` filters, TRX loggers, etc.).

## Framework tests (`UnitTestBase` + `ITestRunner`)

Some suites inherit **`UnitTestBase`** (which extends **`TestBase`**) and implement **`ExecuteAsync`**. The infrastructure **`TestRunnerAdapter`** discovers those types by reflection and runs them; the **CLI** uses **`ITestRunner`** for the same pipeline.

**`Nexo.Tests.Domain`** now also exposes an **xUnit bridge** (`UnitTestBridgeTests`) so `dotnet test src/Nexo.Tests.Domain` runs each concrete `UnitTestBase` through `ITestRunner`, matching CLI behavior while appearing as normal VSTest cases.

## Local commands

- **Domain framework tests via xUnit:** `dotnet test src/Nexo.Tests.Domain/Nexo.Tests.Domain.csproj`
- **Broader local bar:** `make test` (see `Makefile`; uses blame-hang options)
- **CI-style verification:** `make ci-verify` or `dotnet run --project src/Nexo.CLI -- ci verify`
