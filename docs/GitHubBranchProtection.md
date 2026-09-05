# GitHub branch protection (release discipline)

Branch protection cannot run **`release.yml`** (that workflow is triggered by **tags**, not PR merges). Use it to keep **`master` / `main`** healthy so the commit you tag is already green.

## What `master` enforces today

The upstream `master` rule (verified 2026-08-16 via `gh api repos/IanFrelinger/Nexo/branches/master/protection`) requires **one** status check, **`cert-gate`**, with "require branches to be up to date" (`strict: true`) and `enforce_admins: true`. Every other gate — `testing-strategy`, `domain-coverage`, `kernel-coverage`, `layer-boundary / verify`, `Kernel Gate`, `Application Gate`, … — reports on PRs when its `paths:` filter matches but does **not** block a merge. The authoritative inventory is [`CiGateInventory.md`](CiGateInventory.md).

## Recommended rules for `master` (or `main`) — proposal, not the current setting

1. **Require a pull request** before merging (disable direct pushes if your team can tolerate it).
2. **Require status checks to pass** — keep **`cert-gate`** and add, once each gate always reports on PRs (see "Why the other gates are not required" in [`CiGateInventory.md`](CiGateInventory.md)):
   - **`testing-strategy`** — pivot policy (gap freeze, ProdStyle wiring hints); see [Testing strategy pivot v1](architecture/TestingStrategyPivot-v1.md)
   - **`kernel-coverage`** — composite floors as enforced by `scripts/ci/kernel-coverage-gate.sh` (Domain 100%, Infrastructure 80%, Application 67%)
   - **`layer-boundary / verify`** — already unfiltered (`paths: "**"`), the one gate that could be required today
   - Path-filtered gates as applicable: **Kernel Gate**, **Application Gate** (each needs an always-report job first)
   - **Cross-Platform Tests**, **Composition Mesh Gate** and **Mesh virtual lab gate** are `workflow_dispatch`-only and cannot be required as-is
3. **Require branches to be up to date** before merge (already on).
4. **Require conversation resolution** (optional, for review hygiene).

Full path → workflow map: [Testing strategy tracking v1](architecture/TestingStrategyTracking-v1.md).

## Release-specific checks

- Treat **`runtime-release-gate`** as a **manual or scheduled** quality bar before a big release (or wire it into your process): `dotnet run --project application/src/Ashlar.CLI -- release gate` after `gh auth login`.
- Run **`make rc-gate-full`** before tagging; see [RC readiness v1](production-readiness/RCReadiness-v1.md).
- The **tag** `v*.*.*` is the contract for **`release.yml`**; protect **`master`** so that tag usually points at a merged, reviewed commit.

## Forks

Contributors working in **forks** often cannot push **GHCR** packages to the upstream namespace with the default `GITHUB_TOKEN`. Releases that publish images or NuGet should run in the **upstream** repository (or document PAT-based publishing for maintainers).
