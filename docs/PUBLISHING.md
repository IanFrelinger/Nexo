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

To verify an **unpacked** CI artifact folder without re-packing:

```bash
export NEXO_SDK_PACKAGE_VERSION=1.2.3
export NEXO_SDK_PACKAGE_FEED=/path/to/unpacked/nuget-packages
bash scripts/verify-stable-sdk-host-sample-packages.sh
```

## Publish to nuget.org (you do this)

### Option A — GitHub Actions (recommended)

Workflows:

- **`.github/workflows/release.yml`** — **tag `v*.*.*`**: GHCR **and** NuGet in one run.
- **`.github/workflows/release-nuget.yml`** — **manual NuGet-only** dispatch (version input).

**Trusted Publishing (OIDC)** is bound to the **caller** workflow file. Register every entry point you use on nuget.org:

| If you publish via | Register this workflow file |
|--------------------|-----------------------------|
| Tag push → **Release** | **`release.yml`** |
| **Actions → Release NuGet packages** with `NUGET_PUBLISH_MODE=oidc` | **`release-nuget.yml`** |

1. **Repository variable** `NUGET_PUBLISH_MODE`:
   - unset, empty, or **`none`** — pack + artifact only; download **`nuget-packages-<version>`** and push manually if desired.
   - **`oidc`** — [Trusted Publishing](https://learn.microsoft.com/nuget/nuget-org/trusted-publishing) for the table above. Secret **`NUGET_USER`** = nuget.org **profile name** (not email).
   - **`apikey`** — secret **`NUGET_API_KEY`**.
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

- When `Nexo.Hosting` gains a **new project reference** to another in-repo `Nexo.*` project, add that project to **`scripts/pack-nexo-hosting-graph.sh`** / **`.ps1`**. CI runs **`python3 scripts/verify-pack-nexo-hosting-graph-alignment.py`**, which compares the pack list to the **transitive** `ProjectReference` closure from `Nexo.Hosting`. If you must pack extra `Nexo.*` projects **not** in that closure, list them (one path per line) in **`scripts/pack-nexo-hosting-graph.allowlist.txt`** with a short comment.
- Keep **`PackageVersion`** in sync across the graph for a given release (the scripts pass one version to every `dotnet pack`).

## After push (CI): visibility + restore

When **`NUGET_PUBLISH_MODE`** is **`oidc`** or **`apikey`**, **`reusable-release-nuget.yml`** (unless disabled below):

1. Polls the nuget.org **flat container** until these ids are visible: **`Nexo.Hosting.Bundle`**, **`Nexo.Hosting`**, **`Nexo.Sdk`** (override with repo variable **`NUGET_POST_PUSH_VERIFY_PACKAGE_IDS`** as a comma-separated list).
2. Runs **`scripts/verify-nuget-org-restore-published-version.sh`**: `dotnet restore` of **`docs/samples/NugetOrgRestoreVerify`** using **only** `https://api.nuget.org/v3/index.json`, so transitive resolution must succeed on the public feed.

**Repository variables** (all optional except where noted):

| Variable | Purpose |
|----------|---------|
| **`NUGET_POST_PUSH_VERIFY`** | Set to **`false`** to skip steps 1–2 after push. |
| **`NUGET_POST_PUSH_VERIFY_PACKAGE_IDS`** | Comma-separated package ids for flat-container HEAD checks (default: `Nexo.Hosting.Bundle,Nexo.Hosting,Nexo.Sdk`). |
| **`NUGET_POST_PUSH_ATTEMPTS`** | Max poll rounds (default **12**; empty uses default). |
| **`NUGET_POST_PUSH_SLEEP_SEC`** | Seconds between rounds (default **15**; empty uses default). |

**Not atomic:** nuget.org still accepts packages one at a time; a failed push mid-loop can leave a **partial** set on the feed until you fix and re-run with **`--skip-duplicate`**.

## Operator checklist

See **`docs/RELEASE_RUNBOOK.md`** (decision table: tag vs NuGet-only vs branch images).
