# Portable Multi-Platform Testing

This document describes the portable testing infrastructure that eliminates command-line dependencies for maximum portability.

## Overview

The Nexo framework uses a **fully portable testing approach** that:
- **No command-line dependencies**: All Docker operations use Docker.DotNet API
- **No shell scripts**: All test execution logic is embedded in C#
- **Cross-platform**: Works on Windows, Linux, and macOS
- **Self-contained**: Everything runs through .NET without external tools

## Architecture

### Docker Service Layer

**`IDockerService`** (`src/Nexo.Infrastructure/Testing/Docker/IDockerService.cs`)
- Portable interface for Docker operations
- No command-line dependencies
- Uses Docker.DotNet library for API access

**`DockerService`** (`src/Nexo.Infrastructure/Testing/Docker/DockerService.cs`)
- Implementation using Docker.DotNet
- Handles image building, container execution, log retrieval
- Automatic container cleanup
- Cross-platform Docker socket detection (Windows named pipe, Linux/macOS Unix socket)

### Test Execution Layer

**`MultiPlatformTestBase`** (`src/Nexo.Tests.Infrastructure/Tests/MultiPlatform/MultiPlatformTestBase.cs`)
- Base class for all multi-platform tests
- Uses `IDockerService` instead of `Process.Start`
- Embedded test commands in C# (no shell scripts)
- Automatic Docker availability checking

### Platform Test Implementations

All platform tests extend `MultiPlatformTestBase`:
- **Ubuntu80Test** / **Ubuntu70Test** - Ubuntu 22.04
- **Alpine80Test** - Alpine Linux
- **Debian80Test** - Debian 12
- **Android80Test** - Android
- **Windows80Test** - Windows Server
- **UnityInfrastructureTest** - Unity (GameCI Docker images)

## Key Benefits

### 1. Portability
- **No shell scripts**: All logic in C#
- **No command-line tools**: Uses Docker API directly
- **Cross-platform**: Works identically on Windows, Linux, macOS

### 2. Maintainability
- **Single codebase**: All test logic in .NET
- **Type-safe**: Compile-time checking
- **Debuggable**: Can debug Docker operations in C#

### 3. Consistency
- **Unified interface**: All platforms use same `IDockerService`
- **Predictable behavior**: No shell differences between platforms
- **Error handling**: Consistent exception handling

## Usage

### Running Tests

All multi-platform tests are discoverable through the standard test runner:

```bash
# Run all tests (includes multi-platform)
dotnet run --project src/Nexo.CLI -- test

# Run only multi-platform tests
dotnet run --project src/Nexo.CLI -- test --filter MultiPlatform

# Run specific platform
dotnet run --project src/Nexo.CLI -- test --filter Ubuntu
dotnet run --project src/Nexo.CLI -- test --filter Unity
```

### Docker Requirements

The framework requires:
- **Docker Engine** running (Docker Desktop on Windows/macOS, Docker daemon on Linux)
- **Docker API access** (automatic detection based on platform)
- **No Docker CLI** required (uses API directly)

## Implementation Details

### Docker Image Building

```csharp
var buildResult = await _dockerService.BuildImageAsync(
    dockerfilePath,
    imageTag,
    contextPath,
    buildArgs,
    progress,
    cancellationToken);
```

- Creates tar.gz archive of build context
- Uses Docker API for building
- Progress reporting support

### Container Execution

```csharp
var runResult = await _dockerService.RunContainerAsync(
    imageTag,
    command,
    environmentVariables,
    volumeMounts,
    progress,
    cancellationToken);
```

- Creates and starts container
- Waits for completion
- Retrieves logs via MultiplexedStream
- Automatic cleanup

### Test Command Building

Test commands are built in C#:

```csharp
protected virtual string[] BuildTestCommand(bool isWindows)
{
    return isWindows
        ? new[] { "cmd", "/c", "dotnet test ..." }
        : new[] { "bash", "-c", "dotnet test ..." };
}
```

No shell scripts required - all logic embedded in C#.

## Migration from Command-Line

### Before (Command-Line)
```csharp
var process = Process.Start(new ProcessStartInfo
{
    FileName = "docker",
    Arguments = "build -f Dockerfile -t image ."
});
```

### After (Docker API)
```csharp
var result = await _dockerService.BuildImageAsync(
    "Dockerfile",
    "image",
    ".",
    buildArgs);
```

## Platform Support

### Docker-Based Platforms (Fully Portable)

These platforms use Docker containers and work anywhere Docker is available:
- ✅ **Ubuntu** - Docker container
- ✅ **Alpine** - Docker container
- ✅ **Debian** - Docker container
- ✅ **Android** - Docker container (with Android SDK)
- ✅ **Windows** - Docker container (Windows containers)
- ✅ **Unity** - Docker container (GameCI Unity images)

### Native Execution Platforms (Limited Portability)

These platforms require native execution due to technical limitations:
- ⚠️ **iOS** - Native macOS execution (iOS devices/simulators don't support Docker)

**iOS Limitations:**
- iOS devices and simulators cannot run Docker
- Requires macOS with Xcode installed
- Uses native .NET execution instead of Docker API
- Still integrated with test runner infrastructure
- Automatically skips on non-macOS systems

## Future Enhancements

Potential improvements:
- **Parallel execution**: Run multiple platform tests simultaneously
- **Caching**: Cache Docker images between test runs
- **Progress UI**: Real-time progress display for long-running tests
- **Test result aggregation**: Combine results from multiple platforms

## Mobile Environment Support

### Android ✅

**Fully Supported via Docker**
- Uses Docker container with Android SDK
- Works on any system with Docker
- No special requirements beyond Docker Engine
- Fully portable (no command-line dependencies)

### iOS ⚠️

**Hybrid Approach - Native Execution Required**

**Container Options for iOS:**
After research, there are **no container solutions** that work on iOS:
- ❌ **Docker**: Not supported on iOS devices or simulators
- ❌ **Podman**: macOS only (not iOS devices)
- ❌ **Apple Container**: macOS only (not iOS devices)
- ❌ **LXC/LXD**: Not available on iOS

**Why iOS Can't Use Containers:**
1. iOS devices are locked down and don't support containerization
2. iOS simulators run in macOS but don't expose container APIs
3. iOS sandboxing prevents container runtime installation
4. Apple's security model doesn't allow containerization on iOS

**Hybrid Implementation:**
- `Ios80Test` extends `MultiPlatformTestBase` for consistency
- Uses `PlatformCapabilityDetector` to determine execution mode
- Automatically uses native execution (Docker not available)
- Detects macOS and Xcode availability
- Gracefully skips on non-macOS systems
- Fully integrated with test runner infrastructure

**Execution Flow:**
```csharp
// iOS automatically detects it needs native execution
var mode = PlatformCapabilityDetector.GetExecutionMode("ios-8.0");
// Returns: ExecutionMode.Native

// Test uses ExecuteTestAsync which routes to native executor
return await ExecuteTestAsync(
    dockerfile: null,  // iOS doesn't support Docker
    nativeExecutor: ExecuteNativeIosTestAsync,
    cancellationToken);
```

**Future Options for True iOS Device Testing:**
- Cloud testing services (Appium Cloud, BrowserStack, Sauce Labs)
- CI/CD with macOS runners (GitHub Actions, Azure Pipelines)
- Xcode Cloud for native iOS testing
- Physical device testing via USB (requires manual setup)

## Troubleshooting

### Docker Not Available

If Docker is not running:
```
Docker is not available for {platform}
```

**Solution**: Start Docker Desktop or Docker daemon

### iOS Testing Limitations

If iOS tests fail or are skipped:
```
iOS testing requires macOS (cannot run in Docker)
```

**Solution**: 
- Run on macOS system
- Install Xcode from App Store
- iOS tests will run natively (not in Docker)

### Build Failures

Check Docker logs for build errors. The framework will report:
```
Docker build failed for {platform}
```

**Solution**: Check Dockerfile syntax and dependencies

### Container Execution Errors

Container execution errors are captured in `DockerRunResult.StandardError`:
```
Container finished: {containerId}, ExitCode: {code}
```

**Solution**: Review container logs and test command syntax

## Summary

The portable testing infrastructure provides:
- ✅ **Zero command-line dependencies**
- ✅ **Fully embedded in C#**
- ✅ **Cross-platform compatibility**
- ✅ **Type-safe and debuggable**
- ✅ **Consistent behavior across platforms**

All multi-platform tests can now run anywhere Docker is available, without requiring shell scripts, command-line tools, or platform-specific configurations.
