# SDK Integration Guide

This guide covers building external integrations against the stable Nexo SDK surface.

## Stability Tiers

| Tier | Meaning | Breaking-change policy |
|------|---------|----------------------|
| **Stable** | Public API, versioned, covered by compatibility tests | Deprecation notice + 1 minor version migration window |
| **Experimental** | May change without notice; marked with `[Obsolete("Experimental")]` | No guarantee |
| **Internal** | Implementation detail; do not depend on from external code | May change at any time |

See `docs/SdkCompatibilityPolicy.md` for the full versioning policy.

## Stable Public Surface

### Registration

```csharp
services.AddNexoSdk(sdk => sdk
    .RegisterBrick<MyBrick>()
    .RegisterAgent<MyAgent>()
    .RegisterAgentCard(new AgentCard { ... }));

services.AddNexo(options =>
{
    options.StrictMode.Enabled = true; // fail-fast during development
});
```

### Key Abstractions

| Interface | Package | Purpose |
|-----------|---------|---------|
| `IModel` | `Nexo.Abstractions` | LLM execution |
| `IProviderFactory` | `Nexo.Infrastructure` | Provider selection and execution |
| `IConfigurationService` | `Nexo.Core.Application` | Configuration management |
| `IPatternStore` | `Nexo.Core.Application` | Pattern storage for adaptation |
| `IKnowledgeQueryService` | `Nexo.Core.Application` | Cross-store knowledge queries |
| `ICopilotTaskStore` | `Nexo.Core.Application` | Copilot task persistence |

### Extension Points

- **Bricks:** Implement `Brick` (domain behavior unit). Register via `sdk.RegisterBrick<T>()`.
- **Agents:** Implement `IAgent` and provide an `AgentCard`. Register via `sdk.RegisterAgent<T>()` + `sdk.RegisterAgentCard(...)`.
- **Background agents:** Configure via agent set JSON (see `apps/release-manager/config/`).

## Reference Integration

A complete working sample lives in `docs/samples/StableSdkHostSample/`:

```bash
cd docs/samples/StableSdkHostSample
dotnet build
dotnet run
# Output: "Stable SDK host sample bootstrapped successfully."
```

This sample:
- Registers a custom brick and agent using only stable APIs
- Bootstraps the full Nexo kernel with `AddNexo()`
- Does not depend on internal namespaces

## Reference Integration Archetypes

### 1. CLI Tool Extension

Build a standalone CLI tool that uses Nexo for code analysis:

```csharp
services.AddNexoSdk(sdk => sdk.RegisterBrick<MyAnalyzerBrick>());
services.AddNexoProfile(NexoDeploymentProfile.System); // minimal
```

### 2. Background Service

Embed Nexo agents in a long-running service:

```csharp
services.AddNexo(opts =>
{
    opts.RegisterBackgroundAgentHostedService = true;
    opts.DeploymentProfile = NexoDeploymentProfile.Server;
});
```

### 3. Air-Gapped Deployment

Run Nexo with no cloud connectivity:

```csharp
services.AddNexoProfile(NexoDeploymentProfile.AirGapped, opts =>
{
    opts.StrictMode.Enabled = true;
    opts.TrustEnabled = true;
});
// Set NEXO_ALLOW_MOCK=1 or use Ollama locally
```

## CI Validation

The SDK sample is built in the readiness gate workflow to catch breaking changes:

```yaml
# .github/workflows/full-platform-readiness-gate.yml
- name: "Build SDK sample"
  run: dotnet build docs/samples/StableSdkHostSample/StableSdkHostSample.csproj
```
