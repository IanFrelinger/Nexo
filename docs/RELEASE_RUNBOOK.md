# Release runbook (operator)

Short checklist for cutting a Nexo release (GHCR images + NuGet). Details live in `docs/PUBLISHING.md` and `docs/DEPLOYMENT.md`.

## Before you tag

1. **Green CI on the commit you will ship** — run **`runtime-release-gate`** (and any other gates you rely on) on that ref; default branch green can omit path-filtered workflows.
2. **Hosting graph vs pack script** — CI runs `python3 scripts/verify-pack-nexo-hosting-graph-alignment.py`. Locally: same command after adding any `ProjectReference` under `Nexo.Hosting`.
3. **Consumer sample** — `bash scripts/verify-stable-sdk-host-sample-packages.sh` with `NEXO_SDK_PACKAGE_VERSION` set to your target semver (or use `NEXO_SDK_PACKAGE_FEED` after packing).

## Cut the release

1. **Tag** `vX.Y.Z` on the chosen commit and **push the tag**.
2. **`.github/workflows/release.yml`** runs: GHCR images (`nexo-cli`, `nexo-api`, optional quickstart) and NuGet pack/push per **`NUGET_PUBLISH_MODE`**.
3. **Trusted Publishing** — register **`release.yml`** on nuget.org for OIDC tag releases; register **`release-nuget.yml`** too if you use NuGet-only dispatch with OIDC (`docs/PUBLISHING.md`).
4. **Secrets / variables** — `NUGET_USER` for OIDC; `NUGET_API_KEY` for apikey mode; repo variable **`NUGET_PUBLISH_MODE`**: `none` | `oidc` | `apikey`. Optional: set **`NUGET_POST_PUSH_VERIFY`** to `false` to skip the post-push nuget.org visibility poll in CI.

## After the workflow finishes

1. Open the workflow **Summary** for image digests and NuGet version lines.
2. On nuget.org, confirm **`Nexo.Hosting.Bundle`** (and siblings) at **X.Y.Z**. CI may poll the flat container until the version appears (index lag).
3. **GitHub Release** notes: version, migration pointers, and `docs/SdkCompatibilityPolicy.md` if the HTTP surface changed.

## If something went wrong

- **NuGet partial push** — Pushes are per-package; fix the root cause and re-run with **`--skip-duplicate`** (CI already uses it). Unlist bad versions on nuget.org per your policy.
- **Images only** — Branch pushes still publish **`sha-*`** via `container-image-publish.yml`; semver tags on GHCR come from **tag** runs via `release.yml`.
- **Forks** — GHCR publish with default `GITHUB_TOKEN` often fails on forks; run release workflows in the upstream repo.
