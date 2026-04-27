# GitHub repository variables (release / NuGet)

Set these under **Repository → Settings → Secrets and variables → Actions → Variables** (not secrets unless noted). Use the same names on **forks** only if you publish from the fork; upstream releases usually set them on the **main** repo.

| Variable | Values | When |
|----------|--------|------|
| **`NUGET_PUBLISH_MODE`** | `none`, `oidc`, `apikey` | Controls whether **`reusable-release-nuget.yml`** pushes to nuget.org. `none` = artifact only. |
| **`NUGET_POST_PUSH_VERIFY`** | unset / `true` / `false` | Set **`false`** to skip post-push flat-container, registration, SHA256 download check, and nuget.org-only restores. |
| **`NUGET_POST_PUSH_VERIFY_PACKAGE_IDS`** | e.g. `Nexo.Hosting.Bundle,Nexo.Hosting,Nexo.Sdk` | Comma list for visibility + registration polls (default if empty). |
| **`NUGET_POST_PUSH_ATTEMPTS`** | e.g. `12` | Poll rounds (empty = script default). |
| **`NUGET_POST_PUSH_SLEEP_SEC`** | e.g. `15` | Seconds between polls. |
| **`NUGET_RELEASE_SBOM`** | `true` / unset | When **`true`**, Syft generates SPDX JSON per `.nupkg` and uploads **`nuget-sbom-<version>`**. |
| **`NUGET_RELEASE_GRYPE`** | `true` / unset | With SBOM, runs Grype (non-blocking for the workflow). |
| **`RELEASE_CROSS_VERIFY`** | unset / `true` / `false` | Set **`false`** to skip **`release.yml`** job that re-pulls GHCR **`sha-*`** images for CLI/API smoke. |

**Secrets** (same settings page → Secrets):

| Secret | Purpose |
|--------|---------|
| **`NUGET_USER`** | nuget.org **profile name** (OIDC + `NuGet/login@v1`). |
| **`NUGET_API_KEY`** | Push when `NUGET_PUBLISH_MODE=apikey`. |

## Org / new repository template

For **new repos** under your GitHub org, add a **repository template** or **org variable** documentation so teams copy the table above. GitHub does not inherit variables from org to child repos automatically for arbitrary names; document the required set in onboarding or a parent “platform ops” repo.
