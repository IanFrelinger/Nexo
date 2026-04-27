# Publishing Nexo for external consumption

This document describes how to **produce** and **publish** the .NET packages that external repos (for example game tooling) consume. CI already **verifies** NuGet-only consumption locally; publishing to a feed is an operator step.

## What gets published

Embedding the Nexo kernel from another .NET app uses **`Nexo.Hosting`**, which depends on other **`Nexo.*`** packages built from this repo. Publish **all of them with the same semantic version** (for example `1.2.3`).

Use:

```bash
bash scripts/pack-nexo-hosting-graph.sh 1.2.3 ./artifacts/nuget-release
```

(or `scripts/pack-nexo-hosting-graph.ps1` on Windows). Output is a folder of `*.nupkg` / `*.snupkg` files, including **`Nexo.Hosting.Bundle`** — a **metapackage** so consumers can add **one** `PackageReference` instead of chasing transitive versions manually.

**Consumer recommendation:** reference **`Nexo.Hosting.Bundle`** at version `1.2.3` (same as the graph). **`Nexo.Hosting`** remains the real assembly package; the bundle only pulls the graph.

**Note:** `Nexo.Hosting.Bundle` is **not** part of `Nexo.sln` — it only restores after the graph exists on a feed. CI packs it via `scripts/pack-nexo-hosting-graph.*`; local `dotnet build` of the repo does not need it.

Stable **client** surface (HTTP) is documented in `docs/sdk.md` (`Nexo.Sdk` / `Nexo.Client`); pack those separately if you publish them to the same feed:

```bash
dotnet pack src/Nexo.Client/Nexo.Client.csproj -c Release -o ./artifacts/nuget-release -p:PackageVersion=1.2.3
dotnet pack src/Nexo.Sdk/Nexo.Sdk.csproj -c Release -o ./artifacts/nuget-release -p:PackageVersion=1.2.3
```

## Verify before you push

From a clean machine or CI artifact:

```bash
export NEXO_SDK_PACKAGE_VERSION=1.2.3
bash scripts/verify-stable-sdk-host-sample-packages.sh
```

This packs the graph to `artifacts/nuget-verify/packages`, restores `docs/samples/StableSdkHostSample/package-consumer/` against **only** that folder + nuget.org, builds, and runs the sample.

## Publish to nuget.org (you do this)

### Option A — GitHub Actions (recommended)

Workflows:

- **`.github/workflows/release.yml`** — **tag `v*.*.*`**: GHCR **and** NuGet in one run. Configure Trusted Publishing for workflow file **`release.yml`** (filename only).
- **`.github/workflows/release-nuget.yml`** — **manual NuGet-only** dispatch (version input).

1. **Repository variable** `NUGET_PUBLISH_MODE`:
   - `none` — build only; download **`nuget-packages-<version>`** artifact and push manually.
   - `oidc` — [Trusted Publishing](https://learn.microsoft.com/nuget/nuget-org/trusted-publishing): policy must include **`release.yml`** (for tag releases). Secret **`NUGET_USER`** = nuget.org **profile name** (not email).
   - `apikey` — secret **`NUGET_API_KEY`**.
2. **Trigger:** push tag **`v1.2.3`** (preferred), or **Actions → Release** / **Release NuGet packages** for partial flows.
3. Write **GitHub Release** notes and verify packages on nuget.org.

### Option B — Manual from your machine

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

- When `Nexo.Hosting` gains a **new project reference** to another in-repo `Nexo.*` project, add that project to **`scripts/pack-nexo-hosting-graph.sh`** / **`.ps1`** so the graph stays publishable. CI runs **`python3 scripts/verify-pack-nexo-hosting-graph-alignment.py`**, which compares the pack list to the **transitive** `ProjectReference` closure from `Nexo.Hosting` (and checks `.sh` matches `.ps1`).
- Keep **`PackageVersion`** in sync across the graph for a given release (the scripts pass one version to every `dotnet pack`).

## Post-push nuget.org check (optional)

After a successful push, **`reusable-release-nuget.yml`** can poll the [flat container](https://api.nuget.org/v3-flatcontainer/) for **`Nexo.Hosting.Bundle`** until the `.nupkg` returns HTTP 200 (handles index lag). Set repository variable **`NUGET_POST_PUSH_VERIFY`** to **`false`** to skip this step. Tune **`NEXO_NUGET_VERIFY_ATTEMPTS`** / **`NEXO_NUGET_VERIFY_SLEEP_SEC`** in `scripts/verify-nuget-org-package-visible.sh` if needed.

## Operator checklist

See **`docs/RELEASE_RUNBOOK.md`** for a one-page release sequence (tag, workflows, secrets, rollback notes).
