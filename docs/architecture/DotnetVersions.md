# .NET SDK and target frameworks

## SDK version (`global.json`)

The repository pins the **.NET SDK** to **8.x** (see `global.json` with `rollForward: latestFeature`). Developer images and CI use **`mcr.microsoft.com/dotnet/sdk:8.0`** so everyone builds with the same LTS toolchain and target baseline.

## Target frameworks

Libraries, executables, and test projects target **`net8.0`** as the current LTS for shipped artifacts and local development.

Some compatibility libraries continue to multi-target **`netstandard2.0` and `net8.0`** where published packages need older consumer/runtime reach (for example Unity-oriented and runtime-core surfaces). Do not add new target frameworks without documenting the package compatibility need.

**Rule of thumb:** use the SDK from `global.json` to build; ship, test, and run on `net8.0` unless a compatibility package explicitly documents a retained `netstandard2.0` target.
