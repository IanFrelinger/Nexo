# Staging NuGet feed (optional, before nuget.org)

Push packages to a **non–nuget.org** feed first, run your own checks, then let **`reusable-release-nuget.yml`** push to **nuget.org** in the same job (staging failure blocks the public push).

## Configure

**Repository variable** (Actions → Variables):

| Variable | Example | Purpose |
|----------|---------|---------|
| **`NUGET_STAGING_FEED_URL`** | `https://nuget.pkg.github.com/YourOrg/index.json` or Azure Artifacts URL | Second push target **before** nuget.org |

**Repository secret:**

| Secret | Purpose |
|--------|---------|
| **`NUGET_STAGING_API_KEY`** | PAT or token with **push** to that feed (GitHub Packages: PAT with `write:packages`; Azure: feed-specific PAT). |

If **`NUGET_STAGING_FEED_URL`** is set but the secret is missing, the workflow **fails** at the staging step (fail-safe).

## Flow

1. Pack + manifest + local sample verify (unchanged).
2. **`dotnet nuget push`** all `*.nupkg` to **`NUGET_STAGING_FEED_URL`** using **`NUGET_STAGING_API_KEY`**.
3. Push to **nuget.org** (OIDC or `NUGET_API_KEY` per `NUGET_PUBLISH_MODE`).
4. Post-push verification against **nuget.org** (unchanged).

Leave **`NUGET_STAGING_FEED_URL`** unset to skip staging entirely.

## GitHub Packages example

- URL: `https://nuget.pkg.github.com/<OWNER_OR_ORG>/index.json`
- Auth: GitHub PAT with **`write:packages`** (and `read:packages` if restoring elsewhere) as the “API key” for `dotnet nuget push`.

## Azure Artifacts example

Create a feed, generate a PAT with **Packaging → Read & write**, set the feed’s **v3** URL as `NUGET_STAGING_FEED_URL`.
