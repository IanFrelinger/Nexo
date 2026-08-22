#!/usr/bin/env bash
# Run any command inside the project's dev container image.
#
#   bash scripts/handoff/devbox.sh dotnet build Nexo.Kernel.sln
#   bash scripts/handoff/devbox.sh bash scripts/run-cert-gate.sh
#   bash scripts/handoff/devbox.sh                       # interactive shell
#
# WHY THIS EXISTS
#
# On this Windows host, Application Control blocks loading freshly-built unsigned
# test assemblies:
#
#   System.IO.FileLoadException: Could not load file or assembly
#   'Nexo.Tests.Infrastructure.dll'. An Application Control policy has blocked
#   this file. (0x800711C7)
#
# Zero tests are discovered, so the cert gate cannot run and there is no test
# signal at all. The policy does not apply inside a Linux container, so running
# the work there restores the signal without touching a host security setting.
#
# CONVENTIONS COPIED FROM scripts/Verify-DevContainer.ps1 (the canonical headless
# dev-container smoke), so both agree:
#   - same image as .devcontainer/devcontainer.json
#   - runs as ROOT, not vscode. Bind-mounted Windows files arrive with host-side
#     ownership; running as root avoids a UID mismatch writing bin/ and obj/.
#   - DOTNET_ROLL_FORWARD=LatestMajor. The cert gate runs `-f net8.0` and the
#     image ships only the 10.0 runtime; CI installs 8.0.x separately, this does
#     the equivalent by rolling forward.
#   - the payload is base64'd into the container, so quoting survives the trip.
#
# NuGet packages live in a named volume, so restores are cached between runs.

set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
IMAGE="mcr.microsoft.com/devcontainers/dotnet:10.0-noble"
NUGET_VOLUME="nexo-nuget-packages-root"

command -v docker >/dev/null 2>&1 || { echo "docker not found. Start Docker Desktop." >&2; exit 1; }
docker info >/dev/null 2>&1 || { echo "docker daemon not reachable. Start Docker Desktop." >&2; exit 1; }

# Git for Windows rewrites a leading /workspace into a Windows path. The doubled
# slash suppresses that; Linux treats //workspace as /workspace.
WORKDIR="/workspace"
MOUNT_SRC="$REPO_ROOT"
case "$(uname -s)" in
  MINGW*|MSYS*|CYGWIN*)
    WORKDIR="//workspace"
    export MSYS_NO_PATHCONV=1
    MOUNT_SRC="$(cd "$REPO_ROOT" && pwd -W 2>/dev/null || printf "%s" "$REPO_ROOT")"
    ;;
esac

if [[ $# -eq 0 ]]; then
  exec docker run --rm -it \
    -v "${MOUNT_SRC}:/workspace:rw" \
    -v "${NUGET_VOLUME}:/root/.nuget/packages" \
    -w "$WORKDIR" \
    -e DOTNET_ROLL_FORWARD=LatestMajor \
    -e DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    "$IMAGE" bash -l
fi

# Build the payload: mark the mount as a safe git directory (the host UID will not
# match root), then run whatever was asked for.
PAYLOAD="$(cat <<'PRE'
set -euo pipefail
export DOTNET_ROLL_FORWARD=LatestMajor
export DOTNET_CLI_TELEMETRY_OPTOUT=1
git config --global --add safe.directory /workspace 2>/dev/null || true
PRE
)"
PAYLOAD="${PAYLOAD}"$'\n'"$*"

B64="$(printf "%s" "$PAYLOAD" | base64 | tr -d "\n")"

exec docker run --rm \
  -v "${MOUNT_SRC}:/workspace:rw" \
  -v "${NUGET_VOLUME}:/root/.nuget/packages" \
  -w "$WORKDIR" \
  -e DOTNET_ROLL_FORWARD=LatestMajor \
  -e DOTNET_CLI_TELEMETRY_OPTOUT=1 \
  "$IMAGE" \
  bash -lc "echo $B64 | base64 -d | bash"
