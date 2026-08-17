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

`Nexo.Authoring` adds `services.AddNexoBrick<HelloBrick>()`, which is exactly `AddNexoSdk(sdk => sdk.RegisterBrick<HelloBrick>())` behind a one-call name: use `AddNexoBrick<T>()` when your project references `Nexo.Authoring` (the `nexo new brick` scaffold and [`consumer-template/CONSUMING.md`](../consumer-template/CONSUMING.md) do), and use `AddNexoSdk(...RegisterBrick<T>())` directly when you only reference the `Nexo.Hosting` graph or need to register several things in one `INexoSdkBuilder` callback. Both must run before `AddNexo()`.

## Code bricks vs generated manifests

Nexo also has an adaptive manifest path: `INewBrickGenerator.GenerateAsync(...)` creates a `BrickManifest` from observed patterns. That path is for runtime/adaptive discovery and promotion.

Use **code-authored bricks** when you want to ship source-controlled domain logic with tests and stable package/version ownership. Use the **manifest generator** when Nexo is inferring a candidate brick from observed workflow patterns. Both paths describe the same conceptual brick surface; code bricks are the developer-authored, reviewable path.

## Packages are not on nuget.org yet

**Nothing has been published.** No `v*` tag or GitHub release has been cut, `release.yml` / `release-nuget.yml` have never run, and `Nexo.CLI`, `Nexo.Authoring`, `Nexo.Hosting` and `Nexo.Hosting.Bundle` all return 404 on nuget.org. Any instruction of the form `dotnet tool install --global Nexo.CLI` or `<PackageReference Include="Nexo.Authoring" ... />` therefore fails today unless **you** supply the packages from a local folder feed. The rest of this page is written for that reality: start from the `ProjectReference` sample, and use the local-feed recipe only when you specifically want to exercise the standalone (`nexo new brick`) shape.

## Primary path: `samples/hello-brick` (ProjectReference)

[`samples/hello-brick/`](../samples/hello-brick/) is the complete reference implementation and the path that works from a repository checkout with no extra setup. Its brick project references the domain model by **`ProjectReference`**, not by package (`samples/hello-brick/HelloBrick/HelloBrick.csproj`):

```xml
<ItemGroup>
  <ProjectReference Include="../../../src/Nexo.Core.Domain/Nexo.Core.Domain.csproj" />
</ItemGroup>
```

Run it from the repository root:

```bash
dotnet test samples/hello-brick/HelloBrick.Tests/HelloBrick.Tests.csproj
```

To start your own brick, copy `samples/hello-brick/` next to it (or anywhere inside the checkout), rename the projects, and keep the `ProjectReference` pointing at `src/Nexo.Core.Domain/Nexo.Core.Domain.csproj` (add `src/Nexo.Authoring/Nexo.Authoring.csproj` if you want `AddNexoBrick<T>()` for host registration). One detail: the sample derives from `DomainBrick`, a `global using` alias for `Nexo.Core.Domain.Bricks.Brick` that `samples/Directory.Build.props` injects **only** into the `HelloBrick` project name, so a renamed copy should derive from `Brick` directly (as the minimal example above does; if your own namespace starts with `Nexo.` and you reference `Nexo.Authoring`, the `Nexo.Brick` namespace from `Nexo.Brick.Contracts` shadows the short name, so write `Nexo.Core.Domain.Bricks.Brick` or declare the same alias). The sample layout is:

- `HelloBrick/HelloBrick.csproj` — code-authored brick project.
- `HelloBrick/HelloBrick.cs` — `public sealed class HelloBrick : DomainBrick` (= `Brick`).
- `HelloBrick.Tests/HelloBrick.Tests.csproj` — xUnit test project.
- `HelloBrick.Tests/HelloBrickTests.cs` — smoke test for `ExecuteAsync`.

## Scaffold with the CLI (`nexo new brick`)

`nexo new brick <Name>` scaffolds a standalone brick project plus an xUnit test project from the template at [`samples/templates/brick/`](../samples/templates/brick/). The generated brick project references **`Nexo.Authoring`** as a `PackageReference` (`<PackageReference Include="Nexo.Authoring" Version="<cli version>" />`); that single package brings the authoring surface (`Brick`, `BrickInput`, `BrickOutput`, `IExecutionContext`, `IBrickExecutor`) and the `AddNexoBrick<T>()` host registration helper. Use `--nexo-version` to pin the generated reference to a specific package version:

```bash
nexo new brick MyThing --nexo-version 1.2.3
```

Because `Nexo.Authoring` is not on nuget.org, restoring the generated project fails with `NU1101: Unable to find package Nexo.Authoring` until you make the package restorable (next section). Inside a checkout you can run the CLI without installing it:

```bash
dotnet run --project application/src/Nexo.CLI -- new brick Hello --output /tmp/hello-brick --nexo-version 9.9.9-local
```

## Restoring Nexo.Authoring

Two options; the first is what CI verifies.

### Option 1: local folder feed (CI-verified)

[`scripts/verify-standalone-brick-authoring.sh`](../scripts/verify-standalone-brick-authoring.sh) packs the CLI and everything the generated brick needs into a folder feed, installs the CLI from that feed, scaffolds a brick, and restores it from the feed plus nuget.org. Run it end to end from the repository root:

```bash
bash scripts/verify-standalone-brick-authoring.sh
# NEXO_AUTHORING_VERIFY_VERSION (default 9.9.9-local) sets the pack/pin version;
# NEXO_AUTHORING_VERIFY_WORK (default: mktemp -d) sets the work dir so you can keep the output.
```

The commands it runs, transcribed (minus its `rg` guard against repo-relative paths leaking into the output), if you want to do it by hand (`ROOT` = repository root, `VERSION` = any semver such as `9.9.9-local`, `FEED` / `TOOL_PATH` / `BRICK_OUT` = empty scratch directories):

```bash
pack() {
  local project="$1"
  dotnet pack "${ROOT}/${project}" \
    -c Release \
    -o "${FEED}" \
    -p:PackageVersion="${VERSION}" \
    -p:IncludeTestProjectReferences=false \
    -v minimal
}

bash "${ROOT}/scripts/pack-nexo-hosting-graph.sh" "${VERSION}" "${FEED}"
pack src/Nexo.Adapters.Models/Nexo.Adapters.Models.csproj
pack src/Nexo.Bricks.Owasp/Nexo.Bricks.Owasp.csproj
pack src/Nexo.BackgroundAgents.HostRunners/Nexo.BackgroundAgents.HostRunners.csproj
pack src/Nexo.Policies.Dev/Nexo.Policies.Dev.csproj
pack src/Nexo.Authoring/Nexo.Authoring.csproj
pack application/src/Nexo.CLI/Nexo.CLI.csproj

dotnet tool install \
  --tool-path "${TOOL_PATH}" \
  Nexo.CLI \
  --version "${VERSION}" \
  --add-source "${FEED}" \
  --ignore-failed-sources

# --json is optional (machine-readable result; the script uses it)
"${TOOL_PATH}/nexo" new brick SampleThing \
  --output "${BRICK_OUT}" \
  --nexo-version "${VERSION}" \
  --json

dotnet restore "${BRICK_OUT}/SampleThingBrick.Tests/SampleThingBrick.Tests.csproj" \
  --source "${FEED}" \
  --source https://api.nuget.org/v3/index.json

dotnet build "${BRICK_OUT}/SampleThingBrick.Tests/SampleThingBrick.Tests.csproj" \
  --no-restore \
  -v minimal

dotnet test "${BRICK_OUT}/SampleThingBrick.Tests/SampleThingBrick.Tests.csproj" \
  --no-build \
  --blame-hang-timeout 120s \
  --blame-hang-dump-type none
```

The two things that matter for any brick you scaffold yourself: pass the packed version to `--nexo-version` so the generated `PackageReference` matches what is in the feed, and give `dotnet restore` both `--source "${FEED}"` (for `Nexo.*`) and `--source https://api.nuget.org/v3/index.json` (for xunit, FluentAssertions and the rest).

### Option 2: switch the scaffold to a ProjectReference

If the brick lives inside (or next to) a Nexo checkout, replace the package reference in the generated `<Name>Brick.csproj`:

```xml
<!-- before -->
<PackageReference Include="Nexo.Authoring" Version="9.9.9-local" />
<!-- after: path relative to the generated project -->
<ProjectReference Include="../../src/Nexo.Authoring/Nexo.Authoring.csproj" />
```

and restore normally. This is the same shape as `samples/hello-brick` (which references `src/Nexo.Core.Domain` directly).
