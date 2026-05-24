# GitHub branch protection (release discipline)

Branch protection cannot run **`release.yml`** (that workflow is triggered by **tags**, not PR merges). Use it to keep **`master` / `main`** healthy so the commit you tag is already green.

## Recommended rules for `master` (or `main`)

1. **Require a pull request** before merging (disable direct pushes if your team can tolerate it).
2. **Require status checks to pass** — include at least:
   - Your default CI workflow(s) that run on every PR (for example **Cross-Platform Tests**, **Full Platform Readiness Gate**, or whatever your team treats as merge-blocking).
3. **Require branches to be up to date** before merge (optional but reduces “green PR on stale base”).
4. **Require conversation resolution** (optional, for review hygiene).

## Release-specific checks

- Treat **`runtime-release-gate`** as a **manual or scheduled** quality bar before a big release (or wire it into your process): `dotnet run --project application/src/Nexo.CLI -- release gate` after `gh auth login`.
- The **tag** `v*.*.*` is the contract for **`release.yml`**; protect **`master`** so that tag usually points at a merged, reviewed commit.

## Forks

Contributors working in **forks** often cannot push **GHCR** packages to the upstream namespace with the default `GITHUB_TOKEN`. Releases that publish images or NuGet should run in the **upstream** repository (or document PAT-based publishing for maintainers).
