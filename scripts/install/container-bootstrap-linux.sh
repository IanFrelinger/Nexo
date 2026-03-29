#!/usr/bin/env bash
set -euo pipefail

DEFAULT_IMAGE="ghcr.io/ianfrelinger/nexo-cli:latest"
IMAGE="${DEFAULT_IMAGE}"
DEFAULT_SDK_IMAGE="mcr.microsoft.com/dotnet/sdk:9.0"
SDK_IMAGE="${DEFAULT_SDK_IMAGE}"
INCLUDE_OPTIONAL=false
YES=false
DRY_RUN=false
WORKSPACE_DIR=""
GUIDED=false

usage() {
  echo "Usage: scripts/install/container-bootstrap-linux.sh [options]"
  echo ""
  echo "Options:"
  echo "  --image <ref>            Container image to pull/run (default: ${DEFAULT_IMAGE})"
  echo "  --sdk-image <ref>        SDK container image to pull/run (default: ${DEFAULT_SDK_IMAGE})"
  echo "  --workspace <path>       Optional host workspace to mount at /work"
  echo "  --include-optional       Also install optional host dependencies when possible"
  echo "  --yes                    Auto-confirm install prompts"
  echo "  --guided                 Print beginner-friendly setup explanations"
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
    --sdk-image)
      require_value "$1" "${2:-}"
      SDK_IMAGE="$2"
      shift
      ;;
    --workspace)
      require_value "$1" "${2:-}"
      WORKSPACE_DIR="$2"
      shift
      ;;
    --include-optional)
      INCLUDE_OPTIONAL=true
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

require_linux() {
  if [[ "$(uname -s)" != "Linux" ]]; then
    die "This script only supports Linux hosts."
  fi
}

run_as_root() {
  if [[ "$DRY_RUN" == "true" ]]; then
    echo "[dry-run] sudo $*"
    return 0
  fi

  if [[ "${EUID}" -eq 0 ]]; then
    "$@"
    return
  fi

  if command -v sudo >/dev/null 2>&1; then
    sudo "$@"
    return
  fi

  die "Root privileges are required to run: $*"
}

run_cmd() {
  if [[ "$DRY_RUN" == "true" ]]; then
    echo "[dry-run] $*"
    return 0
  fi
  "$@"
}

print_guided_banner() {
  if [[ "${GUIDED}" != "true" ]]; then
    return
  fi
  cat <<'TXT'
=============================================
 Nexo Container One-Click Setup (Linux)
=============================================

This setup will:
  1) install Docker if missing
  2) start Docker daemon if needed
  3) pull Nexo runtime + SDK images
  4) run smoke checks so you can use it immediately

You do NOT need to know container tooling details.

TXT
}

pkg_manager() {
  if command -v apt-get >/dev/null 2>&1; then echo "apt-get"; return; fi
  if command -v dnf >/dev/null 2>&1; then echo "dnf"; return; fi
  if command -v yum >/dev/null 2>&1; then echo "yum"; return; fi
  if command -v pacman >/dev/null 2>&1; then echo "pacman"; return; fi
  if command -v zypper >/dev/null 2>&1; then echo "zypper"; return; fi
  echo ""
}

install_pkg() {
  local manager="$1"
  local package_name="$2"
  case "$manager" in
    apt-get)
      run_as_root apt-get update
      run_as_root apt-get install -y "$package_name"
      ;;
    dnf)
      run_as_root dnf install -y "$package_name"
      ;;
    yum)
      run_as_root yum install -y "$package_name"
      ;;
    pacman)
      run_as_root pacman -Sy --noconfirm "$package_name"
      ;;
    zypper)
      run_as_root zypper --non-interactive install "$package_name"
      ;;
    *)
      die "No supported package manager found for installing ${package_name}."
      ;;
  esac
}

install_docker_if_missing() {
  if command -v docker >/dev/null 2>&1; then
    return
  fi

  local manager
  manager="$(pkg_manager)"
  if [[ -z "$manager" ]]; then
    die "Docker is not installed and no supported package manager was found."
  fi

  if [[ "$YES" != "true" ]]; then
    read -r -p "Docker is missing. Install Docker now? [y/N]: " answer
    if [[ "$answer" != "y" && "$answer" != "Y" ]]; then
      die "Docker is required for container bootstrap."
    fi
  fi

  case "$manager" in
    apt-get)
      install_pkg "$manager" docker.io
      ;;
    dnf|yum|pacman|zypper)
      install_pkg "$manager" docker
      ;;
  esac
}

ensure_docker_daemon() {
  if [[ "$DRY_RUN" == "true" ]]; then
    echo "[dry-run] docker info"
    return
  fi

  if docker info >/dev/null 2>&1; then
    return
  fi

  if command -v systemctl >/dev/null 2>&1; then
    run_as_root systemctl enable --now docker || true
  elif command -v service >/dev/null 2>&1; then
    run_as_root service docker start || true
  fi

  if ! docker info >/dev/null 2>&1; then
    die "Docker daemon is not reachable. Start Docker and retry."
  fi
}

run_container_smoke() {
  run_cmd docker pull "$IMAGE"
  run_cmd docker run --rm "$IMAGE" --help
  run_cmd docker pull "$SDK_IMAGE"
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
    run_cmd docker run --rm -v "$abs_workspace:/work" -w /work "$SDK_IMAGE" dotnet --info
    if [[ "$DRY_RUN" == "true" ]]; then
      echo "[dry-run] if [ -f src/Nexo.CLI/Nexo.CLI.csproj ]; then dotnet restore src/Nexo.CLI/Nexo.CLI.csproj; else echo skip; fi"
    else
      run_cmd docker run --rm -v "$abs_workspace:/work" -w /work "$SDK_IMAGE" bash -lc \
        'if [ -f src/Nexo.CLI/Nexo.CLI.csproj ]; then dotnet restore src/Nexo.CLI/Nexo.CLI.csproj; else echo "No Nexo CLI project found under mounted /work; skipping SDK restore smoke."; fi'
    fi
  fi
}

main() {
  require_linux
  print_guided_banner
  install_docker_if_missing
  ensure_docker_daemon

  if [[ "$INCLUDE_OPTIONAL" == "true" ]]; then
    echo "Optional host dependencies are managed by scripts/setup/setup-linux.sh (outside container bootstrap scope)."
  fi

  run_container_smoke

  echo ""
  echo "Container bootstrap complete."
  echo "CLI image: $IMAGE"
  echo "SDK image: $SDK_IMAGE"
  echo ""
  echo "Examples:"
  echo "  docker run --rm $IMAGE --help"
  echo "  docker run --rm -v \"\$PWD:/work\" -w /work $SDK_IMAGE dotnet --info"
  if [[ -n "$WORKSPACE_DIR" ]]; then
    echo "  docker run --rm -v \"$WORKSPACE_DIR:/work\" -w /work $IMAGE pipeline validate --template /work/path/to/template.json"
    echo "  docker run --rm -v \"$WORKSPACE_DIR:/work\" -w /work $SDK_IMAGE dotnet restore src/Nexo.CLI/Nexo.CLI.csproj"
  fi
}

main
