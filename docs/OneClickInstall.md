# One-Click Install (Option 1)

This guide provides "single-command" install/bootstrap paths for each platform using the installer wrappers in `scripts/install/`.

## What these installers do

For Linux, macOS, and Windows installers:

1. Clone or update the Nexo repo.
2. Run platform dependency setup (`scripts/setup/setup.*` in `apply` mode).
3. Run restore for baseline project graph.
4. Build CLI (`src/Nexo.CLI/Nexo.CLI.csproj`).
5. Optionally start `background-agent daemon`.

## Linux / macOS

Unified entrypoint (auto-detects OS):

```bash
bash scripts/install/install.sh --yes
```

Linux-specific entrypoint:

```bash
bash scripts/install/install-linux.sh --yes
```

macOS-specific entrypoint:

```bash
bash scripts/install/install-macos.sh --yes
```

## Windows PowerShell

Unified PowerShell entrypoint:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\install\install.ps1 -Yes
```

Windows wrapper alias:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\install\install-windows.ps1 -Yes
```

## Common options

All installer entrypoints support equivalent options:

- `--repo-url` / `-RepoUrl` — override repo URL.
- `--install-dir` / `-InstallDir` — install target path.
- `--branch` / `-Branch` — checkout a branch/tag after clone.
- `--include-optional` / `-IncludeOptional` — install optional deps (Docker/Ollama) if missing.
- `--yes` / `-Yes` — auto-confirm dependency install prompts.
- `--skip-build` / `-SkipBuild` — skip CLI build after restore.
- `--start-daemon` / `-StartDaemon` — start `background-agent daemon` after setup.
- `--daemon-duration` / `-DaemonDuration` — bound daemon runtime (e.g. `30s`, `5m`).
- `--dry-run` / `-DryRun` — print planned actions without changing system state.

## Examples

Install into custom path and run daemon for 30 seconds:

```bash
bash scripts/install/install.sh --install-dir "$HOME/NexoProd" --yes --start-daemon --daemon-duration 30s
```

PowerShell equivalent:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\install\install.ps1 -InstallDir "$HOME\NexoProd" -Yes -StartDaemon -DaemonDuration 30s
```

Dry-run preview:

```bash
bash scripts/install/install.sh --dry-run --yes
```

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\install\install.ps1 -DryRun -Yes
```
