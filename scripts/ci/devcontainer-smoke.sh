#!/usr/bin/env bash
# Run the same restore + CLI smoke as .devcontainer/post-create + local build.
# Used by .github/workflows/devcontainer-gate.yml and setup-smoke-suite.yml.
# Usage: bash scripts/ci/devcontainer-smoke.sh [REPO_ROOT]
set -euo pipefail
REPO_ROOT="${1:-$(cd "$(dirname "$0")/../.." && pwd)}"
image="mcr.microsoft.com/devcontainers/dotnet:9.0-bookworm"
docker pull "$image"
docker run --rm \
  -v "${REPO_ROOT}:/workspace:rw" \
  -w /workspace \
  "$image" \
  bash -lc '
    set -euo pipefail
    bash .devcontainer/post-create.sh
    dotnet build src/Nexo.CLI/Nexo.CLI.csproj --no-restore -v minimal
    dotnet run --project src/Nexo.CLI -- --help >/dev/null
    echo "devcontainer-smoke: ok"
  '
