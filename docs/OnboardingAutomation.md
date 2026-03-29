# Onboarding Automation Matrix

This document describes which setup steps are automated today, partially automated, or still manual.

## Goal

Make first-run setup predictable with minimal decisions:

- Lane A: container-first runtime.
- Lane B: native SDK + local development.

## Automated today

### Native lane (`scripts/setup/*`)

These are automated by `scripts/setup/setup.sh` (Linux/macOS) and `scripts/setup/setup.ps1` (Windows):

1. Dependency checks (`check` mode): verifies required tools and SDK version.
2. Optional dependency checks (Docker/Ollama/zstd where applicable).
3. Targeted restore (`restore` mode) for core projects.
4. Combined setup flow (`all` mode): dependency check + restore.
5. `apply` mode auto-installs missing required host dependencies where supported, then verifies setup health.

### Container lane (Docker commands)

These are automated by running container commands directly:

1. Pull runtime image.
2. Run CLI smoke command (`--help`).
3. Run mounted workspace commands (`-v "$PWD:/work" -w /work`).

## Partially automated

1. Docker installation/configuration:
   - Linux/Windows setup scripts can attempt Docker install in optional mode, but daemon startup/permissions may still need manual completion.
2. Optional model/provider setup:
   - Env vars and credentials are still user-provided.
3. Platform-specific host policies:
   - Corporate proxy/cert restrictions may need manual host config.

## Still manual

1. API credential provisioning (`OPENAI_API_KEY`, `AZURE_OPENAI_*`, etc.).
2. Selecting runtime lane for team conventions (container-only vs native dev).
3. Debugging host-specific permission/network constraints (enterprise images, locked-down package manager access).

## Recently added automation upgrades

1. Added cross-platform one-shot installers under `scripts/install/` to orchestrate:
   - clone/update repo,
   - setup `check`/`restore`,
   - CLI build smoke,
   - optional hero flow checks (`--help`, `doctor`, quickstart pipeline).
2. Added `nexo doctor` command with a single pass/fail onboarding summary (machine-readable via `--json`).
3. Added CI gate (`onboarding-quickstart-gate`) to validate first-run docs commands end-to-end in ephemeral jobs.

## Next recommended upgrades

1. Add clearer platform-specific troubleshooting pages for missing required tools.
2. Add a richer `nexo doctor --fix` mode that can execute safe remediations with explicit user confirmation.
3. Add periodic scheduled onboarding gate runs to detect ecosystem drift early.

