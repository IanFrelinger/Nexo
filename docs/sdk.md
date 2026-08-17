# Nexo SDK

Nexo currently ships two similarly named integration surfaces:

- **Host SDK surface (stable)**: `Nexo.Hosting.Sdk` extension methods + `INexoSdkBuilder` for registering bricks/agents in a host process.
- **Client SDK surface (stable)**: `Nexo.Sdk` (`Nexo.Client`) for talking to a running Nexo API over HTTP.

Use the host surface when embedding Nexo into your own service. Use the client surface when your app calls an external Nexo API.

## Support boundary (v1)

### Stable

- `Nexo.Hosting.Sdk` (`AddNexoSdk(Action<INexoSdkBuilder>)` on `IServiceCollection`; builder implementation `HostNexoSdkBuilder`)
- `Nexo.Infrastructure.Sdk.Ports.INexoSdkBuilder`
- `Nexo.Sdk.Client` (`AddNexoClientSdk(baseUrl, ...)`, `NexoClientSdkBuilder`) + `Nexo.Client` (`INexoClient`)
- Obsolete compat: `AddNexoSdk(baseUrl, ...)` / `NexoSdkBuilder` on the client package (same assembly as `Nexo.Sdk`)

### Deprecated

- `Nexo.Sdk.NexoSdkBuilder.UseAdaptiveRouting()` — marked `[Obsolete]` and is a no-op. Adaptive routing is configured on the host via `IProviderFactory`, not the client SDK.

### Internal (not for external contracts)

- Anything under `Nexo.Infrastructure.*`
- Runtime execution internals and orchestration internals not exposed through the stable interfaces above

## Breaking-change policy

- Stable SDK APIs follow semantic versioning for package changes; the full policy, the packages it covers and the mechanism that enforces it (`PublicAPI.Shipped.txt` / `PublicAPI.Unshipped.txt` under `Microsoft.CodeAnalysis.PublicApiAnalyzers`, warnings-as-errors) are in `docs/SdkCompatibilityPolicy.md`.
- `v0.1.x`: no breaking changes to stable surfaces within `0.1.x`; breaking changes only in `0.(x+1).0` after an `[Obsolete]` deprecation in the prior minor. From `1.0.0`: only in major version bumps.
- Experimental APIs (`[Experimental("NEXOEXP001")]`, the autonomy loop) may change in any release; using them is a compile-time opt-in. Avoid hard dependencies unless you pin package versions.
- The HTTP routes the client SDK calls have their own policy: `docs/api/versioning.md` (unversioned in `v0.x`, `/api/v1` at `1.0`, one-minor deprecation window, **Breaking** entries in `CHANGELOG.md`).

## Quick Start

```csharp
using Nexo.Hosting;
using Nexo.Hosting.Sdk;

var builder = WebApplication.CreateBuilder(args);

// 1. Configure the SDK (call before AddNexo)
builder.Services.AddNexoSdk(sdk => sdk
    .RegisterBrick<MyCustomBrick>()
    .RegisterAgent<MyCustomAgent>()
    .RegisterAgentCard(myAgentCard));

// 2. Add the Nexo kernel
builder.Services.AddNexo();

var app = builder.Build();
```

## Client SDK quick start

```csharp
using Microsoft.Extensions.DependencyInjection;
using Nexo.Sdk;

var services = new ServiceCollection();
services.AddNexoSdk("http://localhost:5000");
var provider = services.BuildServiceProvider();
```

## Client SDK coverage

`INexoClient` currently exposes these operations:

| Method | API Path |
|--------|----------|
| `RunAgentAsync` | `POST /api/agent` |
| `RunValidationAsync` | `POST /api/validate` |
| `OrchestrateAsync` | `POST /api/orchestrate` |
| `GetStatusAsync` | `GET /api/status` |
| `BuildImageAsync` | `POST /api/execution/build` |
| `RunContainerAsync` | `POST /api/execution/run` |
| `InvokeAsync` | Any path (escape hatch; same `HttpClient`, base URL, and `X-Api-Key` as typed methods) |

Endpoints not yet wrapped as typed methods (copilot, director, trust mutation, knowledge query, capabilities, etc.) can be called with **`InvokeAsync`** using the paths in `docs/api/index.md`, or with your own `HttpClient` against the same base URL.

`AddNexoClient` accepts `BaseUrl`, optional `ApiKey` (sent as `X-Api-Key` header), and `Timeout`.

### `InvokeAsync` example

```csharp
using var json = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");
using var resp = await client.InvokeAsync(HttpMethod.Post, "api/copilot/task", json, ct);
resp.EnsureSuccessStatusCode();
var body = await resp.Content.ReadAsStringAsync(ct);
```

## Unity and other game engines

- **Recommended:** run Nexo as a **separate .NET host** (local or mesh) and call it with **`Nexo.Client`** / **`InvokeAsync`** from an **Editor** assembly (HTTP + optional API key). Keep the **player build** free of the full hosting stack unless you explicitly need it.
- **Embed `Nexo.Hosting` in-process** is possible for **.NET** hosts (tools, servers); for Unity it is usually heavier and version-sensitive than HTTP.
- **Unreal / non-.NET:** use the **HTTP API** (same paths as `docs/api/index.md`); generate clients from OpenAPI if you add a spec to your release pipeline.

## RegisterBrick&lt;T&gt;

Registers a brick type derived from `Nexo.Core.Domain.Bricks.Brick`. The brick will be available in the brick registry and can be used by the adaptation pipeline, workflow executor, and behavior executor.

```csharp
sdk.RegisterBrick<OWASPScannerBrick>();
sdk.RegisterBrick<MySecurityBrick>();
```

Bricks are resolved via dependency injection when instantiated. Ensure required services (e.g. `IProviderFactory`) are registered.

## RegisterAgent&lt;T&gt;

Registers an agent type implementing `Nexo.Abstractions.IAgent`. The agent will be discoverable by the agent executor and can be invoked by name.

```csharp
sdk.RegisterAgent<ToolCallingAgent>();
sdk.RegisterAgent<MyCustomAgent>();
```

Agents must implement `IAgent` (Name, ThinkAsync).

## RegisterAgentCard

Registers an `AgentCard` for workflow execution. Agent cards define personas with behaviors for the behavior executor.

```csharp
var card = new AgentCard
{
    Id = "my-agent",
    Name = "My Agent",
    Domain = "security",
    Description = "Custom security agent",
    Behaviors = new[] { "scan", "remediate" }
};
sdk.RegisterAgentCard(card);
```

## Call Order

**Important:** Call `AddNexoSdk` **before** `AddNexo()`:

```csharp
builder.Services.AddNexoSdk(configure);  // First
builder.Services.AddNexo();               // Second
```

## Integration with AddAdaptationBricks

The SDK `RegisterBrick` and the existing `AddAdaptationBricks` both add brick types to the adaptation pipeline. You can use either:

- **AddNexoSdk** + `RegisterBrick`: For host applications that configure everything in one place
- **AddAdaptationBricks**: For CLI commands or scenarios that add bricks after adaptation infrastructure is registered

Both mechanisms merge into the same `AdaptationBrickOptions.AdditionalBrickTypes` list.

## Reference sample

A minimal, stable-only host integration sample is provided at:

- `docs/samples/StableSdkHostSample/` — **project-reference** mode (`StableSdkHostSample.csproj`) for contributors working inside the repo.
- `docs/samples/StableSdkHostSample/package-consumer/` — **package-only** mode (`StableSdkHostSample.Package.csproj`): references **`Nexo.Hosting.Bundle`** from NuGet; verified by `scripts/verify-stable-sdk-host-sample-packages.sh` against a local feed after `scripts/pack-nexo-hosting-graph.sh` (isolated `NUGET_PACKAGES` + `--force-evaluate` by default; see `docs/PUBLISHING.md`).

The sample intentionally uses only `Nexo.Hosting.Sdk` + `INexoSdkBuilder` extension points and avoids internal namespaces.
