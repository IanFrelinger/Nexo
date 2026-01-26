# Geospatial Testing Infrastructure Guide

## Overview

This guide explains how to leverage the portable testing infrastructure (assembly copying solution) in geospatial test projects to ensure tests run correctly across all target platforms.

## Problem

The .NET 8.0 test host has issues locating assemblies from the NuGet cache when those assemblies target older frameworks (net6.0, netstandard2.x, net7.0, netcoreapp3.1). This affects test projects that use:
- **Moq** (depends on Castle.Core targeting net6.0)
- **FluentAssertions** (targets net6.0)
- **Docker.DotNet** (targets netstandard2.1)
- **Microsoft.CodeAnalysis** (targets net7.0)
- And many other transitive dependencies

## Solution

The testing infrastructure automatically copies all required assemblies from the NuGet cache to the test output directory during build, ensuring the test host can find them.

## How to Apply to Geo Test Projects

### Step 1: Import the CopyAssemblies Targets

Add the following import to your test project's `.csproj` file:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <!-- ... your existing configuration ... -->
  
  <!-- Import the assembly copying targets from Tests.Infrastructure -->
  <Import Project="$(MSBuildThisFileDirectory)../Nexo.Tests.Infrastructure/CopyAssemblies.targets" />
  
  <!-- ... rest of your project ... -->
</Project>
```

### Step 2: Verify the Script Location

The `CopyAssemblies.targets` file references a C# script located at:
```
src/Nexo.Tests.Infrastructure/scripts/copy-assemblies.csproj
```

This script is automatically built and executed during your test project's build process.

### Step 3: Test It Works

Build your test project and verify assemblies are copied:

```bash
dotnet build src/Nexo.Tests.GeospatialUnit/Nexo.Tests.GeospatialUnit.csproj
```

You should see output like:
```
Copying assemblies to bin/Debug/net8.0/
Copied 75 assemblies, 0 failures
```

## Geo Test Projects

The following geo test projects should use this infrastructure:

### ✅ All Configured
- `Nexo.Tests.Infrastructure` - Base infrastructure (has the solution)
- `Nexo.Tests.GeospatialUnit` - Unit tests (uses Moq, FluentAssertions) ✅
- `Nexo.Tests.GeospatialE2E` - E2E tests (uses FluentAssertions) ✅
- `Nexo.Tests.GeoTerrain` - Domain tests ✅
- `Nexo.Tests.GeoVector` - Domain tests ✅
- `Nexo.Tests.GeoWorld` - Domain tests ✅

## Example: Adding to GeospatialUnit

Here's how to add it to `Nexo.Tests.GeospatialUnit.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <!-- Import assembly copying infrastructure -->
  <Import Project="$(MSBuildThisFileDirectory)../Nexo.Tests.Infrastructure/CopyAssemblies.targets" />

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio" />
    <PackageReference Include="FluentAssertions" />
    <PackageReference Include="Moq" />
    <!-- ... other packages ... -->
  </ItemGroup>

  <!-- ... rest of project ... -->
</Project>
```

## How It Works

1. **During Build**: The MSBuild target `CopyAllRequiredAssemblies` runs after the build completes
2. **Script Execution**: A C# console application (`copy-assemblies.cs`) parses the `deps.json` file
3. **Assembly Discovery**: The script identifies all runtime DLLs, especially those targeting older frameworks
4. **Automatic Copying**: Required assemblies are copied from the NuGet cache to the output directory
5. **Test Execution**: The test host can now find all assemblies in the output directory

## Benefits

- ✅ **Fully Portable**: Works on Windows, Linux, macOS, Docker, Unity, Android
- ✅ **No External Dependencies**: Uses only .NET SDK (no Python, no dotnet-script)
- ✅ **Automatic**: Runs during build, no manual steps required
- ✅ **Comprehensive**: Handles all problematic assemblies automatically
- ✅ **Type-Safe**: C# implementation is debuggable and maintainable

## Troubleshooting

### Tests Still Failing with "Assembly Not Found"

1. **Check the import path**: Ensure the relative path to `CopyAssemblies.targets` is correct
2. **Verify script exists**: Check that `src/Nexo.Tests.Infrastructure/scripts/copy-assemblies.csproj` exists
3. **Check build output**: Look for "Copying assemblies" message in build logs
4. **Verify deps.json**: Ensure `$(OutputPath)$(AssemblyName).deps.json` exists after build

### Script Not Running

The script runs automatically if:
- The `CopyAssemblies.targets` file is imported
- The `deps.json` file exists in the output directory
- The script project exists at the expected location

### Manual Execution

You can manually run the script to debug:

```bash
dotnet run --project src/Nexo.Tests.Infrastructure/scripts/copy-assemblies.csproj -- \
  src/Nexo.Tests.GeospatialUnit/bin/Debug/net8.0/Nexo.Tests.GeospatialUnit.deps.json \
  src/Nexo.Tests.GeospatialUnit/bin/Debug/net8.0/ \
  ~/.nuget/packages
```

## Multi-Environment Testing

This infrastructure works seamlessly with the multi-environment testing setup:

- **Docker containers**: All Dockerfiles use .NET 8.0 SDK, so the script runs automatically
- **Windows**: Path separators are handled automatically
- **Linux/macOS**: Unix paths work correctly
- **Unity**: Works with Unity-compatible .NET Standard 2.0 tests

## Related Documentation

- [Multi-Environment Testing Guide](./MULTI_ENV_TESTING.md)
- [Portable Testing Documentation](./PORTABLE_TESTING.md)
- [Geospatial Unit Tests](./GEOSPATIAL_UNIT_TESTS.md)
- [Geospatial E2E Tests](./GEOSPATIAL_E2E_TESTS.md)
