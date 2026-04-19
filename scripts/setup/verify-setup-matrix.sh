#!/usr/bin/env bash
# Brute-force style Nexo setup verification (Linux/macOS host + optional Docker).
# For JSON report + full Windows matrix, use: pwsh ./scripts/setup/verify-setup-matrix.ps1

set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
SKIP_DOCKER="${SKIP_DOCKER:-0}"
SKIP_DOCKER_BUILD="${SKIP_DOCKER_BUILD:-0}"
FAIL_A=0

# grpc.tools bundled protoc can segfault on linux_arm64 inside some Debian slim SDK
# images (exit 139). GitHub-hosted runners are amd64; on Apple Silicon, match that so
# Tier C container builds stay reliable.
MATRIX_CONTAINER_PLATFORM=()
case "$(uname -m 2>/dev/null || true)" in
  arm64 | aarch64) MATRIX_CONTAINER_PLATFORM=(--platform linux/amd64) ;;
esac

log() { echo ""; echo "=== $1 ==="; echo "$2"; }

run_a() {
  local name="$1"
  shift
  log "A :: ${name}" "Running: $*"
  if (cd "${REPO_ROOT}" && "$@"); then echo "OK  ${name}"; else echo "FAIL ${name}"; FAIL_A=$((FAIL_A + 1)); fi
}

run_b() {
  local name="$1"
  shift
  log "B :: ${name}" "Running: $*"
  if (cd "${REPO_ROOT}" && "$@"); then echo "OK  ${name}"; else echo "FAIL ${name} (non-fatal tier B)"; fi
}

run_a "setup-unix.sh check" bash scripts/setup/setup-unix.sh check
run_a "setup-unix.sh -Mode check (pwsh-style)" bash scripts/setup/setup-unix.sh -Mode check
run_a "setup-unix.sh restore" bash scripts/setup/setup-unix.sh restore
run_a "setup-unix.sh restore (repeat)" bash scripts/setup/setup-unix.sh restore
run_a "dotnet build Nexo.CLI --no-restore" dotnet build src/Nexo.CLI/Nexo.CLI.csproj --no-restore -v minimal

ISO_NUGET="$(mktemp -d 2>/dev/null || mktemp -d -t nexo-nuget)"
export NUGET_PACKAGES="${ISO_NUGET}"
run_a "setup-unix.sh restore (isolated NUGET_PACKAGES)" bash scripts/setup/setup-unix.sh restore
run_a "dotnet build (isolated NUGET_PACKAGES)" dotnet build src/Nexo.CLI/Nexo.CLI.csproj --no-restore -v minimal
unset NUGET_PACKAGES

if [[ "${SKIP_DOCKER}" == "1" ]] || ! command -v docker >/dev/null 2>&1 || ! docker info >/dev/null 2>&1; then
  echo "Docker tier skipped (SKIP_DOCKER=1 or no engine)."
else
  for cf in docker-compose.agent-server.yml docker-compose.portal.yml docker-compose.test.yml docker-compose.ephemeral.yml docker-compose.ollama.yml; do
    [[ -f "${REPO_ROOT}/${cf}" ]] || continue
    proj="mtx-$(echo "${cf}" | tr -cd 'a-zA-Z0-9')"
    run_b "docker compose config ${cf}" docker compose -p "${proj}" -f "${cf}" config >/dev/null
  done
  if command -v pwsh >/dev/null 2>&1; then
    run_b "docker-restore.ps1" pwsh -NoProfile -ExecutionPolicy Bypass -File "${REPO_ROOT}/scripts/docker-restore.ps1" -RepoRoot "${REPO_ROOT}"
  else
    echo "SKIP docker-restore.ps1 (pwsh not installed)"
  fi
  if [[ "${SKIP_DOCKER_BUILD}" != "1" ]]; then
    run_b "docker compose build agent-server" docker compose -f docker-compose.agent-server.yml build nexo-api
    run_b "docker compose build portal" docker compose -p mtx-portal -f docker-compose.portal.yml build nexo-api
    if command -v pwsh >/dev/null 2>&1; then
      run_b "docker-restore.ps1 -Build" pwsh -NoProfile -ExecutionPolicy Bypass -File "${REPO_ROOT}/scripts/docker-restore.ps1" -RepoRoot "${REPO_ROOT}" -Build
    fi
  fi

  for tag in mcr.microsoft.com/dotnet/sdk:9.0 mcr.microsoft.com/dotnet/sdk:9.0-bookworm-slim; do
    log "C :: container ${tag}" "docker pull + setup-linux + build"
    docker pull "${tag}" >/dev/null
    if docker run --rm "${MATRIX_CONTAINER_PLATFORM[@]}" -v "${REPO_ROOT}:/repo" -w /repo "${tag}" bash -lc '
      set -euo pipefail
      export DEBIAN_FRONTEND=noninteractive
      if command -v apt-get >/dev/null 2>&1; then
        apt-get update
        apt-get install -y ca-certificates curl git gnupg bash
      fi
      bash scripts/setup/setup-linux.sh check
      bash scripts/setup/setup-linux.sh restore
      dotnet build src/Nexo.CLI/Nexo.CLI.csproj --no-restore -v minimal
    '; then
      echo "OK  container ${tag}"
    else
      echo "FAIL container ${tag} (non-fatal tier C)"
    fi
  done
fi

if [[ "${FAIL_A}" -gt 0 ]]; then
  echo "Tier A failures: ${FAIL_A}" >&2
  exit 1
fi
echo "Matrix finished: Tier A all passed."
