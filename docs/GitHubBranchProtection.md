# GitHub branch protection (release discipline)

Branch protection cannot run **`release.yml`** (that workflow is triggered by **tags**, not PR merges). Use it to keep **`master` / `main`** healthy so the commit you tag is already green.

## Recommended rules for `master` (or `main`)

1. **Require a pull request** before merging (disable direct pushes if your team can tolerate it).
2. **Require status checks to pass** — include at least:
   - **`testing-strategy`** — pivot policy (gap freeze, ProdStyle wiring hints); see [Testing strategy pivot v1](architecture/TestingStrategyPivot-v1.md)
   - **`domain-coverage`** — `Nexo.Core.Domain` line coverage **100%**
   - **`kernel-coverage`** — composite floors (Domain 100%, Infrastructure 83%, Application 67%)
   - Your default CI workflow(s) on every PR (for example **Cross-Platform Tests**, or team merge-blocking workflows)
   - Path-filtered gates as applicable: **Kernel Gate**, **Application Gate**, **Composition Mesh Gate**, **Mesh virtual lab gate**
3. **Require branches to be up to date** before merge (optional but reduces “green PR on stale base”).
4. **Require conversation resolution** (optional, for review hygiene).

Full path → workflow map: [Testing strategy tracking v1](architecture/TestingStrategyTracking-v1.md).

## Release-specific checks

- Treat **`runtime-release-gate`** as a **manual or scheduled** quality bar before a big release (or wire it into your process): `dotnet run --project application/src/Nexo.CLI -- release gate` after `gh auth login`.
- Run **`make rc-gate-full`** before tagging; see [RC readiness v1](production-readiness/RCReadiness-v1.md).
- The **tag** `v*.*.*` is the contract for **`release.yml`**; protect **`master`** so that tag usually points at a merged, reviewed commit.

## Forks

Contributors working in **forks** often cannot push **GHCR** packages to the upstream namespace with the default `GITHUB_TOKEN`. Releases that publish images or NuGet should run in the **upstream** repository (or document PAT-based publishing for maintainers).
