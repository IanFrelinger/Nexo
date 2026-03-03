# Nexo SDK

The Nexo SDK enables **runtime registration** of bricks and agents without recompiling Nexo. Use `INexoSdkBuilder` to register custom components before calling `AddNexo()`.

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
