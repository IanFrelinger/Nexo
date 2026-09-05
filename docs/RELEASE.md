# Release (start here)

One page that points to everything you need to ship **NuGet + GHCR** from this repo.

## Do this (happy path)

1. **Local preflight** — `bash scripts/release-preflight-local.sh X.Y.Z` or `dotnet run --project application/src/Ashlar.CLI -- release preflight X.Y.Z`
2. **Trigger CI release** (optional instead of tag) — `dotnet run --project application/src/Ashlar.CLI -- release dispatch X.Y.Z --ref master` (needs `gh auth login`)
3. **Ship** — push **`vX.Y.Z`** on the cut commit (runs **`.github/workflows/release.yml`**). Images/NuGet start only after the autonomous release manager verdict is **READY** for that SHA.
4. **Track** — open a **Release checklist** issue (GitHub → New issue)

## Deep links

| Topic | Doc |
|--------|-----|
| Which workflow, fork notes, after-tag checks | **`docs/RELEASE_RUNBOOK.md`** |
| NuGet pack, push modes, post-push verification, SBOM | **`docs/PUBLISHING.md`** |
| GitHub Actions **variables** / secrets for release | **`docs/GitHubRepoVariables.md`** |
| **Branch protection** (merge vs tag) | **`docs/GitHubBranchProtection.md`** |
| **Staging feed** before nuget.org (optional) | **`docs/StagingFeed.md`** |
| **Signing** packages (optional) | **`docs/NuGetPackageSigning.md`** |
| Deploy / compose / images | **`docs/DEPLOYMENT.md`** |

## CLI shortcuts

```text
dotnet run --project application/src/Ashlar.CLI -- release preflight <semver>
dotnet run --project application/src/Ashlar.CLI -- release dispatch <semver> [--ref branch] [--skip-multi-arch]
dotnet run --project application/src/Ashlar.CLI -- release gate [--ref branch]
```

## Automation you get from `release.yml` (tag push)

- GHCR **`nexo-cli`** / **`nexo-api`** with **`sha-*`** (+ semver `X.Y.Z` on tags — a retag of the smoke-tested `sha-*` digest, multi-arch for `nexo-cli`)
- NuGet pack/push per **`NUGET_PUBLISH_MODE`**
- **`validate`** job: **`global.json`** SDK pin vs installed SDKs (`scripts/verify-release-sdk-pin.sh`); on tags, the tag version must equal the root **`VERSION`** file (`assert_version_matches_canonical`)
- Optional **draft GitHub Release** with recent commits (`RELEASE_CREATE_GITHUB_RELEASE`; body includes `scripts/changelog-snippet-for-release.sh` output) with the `.nupkg` / `.snupkg` files, `nuget-publish-manifest.json` and per-package `.sha256.txt` attached as assets
- Optional **webhook** JSON POST (`RELEASE_NOTIFICATION_WEBHOOK_URL` secret)
- Optional **staging feed** push before nuget.org (`docs/StagingFeed.md`)

**Local changelog helper:** `bash scripts/changelog-snippet-for-release.sh` — paste into release notes.
