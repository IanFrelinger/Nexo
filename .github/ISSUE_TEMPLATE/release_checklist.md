---
name: Release checklist
about: Track a versioned ship (NuGet + GHCR) with the minimum operator steps
title: "Release: vX.Y.Z"
---

## Preflight (local)

- [ ] `bash scripts/release-preflight-local.sh X.Y.Z` **or** `make release-preflight VERSION=X.Y.Z` **or** `dotnet run --project src/Nexo.CLI -- release preflight X.Y.Z`
- [ ] (Optional) `dotnet run --project src/Nexo.CLI -- release gate` **or** `make release-gate` — triggers **Runtime Release Gate** on GitHub (needs `gh auth login`)

## GitHub (one-time / per org)

- [ ] **Trusted Publishing** on nuget.org for **`release.yml`** (and **`release-nuget.yml`** if you use NuGet-only OIDC) — `docs/PUBLISHING.md`
- [ ] Repo **Variables**: `NUGET_PUBLISH_MODE` (`none` | `oidc` | `apikey`); optional `NUGET_POST_PUSH_VERIFY`, `NUGET_RELEASE_SBOM`, etc. — `docs/GitHubRepoVariables.md`
- [ ] **Secrets**: `NUGET_USER` (OIDC), `NUGET_API_KEY` (if apikey mode)

## Ship

- [ ] Push tag **`vX.Y.Z`** on the release commit → **`.github/workflows/release.yml`**
- [ ] Confirm workflow **Summary** + artifacts **`nuget-packages-X.Y.Z`**
- [ ] Publish **GitHub Release** notes (link `docs/SdkCompatibilityPolicy.md` if HTTP surface changed)

## Post-ship

- [ ] Spot-check nuget.org packages at **X.Y.Z**
- [ ] (Optional) `ghcr.io/<owner>/nexo-cli:sha-…` / `nexo-api:sha-…` from the run Summary
