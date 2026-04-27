# Release runbook (operator)

Which workflow do I run?

| Goal | Workflow | Notes |
|------|-----------|--------|
| **Ship everything** (GHCR + NuGet) | Push **`vX.Y.Z`** → **`release.yml`** | Preferred. Post-push NuGet checks + optional GHCR re-pull smoke. |
| **NuGet only** | **Actions → Release NuGet packages** → **`release-nuget.yml`** | Register **`release-nuget.yml`** for OIDC if you use it. |
| **Images from `main` only** | **`container-image-publish.yml`** | Rolling `sha-*` / `latest`; no NuGet. |

Trusted Publishing: register **`release.yml`** and **`release-nuget.yml`** as needed — see `docs/PUBLISHING.md`.

## Before you tag

1. **Green CI** on the commit — run **`runtime-release-gate`** on that ref.
2. **`python3 scripts/verify-pack-nexo-hosting-graph-alignment.py`** after changing `Nexo.Hosting` refs or pack scripts.
3. **`bash scripts/verify-stable-sdk-host-sample-packages.sh`** with `NEXO_SDK_PACKAGE_VERSION` (isolated cache + `--force-evaluate` by default).

## After `release.yml`

1. Workflow **Summary** — image `sha-*` tags, NuGet version, cross-verify status.
2. Artifact **`nuget-packages-<version>`** — includes **`nuget-publish-manifest.json`** and per-`.nupkg` **`.sha256.txt`** for audit / manual hash checks.
3. Optional **`nuget-sbom-<version>`** if **`NUGET_RELEASE_SBOM=true`** on the repo.

## If something went wrong

- **Partial NuGet push** — Re-run with **`--skip-duplicate`**; unlist bad versions per policy.
- **Forks** — Default **`GITHUB_TOKEN`** in forks often cannot push to **`ghcr.io/<upstream>/...`**; run releases in **upstream** or use a **PAT** with `packages: write`.
