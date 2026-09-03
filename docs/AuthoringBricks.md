# Authoring Bricks

This is the authoritative entry point for writing **code-authored bricks** for Ashlar. For host setup and SDK registration context, see [`docs/sdk.md`](sdk.md) and [`docs/SdkIntegrationGuide.md`](SdkIntegrationGuide.md); for consuming Ashlar as packages with no checkout, [`docs/ConsumingFromNuGet.md`](ConsumingFromNuGet.md).

> **Before you choose a project layout, decide whether this brick will be certified.** A brick the
> certification gate can admit must live in its **own project**, carry **no `ProjectReference` at
> all**, and reference **at most two packages** — `Ashlar.Brick.Contracts` and `Ashlar.Authoring`.
> That is enforced, not advisory, and it is the leg most first-time authors hit. The rule, the exact
> refusals, and how to run the gate yourself are in
> [`docs/CertificationGate.md`](CertificationGate.md). Everything else on this page applies either
> way.

## What a brick is

A brick is a small unit of domain logic. Code-authored bricks derive from `Ashlar.Core.Domain.Bricks.Brick` and implement one method:

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
using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Execution;

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
using Ashlar.Hosting;
using Ashlar.Hosting.Sdk;

services.AddAshlarSdk(sdk => sdk.RegisterBrick<HelloBrick>());
services.AddAshlar();
```

At runtime, registered code bricks flow into the same `IBrickRegistry` surface used by Ashlar itself. The concrete registry composition includes local code bricks and may be wrapped by `CompositeBrickRegistry` when remote brick catalogs are configured.

For CLI/adaptation scenarios, `AddAdaptationBricks(typeof(HelloBrick))` is the lower-level equivalent. Prefer `AddAshlarSdk(...RegisterBrick<T>())` for application-host code.

`Ashlar.Authoring` adds `services.AddAshlarBrick<HelloBrick>()`, which is exactly `AddAshlarSdk(sdk => sdk.RegisterBrick<HelloBrick>())` behind a one-call name: use `AddAshlarBrick<T>()` when your project references `Ashlar.Authoring` (the `ashlar new brick` scaffold and [`consumer-template/CONSUMING.md`](../consumer-template/CONSUMING.md) do), and use `AddAshlarSdk(...RegisterBrick<T>())` directly when you only reference the `Ashlar.Hosting` graph or need to register several things in one `IAshlarSdkBuilder` callback. Both must run before `AddAshlar()`.

## Code bricks vs generated manifests

Ashlar also has an adaptive manifest path: `INewBrickGenerator.GenerateAsync(...)` creates a `BrickManifest` from observed patterns. That path is for runtime/adaptive discovery and promotion.

Use **code-authored bricks** when you want to ship source-controlled domain logic with tests and stable package/version ownership. Use the **manifest generator** when Ashlar is inferring a candidate brick from observed workflow patterns. Both paths describe the same conceptual brick surface; code bricks are the developer-authored, reviewable path.

## Getting the packages

**`Ashlar.*` is on nuget.org at `0.1.1`** (published 2026-09-01 by `release.yml` via Trusted Publishing/OIDC, with SPDX SBOMs), including `Ashlar.Brick.Contracts`, `Ashlar.Authoring`, `Ashlar.Hosting`, `Ashlar.Hosting.Bundle` and the `Ashlar.CLI` .NET tool. Both `dotnet tool install --global Ashlar.CLI --version 0.1.1` and `<PackageReference Include="Ashlar.Authoring" Version="0.1.1" />` resolve from plain nuget.org with no extra feed. [`docs/ConsumingFromNuGet.md`](ConsumingFromNuGet.md) is the package-only getting-started page.

The local-folder-feed recipe below is still the right tool for a **pre-release** version you packed yourself out of a checkout — which is what `scripts/verify-standalone-brick-authoring.sh` exercises, and why it pins `9.9.9-local` rather than a released version.

## The certifiable shape: one project, at most two packages

If the brick will ever face the certification gate, this is the layout to start from — retrofitting it later means splitting the project. The gate’s dependency leg rejects *any* `ProjectReference` and allows only `Ashlar.Brick.Contracts` and `Ashlar.Authoring`, and the certificate binds a SHA-256 of the single source file, so the brick has to be a project on its own.

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>true</IsPackable>
    <PackageId>Acme.Bricks.LateFee</PackageId>
    <!-- Required under any directory that has a Directory.Packages.props. -->
    <ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Ashlar.Brick.Contracts" Version="0.1.1" />
  </ItemGroup>
</Project>
```

One brick, one `.cs`, one `.csproj`, in its own directory. Add `Ashlar.Authoring` if the same project also needs `AddAshlarBrick<T>()`; add nothing else. `samples/certified-brick-reuse/Ashlar.Certified.DamageResolver/` is the tracked example of exactly this shape, and [`docs/CertificationGate.md`](CertificationGate.md) is how you drive the gate over it.

## Learning path: `samples/hello-brick` (package-only, certifiable)

[`samples/hello-brick/`](../samples/hello-brick/) is the complete reference implementation of the `Brick` API and the smallest brick the certification gate admits. It has exactly the shape above: one project, one source file, one `PackageReference` to `Ashlar.Brick.Contracts` (`0.1.1`, on nuget.org), and a witness beside the source. `ShippedSampleCertificationTests` certifies it as checked in on every cert-gate run, so the sample can be copied as a starting point for a brick you intend to certify. That run uses the gate **at this line (`0.1.2`)**: the sample carries no `CopyLocalLockFileAssemblies`, because the `0.1.2` loader reads references from the compiler's own record of the build. Under the `Ashlar.Infrastructure 0.1.1` package on nuget.org — whose loader reads `*.dll` from the output folder — this same sample is REJECTED at the analyzer leg (`analyzer anchor type ... is not resolvable`). [`docs/CertificationGate.md`](CertificationGate.md) opens with what a `0.1.1` consumer actually gets.

```xml
<ItemGroup>
  <PackageReference Include="Ashlar.Brick.Contracts" Version="0.1.1" />
</ItemGroup>
```

Run the smoke test from the repository root:

```bash
dotnet test samples/hello-brick/HelloBrick.Tests/HelloBrick.Tests.csproj
```

and certify it (`samples/hello-brick/README.md` lists the exit codes):

```bash
dotnet run --project tools/Ashlar.ExportCertifiedBrick/ExportCertifiedBrick.csproj -- \
  /tmp/hello-brick-record.json samples/hello-brick/HelloBrick
```

To start your own brick inside the checkout, copy `samples/hello-brick/` next to it and rename the projects; keep the `PackageReference`. Swapping it for a `ProjectReference` into `src/Ashlar.Core.Domain` builds without nuget.org but makes the brick uncertifiable (the gate refuses any `ProjectReference`). If your own namespace starts with `Ashlar.`, the `Ashlar.Brick` namespace from `Ashlar.Brick.Contracts` shadows the short name `Brick`, so write `Ashlar.Core.Domain.Bricks.Brick` in full, as `samples/certified-brick-reuse/Ashlar.Certified.DamageResolver/` does. The sample layout is:

- `HelloBrick/HelloBrick.csproj` — code-authored brick project (`PackageReference` only).
- `HelloBrick/HelloBrick.cs` — `public sealed class HelloBrick : Brick`.
- `HelloBrick/hello-brick.witness.json` — the witness the gate replays.
- `HelloBrick.Tests/HelloBrick.Tests.csproj` — xUnit test project.
- `HelloBrick.Tests/HelloBrickTests.cs` — smoke test for `ExecuteAsync`.

## Scaffold with the CLI (`ashlar new brick`)

`ashlar new brick <Name>` scaffolds a standalone brick project plus an xUnit test project from the template at [`samples/templates/brick/`](../samples/templates/brick/). The generated brick project references **`Ashlar.Authoring`** as a `PackageReference` (`<PackageReference Include="Ashlar.Authoring" Version="<cli version>" />`); that single package brings the authoring surface (`Brick`, `BrickInput`, `BrickOutput`, `IExecutionContext`, `IBrickExecutor`) and the `AddAshlarBrick<T>()` host registration helper. Use `--ashlar-version` to pin the generated reference to a specific package version:

```bash
ashlar new brick MyThing --ashlar-version 1.2.3
```

With a released version (`--ashlar-version 0.1.1`) the generated project restores from plain nuget.org. Which *template* you get depends on the CLI, not on `--ashlar-version`: the template tracked at this line certifies exactly as scaffolded under the `0.1.2` gate (`BrickCertificationProjectLoaderReferenceTests.The_brick_template_certifies_exactly_as_scaffolded`), but the **`Ashlar.CLI 0.1.1` tool on nuget.org embeds the older template**, whose scaffold the `0.1.2` gate REJECTS — run the CLI from a checkout (below) or wait for `Ashlar.CLI 0.1.2` if the scaffold has to certify. If you pin a version that exists only in a local feed, restore fails with `NU1101: Unable to find package Ashlar.Authoring` until you make that feed visible (next section). Inside a checkout you can run the CLI without installing it:

```bash
dotnet run --project application/src/Ashlar.CLI -- new brick Hello --output /tmp/hello-brick --ashlar-version 9.9.9-local
```

## Restoring Ashlar.Authoring

For a **released** version there is nothing to do: `Ashlar.Authoring 0.1.1` restores from plain nuget.org. The two options below are for a version that is not on nuget.org — a pre-release you packed yourself, or a brick that must track `src/` exactly. The first is what CI verifies.

### Option 1: local folder feed (CI-verified)

[`scripts/verify-standalone-brick-authoring.sh`](../scripts/verify-standalone-brick-authoring.sh) packs the CLI and everything the generated brick needs into a folder feed, installs the CLI from that feed, scaffolds a brick, and restores it from the feed plus nuget.org. Run it end to end from the repository root:

```bash
bash scripts/verify-standalone-brick-authoring.sh
# ASHLAR_AUTHORING_VERIFY_VERSION (default 9.9.9-local) sets the pack/pin version;
# ASHLAR_AUTHORING_VERIFY_WORK (default: mktemp -d) sets the work dir so you can keep the output.
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

bash "${ROOT}/scripts/pack-ashlar-hosting-graph.sh" "${VERSION}" "${FEED}"
pack src/Ashlar.Adapters.Models/Ashlar.Adapters.Models.csproj
pack src/Ashlar.Bricks.Owasp/Ashlar.Bricks.Owasp.csproj
pack src/Ashlar.BackgroundAgents.HostRunners/Ashlar.BackgroundAgents.HostRunners.csproj
pack src/Ashlar.Policies.Dev/Ashlar.Policies.Dev.csproj
pack src/Ashlar.Authoring/Ashlar.Authoring.csproj
pack application/src/Ashlar.CLI/Ashlar.CLI.csproj

dotnet tool install \
  --tool-path "${TOOL_PATH}" \
  Ashlar.CLI \
  --version "${VERSION}" \
  --add-source "${FEED}" \
  --ignore-failed-sources

# --json is optional (machine-readable result; the script uses it)
"${TOOL_PATH}/ashlar" new brick SampleThing \
  --output "${BRICK_OUT}" \
  --ashlar-version "${VERSION}" \
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

The two things that matter for any brick you scaffold yourself: pass the packed version to `--ashlar-version` so the generated `PackageReference` matches what is in the feed, and give `dotnet restore` both `--source "${FEED}"` (for `Ashlar.*`) and `--source https://api.nuget.org/v3/index.json` (for xunit, FluentAssertions and the rest).

### Option 2: switch the scaffold to a ProjectReference

If the brick lives inside (or next to) a Ashlar checkout, replace the package reference in the generated `<Name>Brick.csproj`:

```xml
<!-- before -->
<PackageReference Include="Ashlar.Authoring" Version="9.9.9-local" />
<!-- after: path relative to the generated project -->
<ProjectReference Include="../../src/Ashlar.Authoring/Ashlar.Authoring.csproj" />
```

and restore normally. A brick built this way is uncertifiable (the gate refuses any `ProjectReference`); `samples/hello-brick` keeps the package reference for that reason.
