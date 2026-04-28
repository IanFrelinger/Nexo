# Contributing to Nexo

Thanks for contributing.

## Recommended dev workflow (container + CLI)

1. **Docker** and **Git** on the host.
2. In **Cursor** or **VS Code**, install [Dev Containers](https://marketplace.visualstudio.com/items?itemName=ms-vscode-remote.remote-containers), open the repo, then **Dev Containers: Reopen in Container**. The first open runs `.devcontainer/post-create.sh` (setup-gate `dotnet restore` graph; not full `Nexo.sln`).
3. In the dev-container terminal, build and use the CLI:

```bash
dotnet build src/Nexo.CLI/Nexo.CLI.csproj --no-restore
dotnet run --project src/Nexo.CLI -- --help
```

**Remote:** connect over **Remote SSH**, open the repo on the remote machine, then **Reopen in Container** there so the toolchain comes from the image, not from manual packages on the host.

## Prerequisites (native escape hatch)

Use this only when you cannot use the dev container or other Docker workflows:

- .NET SDK `9.x` (pinned by `global.json`)
- Git
- Optional: Docker (for multi-environment and compose-based test lanes)

## Local setup (native)

From repository root:

```bash
# Linux/macOS: setup.sh forwards to setup-unix.sh (POSIX or -Mode flags)
bash scripts/setup/setup.sh all
dotnet build src/Nexo.CLI/Nexo.CLI.csproj --no-restore
```

Windows PowerShell:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\setup\setup.ps1 -Mode all
dotnet build src/Nexo.CLI/Nexo.CLI.csproj --no-restore
```

## Required pre-PR checks

Run the minimal local quality bar:

```bash
make test
dotnet run --project src/Nexo.CLI -- --help
dotnet run --project src/Nexo.CLI -- pipeline validate --template <template.json>
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
dotnet run --project src/Nexo.CLI -- release preflight 1.2.3
# optional: trigger CI Release without a tag (needs gh auth)
dotnet run --project src/Nexo.CLI -- release dispatch 1.2.3 --ref master
```

Optional [pre-commit](https://pre-commit.com/): `pip install pre-commit && pre-commit install` then use `.pre-commit-config.yaml` (graph alignment hook).

## Command style in docs

- Prefer `dotnet run --project src/Nexo.CLI -- <subcommand>` in docs so commands work without global tool installation.
- If using `nexo <subcommand>`, make sure the doc also includes a `dotnet run` equivalent.

## Cross-platform CI trigger

Use the repository default branch for refs in manual workflow triggers:

```bash
gh workflow run "Cross-Platform Tests" --ref master -f scope=smoke
```

If your fork uses a different default branch, replace `master` accordingly.

## Resource safety

- Run heavy validations sequentially (avoid parallel `dotnet test`/`dogfood` runs in multiple terminals).
- Avoid kicking off **two full `dotnet build` / `dotnet test` runs at the same time** on the same clone: MSBuild can hit file locks (for example `*.deps.json` under `bin`/`obj`, including projects like `Nexo.BackgroundAgents.HostRunners`). Prefer **one** restore/build, then **`dotnet test --no-build`** in other terminals, or run test projects one after another.
- Use `--blame-hang-dump-type none` for local test loops to avoid very large dump files.
