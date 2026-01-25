# Execution Platform Abstraction

## Overview

The Nexo testing infrastructure uses an abstracted execution platform system that allows users to plug in different container/orchestration platforms (Docker, Rancher, Kubernetes, etc.) without being tied to a specific implementation.

## Architecture

### Core Interface: `IExecutionPlatform`

All execution platforms implement `IExecutionPlatform`, which provides:

- **Platform Detection**: `IsAvailableAsync()` - Check if platform is ready
- **Image Building**: `BuildImageAsync()` - Build container images
- **Container Execution**: `RunContainerAsync()` - Run containers with commands
- **Resource Cleanup**: `RemoveContainerAsync()`, `RemoveImageAsync()`

### Implementations

#### 1. DockerExecutionPlatform (Default)

**Location**: `src/Nexo.Infrastructure/Testing/ExecutionPlatform/DockerExecutionPlatform.cs`

- Uses `Docker.DotNet` library for Docker API access
- Fully portable (no command-line dependencies)
- Works on Windows, Linux, and macOS
- Automatically detects Docker socket location

**Usage**:
```csharp
var logger = loggerFactory.CreateLogger<DockerExecutionPlatform>();
var platform = new DockerExecutionPlatform(logger);
```

#### 2. RancherExecutionPlatform (Placeholder)

**Location**: `src/Nexo.Infrastructure/Testing/ExecutionPlatform/RancherExecutionPlatform.cs`

- Placeholder implementation for Rancher container orchestration
- Users can extend this to integrate with Rancher API
- Supports Rancher endpoint configuration

**Usage**:
```csharp
var logger = loggerFactory.CreateLogger<RancherExecutionPlatform>();
var platform = new RancherExecutionPlatform(
    logger,
    rancherEndpoint: "https://rancher.example.com",
    accessKey: "your-access-key",
    secretKey: "your-secret-key");
```

#### 3. KubernetesExecutionPlatform (Placeholder)

**Location**: `src/Nexo.Infrastructure/Testing/ExecutionPlatform/KubernetesExecutionPlatform.cs`

- Placeholder implementation for Kubernetes
- Users can extend this to use Kubernetes Jobs/Pods
- Supports kubeconfig-based authentication

**Usage**:
```csharp
var logger = loggerFactory.CreateLogger<KubernetesExecutionPlatform>();
var platform = new KubernetesExecutionPlatform(
    logger,
    kubeconfigPath: "~/.kube/config",
    @namespace: "default");
```

## Integration with Multi-Platform Tests

### Automatic Platform Selection

`MultiPlatformTestBase` automatically uses the injected `IExecutionPlatform`:

```csharp
public class Ubuntu80Test : MultiPlatformTestBase
{
    public Ubuntu80Test() 
        : base("ubuntu-8.0", "8.0", "Ubuntu 22.04 (.NET 8.0)")
    {
        // Uses default DockerExecutionPlatform
    }
    
    // Or inject custom platform:
    public Ubuntu80Test(IExecutionPlatform executionPlatform) 
        : base("ubuntu-8.0", "8.0", "Ubuntu 22.04 (.NET 8.0)", executionPlatform)
    {
    }
}
```

### Dependency Injection

The default execution platform is registered in `Program.cs`:

```csharp
// Default to Docker
services.AddSingleton<IExecutionPlatform>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<DockerExecutionPlatform>>();
    return new DockerExecutionPlatform(logger);
});
```

### Custom Platform Registration

Users can override the default platform:

```csharp
// Use Rancher instead of Docker
services.AddSingleton<IExecutionPlatform>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<RancherExecutionPlatform>>();
    return new RancherExecutionPlatform(
        logger,
        rancherEndpoint: Environment.GetEnvironmentVariable("RANCHER_ENDPOINT")!,
        accessKey: Environment.GetEnvironmentVariable("RANCHER_ACCESS_KEY"),
        secretKey: Environment.GetEnvironmentVariable("RANCHER_SECRET_KEY"));
});
```

## Creating Custom Execution Platforms

### Step 1: Implement IExecutionPlatform

```csharp
public class MyCustomExecutionPlatform : IExecutionPlatform
{
    public string PlatformName => "MyCustomPlatform";
    
    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        // Check if your platform is available
    }
    
    public async Task<ExecutionBuildResult> BuildImageAsync(...)
    {
        // Implement image building
    }
    
    public async Task<ExecutionRunResult> RunContainerAsync(...)
    {
        // Implement container execution
    }
    
    // ... implement other methods
}
```

### Step 2: Register in DI Container

```csharp
services.AddSingleton<IExecutionPlatform>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<MyCustomExecutionPlatform>>();
    return new MyCustomExecutionPlatform(logger, /* config */);
});
```

### Step 3: Use in Tests

Tests will automatically use your custom platform:

```csharp
// MultiPlatformTestBase will use the injected IExecutionPlatform
var test = new Ubuntu80Test(); // Uses platform from DI
```

## Platform Capabilities

### Docker ✅

- **Image Building**: Full support via Dockerfile
- **Container Execution**: Full support
- **Volume Mounts**: Full support
- **Environment Variables**: Full support
- **Portability**: Works on Windows, Linux, macOS

### Rancher ⚠️ (Placeholder)

- **Status**: Placeholder - needs implementation
- **Use Case**: Rancher container orchestration
- **Integration**: Rancher API client required

### Kubernetes ⚠️ (Placeholder)

- **Status**: Placeholder - needs implementation
- **Use Case**: Kubernetes Job/Pod execution
- **Integration**: Kubernetes client library required (e.g., `KubernetesClient`)

## Benefits

1. **Flexibility**: Switch between Docker, Rancher, Kubernetes, etc. without code changes
2. **Extensibility**: Easy to add new execution platforms
3. **Testability**: Mock `IExecutionPlatform` for unit tests
4. **Portability**: Abstract away platform-specific details
5. **Future-Proof**: Ready for new orchestration platforms

## Migration from IDockerService

The old `IDockerService` interface is still available for backward compatibility, but new code should use `IExecutionPlatform`:

**Old**:
```csharp
IDockerService dockerService = new DockerService(logger);
```

**New**:
```csharp
IExecutionPlatform platform = new DockerExecutionPlatform(logger);
```

## Examples

### Using Docker (Default)

```csharp
// Automatic - uses DockerExecutionPlatform from DI
var test = new Ubuntu80Test();
await test.ExecuteAsync();
```

### Using Rancher

```csharp
// Register Rancher platform
services.AddSingleton<IExecutionPlatform>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<RancherExecutionPlatform>>();
    return new RancherExecutionPlatform(logger, "https://rancher.example.com");
});

// Tests automatically use Rancher
var test = new Ubuntu80Test(); // Uses RancherExecutionPlatform
```

### Using Kubernetes

```csharp
// Register Kubernetes platform
services.AddSingleton<IExecutionPlatform>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<KubernetesExecutionPlatform>>();
    return new KubernetesExecutionPlatform(logger, "~/.kube/config");
});

// Tests automatically use Kubernetes
var test = new Ubuntu80Test(); // Uses KubernetesExecutionPlatform
```

## Future Enhancements

- **AWS ECS/Fargate**: Execute tests in AWS containers
- **Azure Container Instances**: Execute tests in Azure
- **Google Cloud Run**: Execute tests in GCP
- **Podman**: Alternative container runtime
- **LXC/LXD**: System containers
