# Setup matrix verification (“Calvin-grade” combinations)

This repo’s bootstrap is already exercised in **`.github/workflows/environment-setup-gate-v1.yml`** (Windows, macOS, Ubuntu, plus an ephemeral Linux container). The **matrix verifier** goes further on a **single machine** by trying many *combinations* that commonly break “seamless” setup:

- **Working directory**: `setup.ps1` from repo root vs from `%TEMP%` with an absolute script path.
- **Entrypoint**: `setup.ps1` vs direct `setup-windows.ps1`.
- **Restore idempotency**: `restore` twice in a row.
- **NuGet isolation**: fresh `NUGET_PACKAGES` cache directory, then `dotnet build --no-restore`.
- **Docker**: every `docker-compose*.yml` **`docker compose config`** (validates interpolation), `docker-restore.ps1`, optional **image builds**, and **two** official SDK base images (`8.0` default Debian + `8.0-bookworm-slim`) running `setup-linux.sh` + CLI build *inside* the container.
- **Strict optional deps**: only when you pass `-IncludeOptionalDependencyCheck` to the PowerShell verifier (requires Docker **and** Ollama on `PATH`).

## Windows (full matrix)

From repo root:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .\scripts\setup\verify-setup-matrix.ps1
```

Faster CI-style (skip Docker **image** builds; still runs `compose config` + `docker-restore` when Docker works):

```powershell
./scripts/setup/verify-setup-matrix.ps1 -SkipDockerBuild
```

Skip all Docker tiers (Tier B/C):

```powershell
./scripts/setup/verify-setup-matrix.ps1 -SkipDocker
```

Artifacts:

- `.nexo/setup-matrix-report.json` — machine-readable case results.

Exit codes:

- `0` — all executed cases passed.
- `1` — at least one **Tier A** (native) case failed → core setup is not seamless.
- `2` — Tier A passed but a Docker/container case failed (environment-specific).

## Linux / macOS

```bash
chmod +x scripts/setup/verify-setup-matrix.sh   # once
SKIP_DOCKER_BUILD=1 bash scripts/setup/verify-setup-matrix.sh
```

The matrix exercises `scripts/setup/setup-unix.sh` (bash entry for macOS/Linux, flag-compatible with `setup.ps1` on Windows). `scripts/setup/setup.sh` forwards to the same implementation.

Environment:

- `SKIP_DOCKER=1` — Tier B/C off.
- `SKIP_DOCKER_BUILD=1` — keep `compose config` / `docker-restore`, skip `docker compose build` and heavy container passes.

## CI

`environment-setup-gate-v1` runs the verifier on **Windows** with `-SkipDockerBuild` so PRs get broader coverage without doubling image-build time. Set `NEXO_MATRIX_DOCKER_BUILD=1` on a workflow dispatch run if you need full Docker image builds in GitHub Actions.
