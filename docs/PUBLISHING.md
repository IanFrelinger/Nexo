# Publishing Nexo for external consumption

This document describes how to **produce** and **publish** the .NET packages that external repos (for example game tooling) consume. CI already **verifies** NuGet-only consumption locally; publishing to a feed is an operator step.

## What gets published

Embedding the Nexo kernel from another .NET app uses **`Nexo.Hosting`**. That package depends on other **`Nexo.*`** packages built from this repo. Publish **all of them with the same semantic version** (for example `1.2.3`).

Use:

```bash
bash scripts/pack-nexo-hosting-graph.sh 1.2.3 ./artifacts/nuget-release
```

(or `scripts/pack-nexo-hosting-graph.ps1` on Windows). Output is a folder of `*.nupkg` / `*.snupkg` files.

Stable **client** surface (HTTP) is documented in `docs/sdk.md` (`Nexo.Sdk` / `Nexo.Client`); pack those separately if you publish them to the same feed.

## Verify before you push

From a clean machine or CI artifact:

```bash
export NEXO_SDK_PACKAGE_VERSION=1.2.3
bash scripts/verify-stable-sdk-host-sample-packages.sh
```

This packs the graph to `artifacts/nuget-verify/packages`, restores `docs/samples/StableSdkHostSample/package-consumer/` against **only** that folder + nuget.org, builds, and runs the sample.

## Publish to nuget.org (you do this)

1. Create a **NuGet.org** account (if needed) and an **API key** with scope **Push** for the `Nexo.*` package IDs (or org-owned IDs).
2. Locally: `dotnet nuget push "artifacts/nuget-release/*.nupkg" --api-key <KEY> --source https://api.nuget.org/v3/index.json`
3. Optionally push symbols: `*.snupkg` to the same source (NuGet accepts symbols alongside).
4. Tag the git repo **`v1.2.3`** to match the package version you pushed.
5. Write **GitHub Release** notes: list package versions, migration notes, and any breaking changes per `docs/SdkCompatibilityPolicy.md`.

## Publish to GitHub Packages (alternative)

1. Generate a **PAT** or use `GITHUB_TOKEN` in Actions with `packages: write`.
2. `dotnet nuget add source` pointing at `https://nuget.pkg.github.com/<OWNER>/index.json` with credentials.
3. `dotnet nuget push` the same folder to that source.
4. Document for consumers: **feed URL**, **auth** (PAT as password), and **exact package version** to pin.

## Container image (separate track)

The CLI image is published by `.github/workflows/container-image-publish.yml` to **GHCR**. That is orthogonal to NuGet; ops users may consume **only** the image, .NET hosts consume **NuGet**, or both.

## What you must maintain over time

- When `Nexo.Hosting` gains a **new project reference** to another in-repo `Nexo.*` project, add that project to **`scripts/pack-nexo-hosting-graph.sh`** / **`.ps1`** so the graph stays publishable.
- Keep **`PackageVersion`** in sync across the graph for a given release (the scripts pass one version to every `dotnet pack`).
