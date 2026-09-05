# Release runbook (operator)

## Fastest path (local, one command)

From repo root, with the **semver you are about to ship** (no `v`):

```bash
bash scripts/release-preflight-local.sh 1.2.3
# or:  make release-preflight VERSION=1.2.3
# or:  dotnet run --project application/src/Ashlar.CLI -- release preflight 1.2.3
```

That runs **pack-graph alignment** + **NuGet consumer sample** (isolated cache). Then push tag **`v1.2.3`** so **`release.yml`** runs (see table below).

Optional: also fire CI **`Runtime Release Gate`** from your machine (needs **`gh auth login`**):

```bash
ASHLAR_RELEASE_PREFLIGHT_TRIGGER_GATE=1 ASHLAR_RELEASE_PREFLIGHT_REF=master bash scripts/release-preflight-local.sh 1.2.3
# or:  dotnet run --project application/src/Ashlar.CLI -- release preflight 1.2.3 --trigger-gate --gate-ref master
# or anytime:  make release-gate   /   dotnet run --project application/src/Ashlar.CLI -- release gate
```

**Dispatch without a tag** (same workflow as tag, from a branch; needs `gh auth login`):

```bash
dotnet run --project application/src/Ashlar.CLI -- release dispatch 1.2.3 --ref master
# or:  make release-dispatch VERSION=1.2.3 REF=master
```

---

Which workflow do I run?

| Goal | Workflow | Notes |
|------|-----------|--------|
| **Ship everything** (GHCR + NuGet) | Push **`vX.Y.Z`** → **`release.yml`** | Preferred. Post-push NuGet checks + optional GHCR re-pull smoke. |
| **NuGet only** | **Actions → Release NuGet packages** → **`release-nuget.yml`** | Register **`release-nuget.yml`** for OIDC if you use it. |
| **Images `:latest` (operator)** | **Actions → Container Image Publish** (`container-image-publish.yml`) | Dispatch-only. A push to `master`/`main` does not publish. Versioned tags use **`release.yml`**. |

Trusted Publishing: register **`release.yml`** and **`release-nuget.yml`** as needed — see `docs/PUBLISHING.md`.

**Repo variables & branch protection:** `docs/GitHubRepoVariables.md`, `docs/GitHubBranchProtection.md`.

**Tracking:** open **New issue → Release checklist** (`.github/ISSUE_TEMPLATE/release_checklist.yml`) or use the **Release** section in the PR template when this PR ships a version.

## Before you tag

1. **Autonomous release-manager verdict is READY** on the exact commit:
   `make release-manager-audit`. It runs all six required audit lanes in
   isolated worktrees and never publishes; see
   [`AutonomousReleaseManager.md`](AutonomousReleaseManager.md).
2. **Green CI** on the commit — run **`runtime-release-gate`** on that ref.
3. **`python3 scripts/verify-pack-ashlar-hosting-graph-alignment.py`** after changing `Ashlar.Hosting` refs or pack scripts.
4. **`bash scripts/verify-stable-sdk-host-sample-packages.sh`** with `ASHLAR_SDK_PACKAGE_VERSION` (isolated cache + `--force-evaluate` by default).
5. **Promote the public API**: review each stable-tier project's `PublicAPI.Unshipped.txt`, move its lines into `PublicAPI.Shipped.txt`, commit on the release commit (`docs/SdkCompatibilityPolicy.md`, "Release step"). After the tag those lines are the promise.

## After `release.yml`

1. Workflow **Summary** — image `sha-*` tags, NuGet version, cross-verify status.
2. Artifact **`nuget-packages-<version>`** — includes **`nuget-publish-manifest.json`** and per-`.nupkg` **`.sha256.txt`** for audit / manual hash checks.
3. Optional **`nuget-sbom-<version>`** if **`NUGET_RELEASE_SBOM=true`** on the repo.

## If something went wrong

- **Partial NuGet push** — Re-run with **`--skip-duplicate`**; unlist bad versions per policy.
- **Forks** — Default **`GITHUB_TOKEN`** in forks often cannot push to **`ghcr.io/<upstream>/...`**; run releases in **upstream** or use a **PAT** with `packages: write`.
