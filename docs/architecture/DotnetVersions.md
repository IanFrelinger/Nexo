# .NET SDK and target frameworks

## SDK version (`global.json`)

The repository pins the **.NET SDK** to **9.x** (see `global.json` with `rollForward: latestMinor`). Developer images and CI often use **`mcr.microsoft.com/dotnet/sdk:9.0`** so everyone builds with the same MSBuild and C# language version.

## Target frameworks

Many libraries and executables target **`net8.0`** (current LTS for shipped artifacts). That is intentional: runtime and deployment baselines stay on LTS while the **toolchain** stays current.

Some test projects multi-target **`net8.0` and `net9.0`** (for example `Nexo.Tests.Infrastructure`) so CI can exercise both runtimes where workflows pass `-f net9.0`.

**Rule of thumb:** use the SDK from `global.json` to build; ship or run on `net8.0` unless a specific project or workflow documents a different TFM.
