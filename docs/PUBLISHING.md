# Publishing Nexo for external consumption

This document describes how to **produce** and **publish** the .NET packages that external repos (for example game tooling) consume. CI already **verifies** NuGet-only consumption locally; publishing to a feed is an operator step.

**Minimal local preflight (one command):** `bash scripts/release-preflight-local.sh X.Y.Z`, `make release-preflight VERSION=X.Y.Z`, or `dotnet run --project src/Nexo.CLI -- release preflight X.Y.Z` — then push **`vX.Y.Z`** for **`release.yml`** (or **`release dispatch`** / **`make release-dispatch`**). Hub: **`docs/RELEASE.md`**. Checklist: **`docs/RELEASE_RUNBOOK.md`**. GitHub **variables**: **`docs/GitHubRepoVariables.md`**.

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

This packs the graph to `artifacts/nuget-verify/packages`, restores `docs/samples/StableSdkHostSample/package-consumer/` against **only** that folder + nuget.org, builds, and runs the sample. Restore uses **`--force-evaluate`** and, by default, an **empty `NUGET_PACKAGES` + `DOTNET_CLI_HOME`** under `artifacts/nuget-verify/isolated-*` so a stale user/global cache cannot mask a bad graph (set **`NEXO_SDK_VERIFY_NO_ISOLATED_CACHE=1`** to opt out; **`NEXO_SDK_VERIFY_ISOLATED_ROOT`** to reuse a fixed directory).

To verify an **unpacked** CI artifact folder without re-packing:

```bash
export NEXO_SDK_PACKAGE_VERSION=1.2.3
export NEXO_SDK_PACKAGE_FEED=/path/to/unpacked/nuget-packages
bash scripts/verify-stable-sdk-host-sample-packages.sh
```

**Local manifest (optional):** after packing a folder, `PACKAGE_VERSION=1.2.3 bash scripts/render-nuget-release-manifest.sh ./artifacts/nuget-release` writes **`nuget-publish-manifest.json`** plus one **`.sha256.txt`** per `.nupkg`.

**After packages are on nuget.org (or a private feed):** use `scripts/verify-stable-sdk-host-sample-published-feed.sh` — see **`docs/NuGetConsumerVerify.md`** and workflow **`.github/workflows/nuget-consumer-verify.yml`**.

## Publish to nuget.org (you do this)

### Option A — GitHub Actions (recommended)

Workflows:

- **`.github/workflows/release.yml`** — **tag `v*.*.*`**: GHCR **and** NuGet in one run. Configure Trusted Publishing for workflow file **`release.yml`** (filename only). After a successful push to nuget.org, runs **Verify NuGet consumer** (shared with **release-nuget**).
- **`.github/workflows/release-nuget.yml`** — **manual NuGet-only** dispatch (version input). After push to nuget.org, runs the same **Verify NuGet consumer** job when **`NUGET_PUBLISH_MODE`** is **`oidc`** or **`apikey`**.

**Trusted Publishing (OIDC)** on nuget.org is bound to the **caller** workflow file, not the reusable `reusable-release-nuget.yml`. Register every entry point you use:

| If you publish via | Register this workflow file on nuget.org |
|--------------------|---------------------------------------------|
| Tag push → **Release** | **`release.yml`** |
| **Actions → Release NuGet packages** with `NUGET_PUBLISH_MODE=oidc` | **`release-nuget.yml`** |

If you only ever use tag releases, **`release.yml`** alone is enough. If operators also run **`release-nuget.yml`** with OIDC, add a second Trusted Publishing policy (or equivalent) for **`release-nuget.yml`** or those pushes will be denied.

1. **Repository variable** `NUGET_PUBLISH_MODE`:
   - unset, empty, or **`none`** — pack + verify + artifact only; download **`nuget-packages-<version>`** and push manually if desired.
   - **`oidc`** — [Trusted Publishing](https://learn.microsoft.com/nuget/nuget-org/trusted-publishing) for the workflow files above. Secret **`NUGET_USER`** = nuget.org **profile name** (not email).
   - **`apikey`** — secret **`NUGET_API_KEY`**.
2. **Trigger:** push tag **`v1.2.3`** (preferred), or **Actions → Release** / **Release NuGet packages** for partial flows.
3. **After push to nuget.org:** **`release.yml`** runs **Verify NuGet consumer (nuget.org)** when **`NUGET_PUBLISH_MODE`** is **`oidc`** or **`apikey`** (restores the sample from nuget.org only, with retries for index lag). See **`docs/NuGetConsumerVerify.md`**.
4. Write **GitHub Release** notes and verify packages on nuget.org.

### Post-push verification (`reusable-release-nuget.yml`)

When **`NUGET_PUBLISH_MODE`** is **`oidc`** or **`apikey`** (and **`NUGET_POST_PUSH_VERIFY`** is not **`false`**), the reusable workflow:

1. **Flat-container** — `scripts/verify-nuget-org-packages-visible.sh` (HEAD on `.nupkg` URLs).
2. **Registration API** — `scripts/verify-nuget-org-registration-versions.sh` (version listed in `registration5-gz-semver2`).
3. **Byte match** — `scripts/verify-nuget-published-sha256-matches-manifest.sh` downloads each package from nuget.org and checks **SHA-256** against **`nuget-publish-manifest.json`** produced at pack time (artifact includes manifest + `.sha256.txt` files).
4. **Restore (bundle)** — `scripts/verify-nuget-org-restore-with-isolated-cache.sh` — `Nexo.Hosting.Bundle` on nuget.org only, **isolated** package cache.
5. **Restore (hosting only)** — `scripts/verify-nuget-org-restore-hosting-only-isolated.sh` — direct **`Nexo.Hosting`** reference (second consumer path).

**Repository variables** (optional):

| Variable | Purpose |
|----------|---------|
| **`NUGET_POST_PUSH_VERIFY`** | Set to **`false`** to skip steps 1–5. |
| **`NUGET_POST_PUSH_VERIFY_PACKAGE_IDS`** | Comma-separated ids for steps 1–2 (default: `Nexo.Hosting.Bundle,Nexo.Hosting,Nexo.Sdk`). |
| **`NUGET_POST_PUSH_ATTEMPTS`** / **`NUGET_POST_PUSH_SLEEP_SEC`** | Poll tuning (empty uses defaults in scripts). |
| **`NUGET_RELEASE_SBOM`** | Set to **`true`** to generate SPDX JSON per `.nupkg` with **Syft** and upload artifact **`nuget-sbom-<version>`**. |
| **`NUGET_RELEASE_GRYPE`** | With **`NUGET_RELEASE_SBOM`**, run **Grype** on each SBOM (reports only; **`continue-on-error`** so vuln data does not fail the release). |
| **`RELEASE_CROSS_VERIFY`** | Set to **`false`** to skip **`release.yml`** job that re-pulls GHCR **`sha-*`** images and runs **`scripts/release-smoke-published-docker.sh`**. |
| **`NUGET_STAGING_FEED_URL`** | Staging v3 feed URL | Push **before** nuget.org; requires secret **`NUGET_STAGING_API_KEY`** — `docs/StagingFeed.md`. |
| **`RELEASE_CREATE_GITHUB_RELEASE`** | Set **`false`** to skip **draft GitHub Release** on tag runs. |

Webhook: set secret **`RELEASE_NOTIFICATION_WEBHOOK_URL`** (not a variable) — `docs/GitHubRepoVariables.md`.

**Not atomic:** nuget.org accepts packages one-by-one; a mid-run failure can leave a **partial** set until you fix and re-push with **`--skip-duplicate`**.

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

The CLI image is published by `.github/workflows/container-image-publish.yml` to **GHCR**. Tag releases also build images via **`release.yml`**. **`reusable-container-publish.yml`** smoke-tests **nexo-cli** (`--help`) and **nexo-api** (`/health`) on the **immutable `sha-*`** image after push.

## What you must maintain over time

- When `Nexo.Hosting` gains a **new project reference** to another in-repo `Nexo.*` project, add that project to **`scripts/pack-nexo-hosting-graph.sh`** / **`.ps1`**. CI runs **`python3 scripts/verify-pack-nexo-hosting-graph-alignment.py`** (workflow **`pack-hosting-graph-alignment.yml`**). Rare extras: **`scripts/pack-nexo-hosting-graph.allowlist.txt`**.
- Keep **`PackageVersion`** in sync across the graph for a given release (the scripts pass one version to every `dotnet pack`).

## Operator checklist

See **`docs/RELEASE_RUNBOOK.md`** for the release decision table (tag vs NuGet-only vs branch images).
