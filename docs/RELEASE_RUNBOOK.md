# Release runbook (operator)

Which workflow do I run?

| Goal | Workflow | Notes |
|------|-----------|--------|
| **Ship everything** (GHCR images + NuGet) for a version | Push Git tag **`vX.Y.Z`** → **`.github/workflows/release.yml`** | Preferred. |
| **NuGet only** (e.g. package hotfix, no image retag) | **Actions → Release NuGet packages** → **`release-nuget.yml`** | Register **`release-nuget.yml`** for OIDC on nuget.org if you use Trusted Publishing here. |
| **Images from `main`** (rolling `sha-*`, not a semver release) | **`container-image-publish.yml`** on path-filtered pushes to default branch | No NuGet. |

Trusted Publishing on nuget.org is bound to the **top-level** workflow file: register **`release.yml`** for tag releases and **`release-nuget.yml`** if you use NuGet-only dispatch with OIDC. See `docs/PUBLISHING.md`.

## Before you tag

1. **Green CI on the commit you will ship** — run **`runtime-release-gate`** (and any other gates you rely on) on that ref; default branch green can omit path-filtered workflows.
2. **Hosting graph vs pack script** — `python3 scripts/verify-pack-nexo-hosting-graph-alignment.py` (also **Pack hosting graph alignment** CI). Rare extras go in **`scripts/pack-nexo-hosting-graph.allowlist.txt`** with a comment.
3. **Consumer sample (local feed)** — `bash scripts/verify-stable-sdk-host-sample-packages.sh` with `NEXO_SDK_PACKAGE_VERSION` (or `NEXO_SDK_PACKAGE_FEED` after packing).

## Cut the release

1. **Tag** `vX.Y.Z` on the chosen commit and **push the tag**.
2. **`release.yml`** runs images + NuGet per **`NUGET_PUBLISH_MODE`** (`none` | `oidc` | `apikey`).
3. **Secrets / variables** — `NUGET_USER` (OIDC), `NUGET_API_KEY` (apikey). Optional repo variables for post-push (see `docs/PUBLISHING.md`): **`NUGET_POST_PUSH_VERIFY`**, **`NUGET_POST_PUSH_VERIFY_PACKAGE_IDS`**, **`NUGET_POST_PUSH_ATTEMPTS`**, **`NUGET_POST_PUSH_SLEEP_SEC`**.

## After the workflow finishes

1. Workflow **Summary** for image digests and NuGet version.
2. nuget.org: confirm **`Nexo.Hosting.Bundle`** at **X.Y.Z**. CI may poll flat-container URLs for **Bundle + Hosting + Sdk** and run a **nuget.org-only** `dotnet restore` of `docs/samples/NugetOrgRestoreVerify` (validates transitive graph after index lag).
3. **GitHub Release** notes: version, migration pointers, `docs/SdkCompatibilityPolicy.md` if the HTTP surface changed.

## If something went wrong

- **NuGet partial push** — Pushes are per-package; fix and re-run with **`--skip-duplicate`**. Unlist bad versions on nuget.org per your policy.
- **Images only** — Branch pushes publish **`sha-*`** via `container-image-publish.yml`; semver image tags from **`release.yml`** on tags.
- **Forks** — The default **`GITHUB_TOKEN`** in a **fork** usually **cannot** push packages to **`ghcr.io/<upstream-owner>/...`**. Expect image publish jobs to **fail or skip** unless you run them in the **upstream** repo, retarget to your fork’s GHCR namespace, or use a **PAT** with `packages: write` and login steps that match your registry path. NuGet pushes from forks are also uncommon; use upstream for releases.
