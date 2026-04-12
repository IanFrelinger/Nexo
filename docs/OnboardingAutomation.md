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
6. `apply --yes` enables non-interactive dependency bootstrap for “fire-and-forget” installs.

### Container lane (Docker commands)

These are automated by running container commands directly (or via guided wrappers under `scripts/install/container-one-click.*`):

1. Ensure Docker is installed where possible (Linux package manager / Homebrew / winget).
2. Validate Docker daemon reachability.
3. Pull runtime image and SDK image.
4. Run CLI + SDK smoke checks.
5. Optionally run mounted workspace smoke checks (`-v "$PWD:/work" -w /work`).

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
4. Added `nexo doctor --fix` remediation mode for a safe subset of common onboarding failures.
   - Requires explicit consent (`--yes`) before any remediation runs.
   - Emits remediation attempts/results in JSON output for auditability.
5. Expanded `onboarding-quickstart-gate` with weekly scheduled drift detection and taxonomy artifacts:
   - `*-taxonomy.json` classifies lane/platform/root-cause class.
   - `*-trend.json` captures run metadata signals for drift tracking over time.
   - Summary artifacts now include troubleshooting doc pointers.

## Next recommended upgrades

1. Add clearer platform-specific troubleshooting pages for missing required tools.
2. Expand remediation catalog with additional safe fixers for platform-specific host issues.
3. Add trend aggregation/reporting across multiple scheduled runs to surface regressions in one dashboard view.

