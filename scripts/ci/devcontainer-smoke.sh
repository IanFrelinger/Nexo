#!/usr/bin/env bash
# Run the same restore + CLI smoke as .devcontainer/post-create + local build.
# Used by .github/workflows/devcontainer-gate.yml and setup-smoke-suite.yml.
# Usage: bash scripts/ci/devcontainer-smoke.sh [REPO_ROOT]
set -euo pipefail
REPO_ROOT="${1:-$(cd "$(dirname "$0")/../.." && pwd)}"
image="mcr.microsoft.com/devcontainers/dotnet:9.0-bookworm"
# grpc.tools protoc can segfault on linux_arm64 in this image (exit 139). CI is amd64;
# on Apple Silicon, force amd64 so the smoke matches GitHub-hosted behavior.
DOCKER_PLATFORM=()
case "$(uname -m 2>/dev/null || true)" in
  arm64 | aarch64) DOCKER_PLATFORM=(--platform linux/amd64) ;;
esac
docker pull "${DOCKER_PLATFORM[@]}" "$image"
docker run --rm "${DOCKER_PLATFORM[@]}" \
  -v "${REPO_ROOT}:/workspace:rw" \
  -w /workspace \
  "$image" \
  bash -lc '
    set -euo pipefail
    # Image ships .NET 9 runtime only; Nexo targets net8.0 — roll forward for build/run smoke.
    export DOTNET_ROLL_FORWARD=LatestMajor
    bash .devcontainer/post-create.sh
    dotnet build src/Nexo.CLI/Nexo.CLI.csproj --no-restore -v minimal
    dotnet run --project src/Nexo.CLI -- --help >/dev/null
    echo "devcontainer-smoke: ok"
  '
