#!/usr/bin/env bash
set -euo pipefail

DEFAULT_IMAGE="ghcr.io/ianfrelinger/nexo-cli:latest"
DEFAULT_SDK_IMAGE="mcr.microsoft.com/dotnet/sdk:8.0"
IMAGE="${DEFAULT_IMAGE}"
SDK_IMAGE="${DEFAULT_SDK_IMAGE}"
WORKSPACE_DIR=""
YES=false
DRY_RUN=false
GUIDED=false
DAEMON_DURATION=""

usage() {
  echo "Usage: scripts/install/container-bootstrap-macos.sh [options]"
  echo ""
  echo "Options:"
  echo "  --image <ref>            Container image to pull/run (default: ${DEFAULT_IMAGE})"
  echo "  --workspace <path>       Optional host workspace to mount at /work"
  echo "  --sdk-image <ref>        SDK container image for build/restore tasks (default: ${DEFAULT_SDK_IMAGE})"
  echo "  --start-daemon <dur>     After smoke checks, run background-agent daemon in container (requires --workspace or cwd as repo root)"
  echo "  --yes                    Auto-confirm install prompts"
  echo "  --guided                 Explain each step in plain language for non-technical users"
  echo "  --dry-run                Print actions without executing"
  echo "  -h, --help               Show help"
}

die() {
  echo "$1" >&2
  exit 1
}

require_value() {
  local flag="$1"
  local value="${2:-}"
  if [[ -z "${value}" || "${value}" == --* ]]; then
    die "Missing value for ${flag}"
  fi
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --image)
      require_value "$1" "${2:-}"
      IMAGE="$2"
      shift
      ;;
    --workspace)
      require_value "$1" "${2:-}"
      WORKSPACE_DIR="$2"
      shift
      ;;
    --sdk-image)
      require_value "$1" "${2:-}"
      SDK_IMAGE="$2"
      shift
      ;;
    --start-daemon)
      require_value "$1" "${2:-}"
      DAEMON_DURATION="$2"
      shift
      ;;
    --yes)
      YES=true
      ;;
    --guided)
      GUIDED=true
      ;;
    --dry-run)
      DRY_RUN=true
      ;;
    -h|--help)
      usage
      exit 0
      ;;
    *)
      die "Unknown argument: $1"
      ;;
  esac
  shift
done

require_macos() {
  if [[ "$(uname -s)" != "Darwin" ]]; then
    die "This script only supports macOS hosts."
  fi
}

run_cmd() {
  if [[ "$DRY_RUN" == "true" ]]; then
    echo "[dry-run] $*"
    return 0
  fi
  "$@"
}

print_guided_intro() {
  if [[ "${GUIDED}" != "true" ]]; then
    return
  fi

  echo "============================================="
  echo " Nexo Container Setup (Guided)"
  echo "============================================="
  echo ""
  echo "This setup will:"
  echo "  1) ensure Docker Desktop is installed"
  echo "  2) ensure Docker is running"
  echo "  3) pull Nexo runtime container images"
  echo "  4) run a quick health check"
  echo ""
  echo "You do NOT need to know Docker internals."
  echo ""
}

ensure_homebrew() {
  if command -v brew >/dev/null 2>&1; then
    return
  fi

  if [[ "$YES" != "true" ]]; then
    read -r -p "Homebrew is missing. Install Homebrew now? [y/N]: " answer
    if [[ "$answer" != "y" && "$answer" != "Y" ]]; then
      die "Homebrew is required to install Docker Desktop automatically."
    fi
  fi

  run_cmd /bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"

  if [[ "$DRY_RUN" != "true" ]]; then
    if [[ -x /opt/homebrew/bin/brew ]]; then
      eval "$(/opt/homebrew/bin/brew shellenv)"
    elif [[ -x /usr/local/bin/brew ]]; then
      eval "$(/usr/local/bin/brew shellenv)"
    fi
  fi
}

install_docker_if_missing() {
  if command -v docker >/dev/null 2>&1; then
    [[ "${GUIDED}" == "true" ]] && echo "[guided] Docker CLI already detected."
    return
  fi

  ensure_homebrew

  if [[ "$YES" != "true" ]]; then
    read -r -p "Docker CLI is missing. Install Docker Desktop now? [y/N]: " answer
    if [[ "$answer" != "y" && "$answer" != "Y" ]]; then
      die "Docker is required for container bootstrap."
    fi
  fi

  run_cmd brew install --cask docker
  [[ "${GUIDED}" == "true" ]] && echo "[guided] Docker Desktop install command completed."
}

ensure_docker_daemon() {
  if [[ "$DRY_RUN" == "true" ]]; then
    echo "[dry-run] docker info"
    return
  fi

  if docker info >/dev/null 2>&1; then
    [[ "${GUIDED}" == "true" ]] && echo "[guided] Docker daemon is running."
    return
  fi

  cat >&2 <<'TXT'
Docker is installed but daemon is not running.
Start Docker Desktop, wait for it to initialize, then re-run this script.
TXT
  exit 1
}

run_optional_daemon_smoke() {
  if [[ -z "${DAEMON_DURATION}" ]]; then
    return
  fi

  local ws="${WORKSPACE_DIR}"
  if [[ -z "${ws}" ]]; then
    ws="${PWD}"
  fi

  local abs_workspace="$ws"
  if [[ "${abs_workspace}" == "~" ]]; then
    abs_workspace="$HOME"
  elif [[ "${abs_workspace}" == ~/* ]]; then
    abs_workspace="$HOME/${abs_workspace#~/}"
  fi

  if [[ "$DRY_RUN" == "true" ]]; then
    echo "[dry-run] docker run --rm -v \"${abs_workspace}:/work\" -w /work \"${IMAGE}\" background-agent daemon --duration \"${DAEMON_DURATION}\""
    return
  fi

  if [[ ! -d "${abs_workspace}" ]]; then
    die "Workspace path does not exist for daemon smoke: ${abs_workspace}"
  fi

  echo "Running container background-agent daemon smoke (${DAEMON_DURATION})..."
  run_cmd docker run --rm -v "${abs_workspace}:/work" -w /work "${IMAGE}" background-agent daemon --duration "${DAEMON_DURATION}"
  echo ""
}

run_container_smoke() {
  run_cmd docker pull "$IMAGE"
  run_cmd docker pull "$SDK_IMAGE"
  run_cmd docker run --rm "$IMAGE" --help
  run_cmd docker run --rm "$SDK_IMAGE" dotnet --info

  if [[ -n "$WORKSPACE_DIR" ]]; then
    local abs_workspace="$WORKSPACE_DIR"
    if [[ "$abs_workspace" == "~" ]]; then
      abs_workspace="$HOME"
    elif [[ "$abs_workspace" == ~/* ]]; then
      abs_workspace="$HOME/${abs_workspace#~/}"
    fi

    if [[ "$DRY_RUN" != "true" && ! -d "$abs_workspace" ]]; then
      die "Workspace path does not exist: ${abs_workspace}"
    fi

    run_cmd docker run --rm -v "$abs_workspace:/work" -w /work "$IMAGE" --help
    run_cmd docker run --rm -v "$abs_workspace:/work" -w /work "$SDK_IMAGE" dotnet restore application/src/Nexo.CLI/Nexo.CLI.csproj
  fi
}

main() {
  require_macos
  print_guided_intro
  install_docker_if_missing
  ensure_docker_daemon
  run_container_smoke
  run_optional_daemon_smoke

  echo ""
  echo "Container bootstrap complete."
  echo "CLI image: $IMAGE"
  echo "SDK image: $SDK_IMAGE"
  echo ""
  echo "Examples:"
  echo "  docker run --rm $IMAGE --help"
  echo "  docker run --rm $SDK_IMAGE dotnet --info"
  if [[ -n "$WORKSPACE_DIR" ]]; then
    echo "  docker run --rm -v \"$WORKSPACE_DIR:/work\" -w /work $IMAGE pipeline validate --template /work/path/to/template.json"
    echo "  docker run --rm -v \"$WORKSPACE_DIR:/work\" -w /work $SDK_IMAGE dotnet build application/src/Nexo.CLI/Nexo.CLI.csproj"
  fi
}

main
