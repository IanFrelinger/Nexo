# .NET SDK and target frameworks

## SDK version (`global.json`)

The repository pins the **.NET SDK** to **10.x** (see `global.json` with `rollForward: latestFeature`). Developer images and CI use **`mcr.microsoft.com/dotnet/sdk:10.0`** so everyone builds with the same MSBuild and C# language version. .NET 10 is the current **LTS**; .NET 9 (STS) left support in May 2026.

## Target frameworks

Hosts and shipped executables (`Ashlar.CLI`, `Ashlar.Mcp.Server.Host`, `Ashlar.Transport.Grpc.Server.Host`, the tools under `tools/`) target **`net10.0`**. `Ashlar.API` ships on `net10.0` too (its Dockerfiles publish `-f net10.0`) but multi-targets `net8.0;net10.0` because it is also consumed as a library by `net8.0` projects.

Libraries multi-target **`net8.0;net10.0`** so the **`net8.0` consumer story stays alive until .NET 8 leaves support (November 2026)** while the shipped artifacts are `net10.0`. The `netstandard2.0` contract assemblies (`Ashlar.Abstractions`, `Ashlar.Core.Domain`, `Ashlar.Brick.Contracts`, …) target **`netstandard2.0;net8.0;net10.0`**; `Ashlar.Brick.Contracts` keeps `net8.0` because generated bricks consume it.

Executables and test hosts set `RollForward=Major` (root `Directory.Build.targets`), so the remaining `net8.0` test hosts and samples also run on the 10.x shared runtime that ships with the pinned SDK; an SDK-10-only machine needs no separate .NET 8 runtime.

Test projects multi-target **`net8.0` and `net10.0`** (for example `Ashlar.Tests.Infrastructure`) so CI can exercise both runtimes where workflows pass `-f net8.0` / `-f net10.0`. The in-process `Ashlar.API` tests (`Tests/VirtualProduction`) compile only for `net10.0` (they need the 10.0 `Microsoft.AspNetCore.Mvc.Testing` TestHost). `Ashlar.Tests.CLI` is `net10.0` only, like the CLI it tests.

**Rule of thumb:** use the SDK from `global.json` to build; ship or run on `net10.0`; keep `net8.0` in a library's `TargetFrameworks` unless a package dependency cannot resolve for it (then document the exception in the project file).
