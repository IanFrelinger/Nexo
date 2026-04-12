# Nexo SDK

Nexo currently ships two similarly named integration surfaces:

- **Host SDK surface (stable)**: `Nexo.Hosting.Sdk` extension methods + `INexoSdkBuilder` for registering bricks/agents in a host process.
- **Client SDK surface (stable)**: `Nexo.Sdk` (`Nexo.Client`) for talking to a running Nexo API over HTTP.

Use the host surface when embedding Nexo into your own service. Use the client surface when your app calls an external Nexo API.

## Support boundary (v1)

### Stable

- `Nexo.Hosting.Sdk` (`AddNexoSdk(Action<INexoSdkBuilder>)` on `IServiceCollection`)
- `Nexo.Core.Application.Sdk.Ports.INexoSdkBuilder`
- `Nexo.Sdk` + `Nexo.Client` (`INexoClient`, `AddNexoSdk(baseUrl, ...)`)

### Experimental (subject to faster change)

- `Nexo.Sdk.NexoSdkBuilder.UseAdaptiveRouting()` intent flag behavior

### Internal (not for external contracts)

- Anything under `Nexo.Infrastructure.*`
- Runtime execution internals and orchestration internals not exposed through the stable interfaces above

## Breaking-change policy

- Stable SDK APIs follow semantic versioning for package changes.
- Breaking changes to stable surfaces are only introduced in major version bumps.
- Experimental APIs may change in minor versions; avoid hard dependencies unless you pin package versions.

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

- `docs/samples/StableSdkHostSample/`

The sample intentionally uses only `Nexo.Hosting.Sdk` + `INexoSdkBuilder` extension points and avoids internal namespaces.
