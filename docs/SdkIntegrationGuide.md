# SDK Integration Guide

This guide covers building external integrations against the stable Nexo SDK surface. For packaging, pinning, and per-channel CI validation, see **`docs/DistributionModels.md`**.

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

The readiness gate builds the **project-reference** sample and then verifies **NuGet-only** consumption:

1. `dotnet build docs/samples/StableSdkHostSample/StableSdkHostSample.csproj` (in-repo references).
2. `scripts/verify-stable-sdk-host-sample-packages.sh` (POSIX) or `scripts/verify-stable-sdk-host-sample-packages.ps1` (Windows): packs the `Nexo.Hosting` dependency graph to a local feed, restores `docs/samples/StableSdkHostSample/package-consumer/StableSdkHostSample.Package.csproj` with **`--force-evaluate`** and an **empty package cache** by default (avoids masking from `~/.nuget/packages`), builds, and runs.

See `.github/workflows/full-platform-readiness-gate.yml` (steps **Setup — build SDK sample** and **Setup — verify SDK sample consumes local NuGet graph**).

## Publishing `Nexo.Hosting` for external repos

`Nexo.Hosting` depends on other `Nexo.*` projects; publish **the same `PackageVersion`** for the whole graph before pushing to a feed:

```bash
bash scripts/pack-nexo-hosting-graph.sh 1.2.3 /path/to/output
```

Then push `*.nupkg` from that folder to **nuget.org** or **GitHub Packages**. External hosts reference **`Nexo.Hosting`** only; NuGet resolves the matching versions of transitive `Nexo.*` packages from the same feed.
