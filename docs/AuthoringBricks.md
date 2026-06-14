# Authoring Bricks

This is the authoritative entry point for writing **code-authored bricks** for Nexo. For host setup and SDK registration context, see [`docs/sdk.md`](sdk.md) and [`docs/SdkIntegrationGuide.md`](SdkIntegrationGuide.md).

## What a brick is

A brick is a small unit of domain logic. Code-authored bricks derive from `Nexo.Core.Domain.Bricks.Brick` and implement one method:

```csharp
Task<BrickOutput> ExecuteAsync(
    BrickInput input,
    ImplementationType implementation,
    IExecutionContext context,
    CancellationToken cancellationToken = default);
```

The constructor or init properties define metadata and the input/output contract:

- `Id`, `Name`, `Version`, `Category`, `Description`
- optional `Icon`, `DomainKnowledge`, `Metadata`
- `Interface` with `BrickInputDefinition` and `BrickOutputDefinition`
- optional `Implementations`, `DefaultImplementation`, `FallbackChain`, `Selector`

`ExecuteAsync` reads typed values from `BrickInput`, returns a `BrickOutput`, and should set a human-readable `Summary`.

## Minimal code brick

```csharp
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;

public sealed class HelloBrick : Brick
{
    public HelloBrick()
    {
        Id = "sample.hello";
        Name = "Hello Brick";
        Version = "1.0.0";
        Category = BrickCategory.Transform;
        Description = "Returns a greeting for a supplied name.";
        Interface = new BrickInterface
        {
            Inputs = [new BrickInputDefinition("name", "string", "Name to greet", required: false, defaultValue: "world")],
            Outputs = [new BrickOutputDefinition("message", "string", "Greeting text")]
        };
    }

    public override Task<BrickOutput> ExecuteAsync(
        BrickInput input,
        ImplementationType implementation,
        IExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var name = input.Get("name", "world") ?? "world";
        var output = new BrickOutput { Summary = $"Greeted {name}." };
        output.Set("message", $"Hello, {name}!");
        return Task.FromResult(output);
    }
}
```

## Registering a brick

Host applications register code bricks through the stable host SDK surface:

```csharp
using Nexo.Hosting;
using Nexo.Hosting.Sdk;

services.AddNexoSdk(sdk => sdk.RegisterBrick<HelloBrick>());
services.AddNexo();
```

At runtime, registered code bricks flow into the same `IBrickRegistry` surface used by Nexo itself. The concrete registry composition includes local code bricks and may be wrapped by `CompositeBrickRegistry` when remote brick catalogs are configured.

For CLI/adaptation scenarios, `AddAdaptationBricks(typeof(HelloBrick))` is the lower-level equivalent. Prefer `AddNexoSdk(...RegisterBrick<T>())` for application-host code.

## Code bricks vs generated manifests

Nexo also has an adaptive manifest path: `INewBrickGenerator.GenerateAsync(...)` creates a `BrickManifest` from observed patterns. That path is for runtime/adaptive discovery and promotion.

Use **code-authored bricks** when you want to ship source-controlled domain logic with tests and stable package/version ownership. Use the **manifest generator** when Nexo is inferring a candidate brick from observed workflow patterns. Both paths describe the same conceptual brick surface; code bricks are the developer-authored, reviewable path.

## Scaffold a code brick from the published CLI

Install the CLI as a .NET tool and scaffold a standalone brick:

```bash
dotnet tool install --global Nexo.CLI
nexo new brick MyThing
cd MyThingBrick.Tests
dotnet test
```

The generated brick project references **`Nexo.Authoring`**, the published package for code-authored brick development. That single package brings the authoring surface (`Brick`, `BrickInput`, `BrickOutput`, `IExecutionContext`, `IBrickExecutor`) and host registration helpers.

Use `--nexo-version` when you want to pin the generated project to a specific package version:

```bash
nexo new brick MyThing --nexo-version 1.2.3
```

For local repo development you can still run:

```bash
dotnet run --project application/src/Nexo.CLI -- new brick Hello --output /tmp/hello-brick --nexo-version 9.9.9-local
```

Or inspect the template directly at [`samples/templates/brick/`](../samples/templates/brick/).

The complete reference implementation lives in [`samples/hello-brick/`](../samples/hello-brick/).
