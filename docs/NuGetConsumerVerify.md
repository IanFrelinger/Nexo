# NuGet consumer verification

Validates that **`Nexo.Hosting.Bundle`** (and its graph) can be **restored, built, and run** the way an external host app would—without referencing the Nexo source tree as project references.

## When to run

| Scenario | Command |
| -------- | ------- |
| **Pre-push / CI** (local `.nupkg` folder + nuget.org for third-party deps) | `NEXO_SDK_PACKAGE_VERSION=1.2.3 bash scripts/verify-stable-sdk-host-sample-packages.sh` |
| **After publish** (packages already on a feed—nuget.org or private) | `bash scripts/verify-stable-sdk-host-sample-published-feed.sh 1.2.3` |

The sample project is `docs/samples/StableSdkHostSample/package-consumer/StableSdkHostSample.Package.csproj` (single `PackageReference` to **`Nexo.Hosting.Bundle`**).

## Published feed (post-deploy)

```bash
bash scripts/verify-stable-sdk-host-sample-published-feed.sh 1.2.3
# default feed: https://api.nuget.org/v3/index.json

bash scripts/verify-stable-sdk-host-sample-published-feed.sh 1.2.3 https://nuget.pkg.github.com/OWNER/index.json
```

### Private feed (GitHub Packages, Azure Artifacts, etc.)

Set credentials so `dotnet restore` can read the feed:

```bash
export NEXO_NUGET_USERNAME="your-user-or-ORG"
export NEXO_NUGET_PASSWORD="PAT-or-token"
bash scripts/verify-stable-sdk-host-sample-published-feed.sh 1.2.3 "https://nuget.pkg.github.com/OWNER/index.json"
```

Optional: `NEXO_VERIFY_SOURCE_KEY=myfeed` renames the `<packageSources>` key (default `published`).

Windows:

```powershell
$env:NEXO_NUGET_USERNAME = "..."
$env:NEXO_NUGET_PASSWORD = "..."
pwsh -NoProfile -File scripts/verify-stable-sdk-host-sample-published-feed.ps1 -Version 1.2.3 -FeedUrl "https://..."
```

## Automation in GitHub Actions

Workflow **`.github/workflows/release.yml`** runs **`verify-nuget-consumer`** after **`NUGET_PUBLISH_MODE`** is **`oidc`** or **`apikey`** (packages were pushed to nuget.org): it restores the sample from **nuget.org only** with retries for index lag. The same step is reused from **`.github/workflows/release-nuget.yml`** (manual NuGet-only) via **`.github/workflows/reusable-verify-nuget-consumer.yml`**.

Workflow **`.github/workflows/nuget-consumer-verify.yml`** (standalone):

- **`workflow_dispatch`**: input **version** (e.g. `1.2.3`); verifies against **nuget.org** only (no secrets).

For private feeds, add a manual workflow in your org that sets `NEXO_NUGET_USERNAME` / `NEXO_NUGET_PASSWORD` from secrets and calls the same script with your feed URL.

## Related

- `docs/PUBLISHING.md` — pack graph and pre-push verify.
- `scripts/verify-stable-sdk-host-sample-packages.sh` — local pack + consumer.
