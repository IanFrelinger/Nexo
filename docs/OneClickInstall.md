# One-Shot Installers

Use these wrappers when you want a single command to bootstrap Nexo.

## What these wrappers automate

For Linux, macOS, and Windows wrappers under `scripts/install/`:

1. Clone or update the Nexo repo.
2. Run setup dependency/install flow (`scripts/setup/setup.* apply`).
3. Run targeted restore (`scripts/setup/setup.* restore`).
4. Build CLI smoke target (`src/Nexo.CLI/Nexo.CLI.csproj`) unless skipped.
5. Optionally run container smoke.

## Linux / macOS

Unified entrypoint:

```bash
bash scripts/install/install.sh --yes
```

Platform-specific entrypoints:

```bash
bash scripts/install/install-linux.sh --yes
bash scripts/install/install-macos.sh --yes
```

## Windows PowerShell

Unified entrypoint:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\install\install.ps1 -Yes
```

Wrapper alias:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\install\install-windows.ps1 -Yes
```

## Common options

- `--repo-url` / `-RepoUrl` — override source repository URL.
- `--install-dir` / `-InstallDir` — install target path.
- `--branch` / `-Branch` — checkout branch/tag after clone/update.
- `--include-optional` / `-IncludeOptional` — include optional dependencies in setup apply.
- `--yes` / `-Yes` — auto-confirm prompts.
- `--skip-build` / `-SkipBuild` — skip CLI build smoke step.
- `--run-container-smoke` / `-RunContainerSmoke` — run Docker smoke after native setup.
- `--dry-run` / `-DryRun` — print actions without changing system state.

## Example

```bash
bash scripts/install/install.sh --yes --run-container-smoke
```
