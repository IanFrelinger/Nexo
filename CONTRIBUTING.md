# Contributing to Nexo

Thanks for contributing.

## Branching and releases

Development is trunk-based: `master` is the only long-lived branch, protected by required CI gates.

- Branch from the latest `master`, keep branches short-lived (days, not weeks), one concern per branch.
- Name branches `<type>/<topic>` using the same types as Conventional Commits: `feat/…`, `fix/…`, `docs/…`, `chore/…`, `ci/…`, `refactor/…`, `test/…`. For multi-PR efforts, put the epic name at the front of the topic so related branches sort together: `feat/trust-loop-hot-swap`, `feat/trust-loop-fence-probe`.
- Everything lands through a PR into `master`. Commit messages follow Conventional Commits (enforced by commitlint). Merged head branches are deleted automatically.
- Spikes that should not merge get an `archive/spike-<name>` tag on their tip, then the branch is deleted — the work stays reachable without cluttering the branch list.
- Releases are tags on `master`: bump `VERSION`, move the `[Unreleased]` notes in `CHANGELOG.md` under the new version heading, tag `vX.Y.Z`, and publish a GitHub Release (`scripts/changelog-snippet-for-release.sh` drafts the notes). Release branches (`release/x.y`) only appear if an old version ever needs long-term patch support.

## Recommended dev workflow (container + CLI)

1. **Docker** and **Git** on the host.
2. In **Cursor** or **VS Code**, install [Dev Containers](https://marketplace.visualstudio.com/items?itemName=ms-vscode-remote.remote-containers), open the repo, then **Dev Containers: Reopen in Container**. The first open runs `.devcontainer/post-create.sh` (setup-gate `dotnet restore` graph; not full `Nexo.sln`).
3. In the dev-container terminal, build and use the CLI:

```bash
dotnet build application/src/Nexo.CLI/Nexo.CLI.csproj --no-restore
dotnet run --project application/src/Nexo.CLI -- --help
```

**Remote:** connect over **Remote SSH**, open the repo on the remote machine, then **Reopen in Container** there so the toolchain comes from the image, not from manual packages on the host.

## Faster local restore (subset solution)

Full **`Nexo.sln`** is intended to build on **Linux** with a stock .NET SDK (no optional workloads). For a faster subset (**CLI** plus core tests), use the solution filter **`Nexo.LocalDevCore.slnf`**:

```bash
dotnet restore Nexo.LocalDevCore.slnf
dotnet build Nexo.LocalDevCore.slnf -v minimal
```

## Solution filters, Makefile targets, and CI

| Artifact | Typical use |
| -------- | ----------- |
| **`Nexo.sln`** | Full repository build — run locally after **`Nexo.Hosting`**, **`Nexo.Infrastructure`** Sdk surface, or registrar phase edits. |
| **`Nexo.LocalDevCore.slnf`** | Faster slice (CLI + core tests); **`make restore-core`** / **`make build-core`**. |
| **`Nexo.PrimeTime.slnf`** | Nine **`Nexo.Tests.*`** assemblies — **`make test-prime-time`** runs **`Category=ProdStyle`** across this filter; **`make test-prime-time-full`** runs the full test matrix after that gate. |

**Cross-platform workflow:** `.github/workflows/cross-platform-tests.yml` triggers on changes under **`src/Nexo.Infrastructure/**`** (among other paths) and runs **`dotnet restore`** / **`dotnet build`** on **`Nexo.sln`** (implicit via repo root). **Prod-shaped Compose:** `.github/workflows/prod-dry-run-pr.yml` runs **`scripts/prod-dry-run.sh`** on PRs to **`master`**, **`main`**, and **`cursor/**`** branches.

For Infrastructure Sdk / Hosting registration changes, prefer **`dotnet build Nexo.sln`** then **`make test-framework-prod-first`** or **`make test-prime-time`** when validating framework-wide behaviour (see also **`Makefile`** targets **`test-prod-style`**, **`ci-verify`**).

## Testing strategy

**North star:** [Testing strategy pivot v1](docs/architecture/TestingStrategyPivot-v1.md) — domain at 100% line coverage, ratcheted floors on Infrastructure/Application, **ProdStyle** for production wiring, mesh/RC gates for environment truth. Track progress: [Testing strategy tracking v1](docs/architecture/TestingStrategyTracking-v1.md).

**New kernel features** (bricks, barriers, pipelines, routing, `AddNexo` wiring): add at least one **`[Trait("Category", "ProdStyle")]`** or `WebApplicationFactory` test; do not rely on gap coverage alone.

**Do not add new `*GapCoverageTests.cs` files** without `gap-coverage-justify: <reason>` in the PR description (enforced by `testing-strategy-gate` CI).

**Before opening a PR:** `make testing-strategy-gate` (diff vs `origin/master`).

**Coverage:** `make kernel-coverage-gate` before PRs that touch `src/Nexo.Core.Domain`, `src/Nexo.Core.Application`, or `src/Nexo.Infrastructure`.

## Testing: xUnit vs. `UnitTestBase`

- **xUnit** suites (for example `Nexo.Tests.Infrastructure`) run with normal `dotnet test` filters.
- **`UnitTestBase`** tests are executed by **`ITestRunner`** / **`TestRunnerAdapter`** (same path as the CLI). **`UnitTestFrameworkBridge`** (in `Nexo.Infrastructure`) exposes **`UnitTestBridgeTests`** in **`Nexo.Tests.Domain`**, **`Nexo.Tests.Application`**, **`Nexo.Tests.Infrastructure`**, and **`Nexo.Tests.CLI`** so `dotnet test` on those projects runs most framework suites. A few types are skipped when they need a special layout or host (see `docs/architecture/TestingModel.md`).

High-level architecture notes: `docs/architecture/README.md`. SDK vs. `net8.0` / `net9.0`: `docs/architecture/DotnetVersions.md`.

## Prerequisites (native escape hatch)

Use this only when you cannot use the dev container or other Docker workflows:

- .NET SDK `9.x` (pinned by `global.json`). The CLI and API target `net8.0` and roll forward onto the 9.x runtime (`RollForward=Major` in `Directory.Build.targets`), so an SDK-9-only machine runs them without a separate .NET 8 runtime.
- Git
- Optional: Docker (for multi-environment and compose-based test lanes)

## Local setup (native)

From repository root:

```bash
# Linux/macOS: setup.sh forwards to setup-unix.sh (POSIX or -Mode flags)
bash scripts/setup/setup.sh all
dotnet build application/src/Nexo.CLI/Nexo.CLI.csproj --no-restore
```

Windows PowerShell:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\setup\setup.ps1 -Mode all
dotnet build application/src/Nexo.CLI/Nexo.CLI.csproj --no-restore
```

## Required pre-PR checks

Run the lanes CI actually runs on pull requests — the same scripts, filters and target frameworks — not `dotnet test Nexo.sln` (`make test`), which no CI lane executes and which drags in Docker/Ollama/GPU suites that only report as **Skipped** on a plain workstation:

```bash
bash scripts/ci/kernel-coverage-gate.sh   # kernel-coverage-gate.yml: Domain (100%), Infrastructure net9.0 (Category!=External), Core.Application coverage floors
bash scripts/run-cert-gate.sh             # cert-gate.yml: hermetic certification + generation-safety tests (net8.0)
make kernel-gate                          # kernel-gate.yml tier A (runtime graph build + hosting matrix + pipeline lifecycle)
make application-gate-tier-a              # application-gate.yml: product sln build + CLI smoke (tier-c for the in-process API tests)
make testing-strategy-gate                # testing-strategy-gate.yml: gap freeze / ProdStyle wiring rules on your diff
dotnet run --project application/src/Nexo.CLI -- ci verify   # `make ci-verify`: build + C#-driven checks
dotnet run --project application/src/Nexo.CLI -- pipeline validate --template <template.json>
```

Pick the lanes that match what you changed (`make testing-strategy-gate` prints the suggested set for your diff). Optional external suites (Docker, Ollama, Mapbox, mesh lab) are opt-in by environment variable and are reported as **Skipped** until enabled — see `docs/Testing.md`, "Opt-in external suites".

If you touch **`Nexo.Hosting`** project references or **`scripts/pack-nexo-hosting-graph.*`**, also run:

```bash
python3 scripts/verify-pack-nexo-hosting-graph-alignment.py
```

(Optional) Install [pre-commit](https://pre-commit.com/) once per clone, then hooks run on commit:

```bash
pip install pre-commit   # or brew install pre-commit
pre-commit install
```

For broader checks:

```bash
make ci-verify
```

If you change **`Nexo.Hosting`** project references or **`scripts/pack-nexo-hosting-graph.*`**:

```bash
python3 scripts/verify-pack-nexo-hosting-graph-alignment.py
```

Before a **versioned** NuGet/GHCR release:

```bash
dotnet run --project application/src/Nexo.CLI -- release preflight 1.2.3
# optional: trigger CI Release without a tag (needs gh auth)
dotnet run --project application/src/Nexo.CLI -- release dispatch 1.2.3 --ref master
```

Optional [pre-commit](https://pre-commit.com/): `pip install pre-commit && pre-commit install` then use `.pre-commit-config.yaml` (graph alignment hook).

## Command style and paths in docs

- Prefer **`dotnet run --project application/src/Nexo.CLI -- <subcommand>`** so commands work without a global `nexo` tool. The CLI project lives only under **`application/src/Nexo.CLI`** (not `src/Nexo.CLI`).
- For **`Nexo.API`**, prefer **`dotnet run --project application/src/Nexo.API`** (see **`docs/architecture/runtime-vs-application.md`**).
- If using `nexo <subcommand>`, include a `dotnet run --project application/src/Nexo.CLI -- …` equivalent when the audience is contributors cloning the repo.
- Canonical doc index: **`docs/DocsIndex.md`**.

## Cross-platform CI trigger

Use the repository default branch for refs in manual workflow triggers:

```bash
gh workflow run "Cross-Platform Tests" --ref master -f scope=smoke
```

If your fork uses a different default branch, replace `master` accordingly.

**Branch protection:** If required checks reference workflows that only run on manual dispatch,
update protection rules or add an always-on check. See `.github/workflows/README.md`.

## Resource safety

- Run heavy validations sequentially (avoid parallel `dotnet test`/`dogfood` runs in multiple terminals).
- Avoid kicking off **two full `dotnet build` / `dotnet test` runs at the same time** on the same clone: MSBuild can hit file locks (for example `*.deps.json` under `bin`/`obj`, including projects like `Nexo.BackgroundAgents.HostRunners`). Prefer **one** restore/build, then **`dotnet test --no-build`** in other terminals, or run test projects one after another.
- Use `--blame-hang-dump-type none` for local test loops to avoid very large dump files.
