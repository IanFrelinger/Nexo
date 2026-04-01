# Contributing to Nexo

Thanks for contributing.

## Prerequisites

- .NET SDK `9.x` (pinned by `global.json`)
- Git
- Optional: Docker (for multi-environment and compose-based test lanes)

## Local setup

From repository root:

```bash
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
- Use `--blame-hang-dump-type none` for local test loops to avoid very large dump files.
