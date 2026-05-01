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

Workflow **`.github/workflows/nuget-consumer-verify.yml`**:

- **`workflow_dispatch`**: input **version** (e.g. `1.2.3`); verifies against **nuget.org** only (no secrets).
- **`schedule`**: weekly smoke against a repo variable **`NEXO_VERIFY_NUGET_VERSION`** (set in **Settings → Variables**). If unset, the job skips so scheduled runs stay quiet.

For private feeds, add a scheduled or manual workflow in your org that sets `NEXO_NUGET_USERNAME` / `NEXO_NUGET_PASSWORD` from secrets and calls the same script with your feed URL.

## Related

- `docs/PUBLISHING.md` — pack graph and pre-push verify.
- `scripts/verify-stable-sdk-host-sample-packages.sh` — local pack + consumer.
