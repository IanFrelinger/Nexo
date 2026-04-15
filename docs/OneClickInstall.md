# One-Click Install

This guide provides single-command install/bootstrap paths for each platform using wrappers in `scripts/install/`.

## Quickstart (recommended)

The fastest path to a working portal — detects Docker or .NET SDK, builds, and opens the browser:

```bash
bash scripts/install/quickstart.sh
# Opens http://localhost:8080 with mock provider enabled
```

See `README.md` for details. The quickstart script handles .NET SDK installation if missing.

## Full installer (Anaconda-style guided prompts)

For the most beginner-friendly onboarding (guided prompts + package-manager setup guidance), use:

```bash
bash scripts/install/one-click.sh --yes
```

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\install\one-click.ps1 -Yes
```

## What these installers do

For Linux, macOS, and Windows installers:

1. Clone or update the Nexo repo.
2. Auto-bootstrap required prerequisites where possible (`git`, `curl`, package manager setup guidance, `.NET SDK 9+`).
3. Run setup preflight checks (`scripts/setup/setup.* check`) and restore baseline project graph.
4. Build CLI (`src/Nexo.CLI/Nexo.CLI.csproj`).
5. Optionally run full first-user "hero" checks (`--hero`) and then start `background-agent daemon`.

## One-shot container bootstrap (no native build path)

If you want to run Nexo purely via container, use container bootstrap wrappers under `scripts/install/`.

For the most beginner-friendly container onboarding:

```bash
bash scripts/install/container-one-click.sh --yes --workspace "$PWD"
```

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\install\container-one-click.ps1 -Yes -Workspace .
```

### Linux / macOS

Unified entrypoint:

```bash
bash scripts/install/container-bootstrap.sh --yes --workspace "$PWD"
```

Linux/macOS-specific entrypoints:

```bash
bash scripts/install/container-bootstrap-linux.sh --yes
bash scripts/install/container-bootstrap-macos.sh --yes
```

### Windows PowerShell

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\install\container-bootstrap.ps1 -Yes -Workspace .
```

or:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\install\container-bootstrap-windows.ps1 -Yes
```

### What the container bootstrap does

1. Ensures Docker is installed (installs if missing when possible).
2. Ensures Docker daemon is reachable.
3. Pulls `ghcr.io/ianfrelinger/nexo-cli:latest` (or your `--image` override).
4. Pulls `mcr.microsoft.com/dotnet/sdk:9.0` (or your `--sdk-image` override).
5. Smoke-runs both CLI and SDK images.
6. Optionally validates mount path with `--workspace` and runs mounted smoke checks.
7. If `--workspace` is set and the Nexo repo is mounted, runs SDK restore smoke on `src/Nexo.CLI/Nexo.CLI.csproj`.
8. In guided mode, prints plain-language progress and what to do next (no package-manager knowledge required).

## Packaged native installer artifacts (single-button release build)

For distribution-ready native installer bundles, use the GitHub Actions workflow:

- Workflow: `.github/workflows/native-installer-packages.yml`
- Trigger: **Actions** -> **Native Installer Packages** -> **Run workflow** (optional `version` input)

This one-button run builds self-contained CLI app-directory installer bundles for:

- Linux (`linux-x64`)
- macOS (`osx-x64`)
- Windows (`win-x64`)

### Output artifacts

Each run uploads one artifact per platform:

- `nexo-native-installer-linux-x64` (`.tar.gz`)
- `nexo-native-installer-macos-x64` (`.tar.gz`)
- `nexo-native-installer-windows-x64` (`.zip`)

Each bundle includes:

1. Self-contained CLI runtime directory (`app/` with `Nexo.CLI` entrypoint).
2. Platform-native install launcher (`install.sh`, `install.command`, or `install.ps1`).
3. A bundle README with install and verification commands.

### End-user install behavior

The bundle installers:

1. Copy the CLI runtime to a user-scoped app path and install launchers under `$HOME/.local/bin`.
2. Keep installation user-scoped (no admin/root required).
3. Auto-update shell profile PATH on Linux/macOS when needed (non-destructive append).
4. Run a post-install smoke check (`nexo --version`) and exit non-zero on failure.

## Linux / macOS

Unified entrypoint (auto-detects OS):

```bash
bash scripts/install/install.sh --yes
```

Guided one-click wrapper (recommended for non-technical users):

```bash
bash scripts/install/one-click.sh --yes
```

macOS non-CLI one-click launcher (double-click in Finder):

```bash
scripts/install/nexo-zero-to-hero-macos.command
```

This launcher opens Terminal, runs install + doctor + quickstart pipeline validation, and leaves a success/failure summary on screen.

Linux-specific entrypoint:

```bash
bash scripts/install/install-linux.sh --yes
```

Linux non-CLI one-click launcher:

```bash
bash scripts/install/nexo-zero-to-hero-linux.sh
```

macOS-specific entrypoint:

```bash
bash scripts/install/install-macos.sh --yes
```

## Windows PowerShell

Unified PowerShell entrypoint:

Script path reference: `scripts/install/install.ps1`

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\install\install.ps1 -Yes
```

Guided one-click wrapper:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\install\one-click.ps1 -Yes
```

Windows wrapper alias:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\install\install-windows.ps1 -Yes
```

Windows non-CLI one-click launcher:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\install\nexo-zero-to-hero-windows.ps1
```

## Common options

All installer entrypoints support equivalent options:

- `--repo-url` / `-RepoUrl` — override repo URL.
- `--install-dir` / `-InstallDir` — install target path.
- `--branch` / `-Branch` — checkout a branch/tag after clone.
- `--yes` / `-Yes` — auto-confirm dependency install prompts.
- `--skip-build` / `-SkipBuild` — skip CLI build after restore.
- `--hero` / `-Hero` — run first-user checks (`--help`, `doctor --json`, quickstart pipeline validate/run/diagnostics).
- `--start-daemon` / `-StartDaemon` — start `background-agent daemon` after setup.
- `--daemon-duration` / `-DaemonDuration` — bound daemon runtime (e.g. `30s`, `5m`).
- `--dry-run` / `-DryRun` — print planned actions without changing system state.

## Examples

Install into custom path and run daemon for 30 seconds:

```bash
bash scripts/install/install.sh --install-dir "$HOME/NexoProd" --yes --start-daemon --daemon-duration 30s
```

Run complete zero-to-hero macOS flow (install + doctor + pipeline validate/run):

```bash
bash scripts/install/install-macos.sh --yes --hero
```

Run complete zero-to-hero Linux flow:

```bash
bash scripts/install/install-linux.sh --yes --hero
```

Run complete zero-to-hero Windows flow:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\install\install.ps1 -Yes -Hero
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
