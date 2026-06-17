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

## Pull the trigger (one command)

After one-time bootstrap (below), cut a **staging-only** release and verify the external product shape against the staging feed:

```bash
make release-staging-and-verify VERSION=0.1.0
```

That chains:

1. **`make release-staging`** — guarded `gh workflow run release.yml` (staging push only when `NUGET_PUBLISH_MODE` is unset/`none`; **hard-aborts** if `oidc` or `apikey`).
2. **`make verify-staging`** — round-trip `scripts/verify-external-product-shape-published.sh` against `NUGET_STAGING_FEED_URL` using **`NUGET_STAGING_READ_TOKEN`** from your shell (never committed).

Dry-run the guards without dispatching:

```bash
make release-staging DRY_RUN=1 VERSION=0.1.0
```

Canonical version: create a **`VERSION`** file at the repo root before a real cut; `release-staging` refuses a mismatch so `release.yml` cannot fail its tag/input check later.

### One-time bootstrap (`gh` uses your ambient auth; tokens stay in GitHub / your shell)

```bash
# Staging feed URL (repository variable)
gh variable set NUGET_STAGING_FEED_URL --body 'https://nuget.pkg.github.com/YOUR_ORG/index.json'

# Push token for reusable-release-nuget.yml (repository secret)
gh secret set NUGET_STAGING_API_KEY

# Read token for local verify-staging (your shell only — not stored in the repo)
export NUGET_STAGING_READ_TOKEN='ghp_...'

# Ensure nuget.org push stays OFF for staging cuts
gh variable set NUGET_PUBLISH_MODE --body 'none'   # or leave unset

# Optional: pin the version you are about to ship
echo '0.1.0' > VERSION
git add VERSION && git commit -m 'chore(release): pin VERSION 0.1.0'
```

**Promotion to nuget.org** is a separate deliberate step: set **`NUGET_PUBLISH_MODE=apikey`** (or `oidc`), configure **`NUGET_API_KEY`** / **`NUGET_USER`**, then dispatch **`release.yml`** or push a tag — not via `release-staging`.

Optional label trigger: add the **`release:staging`** label to a pull request (workflow **`.github/workflows/release-staging-on-label.yml`**) after the same variables are configured.

## GitHub Packages example

- URL: `https://nuget.pkg.github.com/<OWNER_OR_ORG>/index.json`
- Auth: GitHub PAT with **`write:packages`** (and `read:packages` if restoring elsewhere) as the “API key” for `dotnet nuget push`.

## Azure Artifacts example

Create a feed, generate a PAT with **Packaging → Read & write**, set the feed’s **v3** URL as `NUGET_STAGING_FEED_URL`.
