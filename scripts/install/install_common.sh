#!/usr/bin/env bash
# Shared native installer for Linux and macOS (escape hatch — prefer Dev Container).
# Invoked only via install.sh, install-linux.sh, or install-macos.sh with NEXO_INSTALL_PLATFORM=linux|darwin.
set -euo pipefail

: "${NEXO_INSTALL_PLATFORM:?NEXO_INSTALL_PLATFORM must be linux or darwin}"

DEFAULT_REPO_URL="https://github.com/IanFrelinger/Nexo.git"
DEFAULT_INSTALL_DIR="${HOME}/Nexo"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_URL="${DEFAULT_REPO_URL}"
INSTALL_DIR="${DEFAULT_INSTALL_DIR}"
BRANCH=""
START_DAEMON=false
DAEMON_DURATION=""
YES=false
DRY_RUN=false
SKIP_BUILD=false
RUN_HERO=false

usage() {
  echo "Usage: scripts/install/install.sh [options]   (or install-linux.sh / install-macos.sh)"
  echo ""
  echo "Options:"
  echo "  --repo-url <url>          Git repository URL (default: ${DEFAULT_REPO_URL})"
  echo "  --install-dir <path>      Installation directory (default: ${DEFAULT_INSTALL_DIR})"
  echo "  --branch <name>           Optional git branch/tag to checkout"
  echo "  --yes                     Auto-confirm setup dependency installation prompts"
  echo "  --skip-build              Skip 'dotnet build src/Nexo.CLI/Nexo.CLI.csproj --no-restore'"
  echo "  --start-daemon            Start background-agent daemon after install"
  echo "  --daemon-duration <dur>   Daemon run duration (e.g. 30s, 5m). Omit to run until Ctrl+C"
  echo "  --hero                    Run onboarding checks and a first pipeline after install"
  echo "  --dry-run                 Print actions without executing"
  echo "  -h, --help                Show help"
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
    --repo-url)
      require_value "$1" "${2:-}"
      REPO_URL="$2"
      shift
      ;;
    --install-dir)
      require_value "$1" "${2:-}"
      INSTALL_DIR="$2"
      shift
      ;;
    --branch)
      require_value "$1" "${2:-}"
      BRANCH="$2"
      shift
      ;;
    --include-optional)
      ;;
    --yes)
      YES=true
      ;;
    --skip-build)
      SKIP_BUILD=true
      ;;
    --start-daemon)
      START_DAEMON=true
      ;;
    --daemon-duration)
      require_value "$1" "${2:-}"
      DAEMON_DURATION="$2"
      shift
      ;;
    --hero)
      RUN_HERO=true
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

require_os() {
  local u
  u="$(uname -s)"
  if [[ "${NEXO_INSTALL_PLATFORM}" == "linux" && "${u}" != "Linux" ]]; then
    die "This entrypoint is for Linux hosts only."
  fi
  if [[ "${NEXO_INSTALL_PLATFORM}" == "darwin" && "${u}" != "Darwin" ]]; then
    die "This entrypoint is for macOS hosts only."
  fi
}

expand_install_dir() {
  if [[ "${INSTALL_DIR}" == "~" ]]; then
    INSTALL_DIR="${HOME}"
  elif [[ "${INSTALL_DIR}" == ~/* ]]; then
    INSTALL_DIR="${HOME}/${INSTALL_DIR#~/}"
  fi
}

ensure_dotnet_ready() {
  if [[ "${NEXO_INSTALL_PLATFORM}" != "linux" ]]; then
    return
  fi

  if [[ "${DRY_RUN}" == "true" ]]; then
    echo "[dry-run] verify dotnet SDK >= 9 is already installed"
    return
  fi

  if ! command -v dotnet >/dev/null 2>&1; then
    echo ".NET SDK not found — installing .NET 9 via dotnet-install script..."
    if command -v curl >/dev/null 2>&1; then
      curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
    elif command -v wget >/dev/null 2>&1; then
      wget -qO /tmp/dotnet-install.sh https://dot.net/v1/dotnet-install.sh
    else
      die ".NET SDK 9+ is required but 'dotnet' was not found and neither curl nor wget is available. Install .NET SDK 9 from https://dot.net and rerun."
    fi
    bash /tmp/dotnet-install.sh --channel 9.0
    export PATH="${HOME}/.dotnet:${PATH}"
    rm -f /tmp/dotnet-install.sh
    if ! command -v dotnet >/dev/null 2>&1; then
      die ".NET SDK installation failed. Install manually from https://dot.net and rerun."
    fi
  fi

  local version major
  version="$(dotnet --version 2>/dev/null || true)"
  major="${version%%.*}"
  if [[ -z "${version}" || -z "${major}" || "${major}" -lt 9 ]]; then
    echo ".NET SDK version too old (found: ${version:-none}) — upgrading to 9..."
    if command -v curl >/dev/null 2>&1; then
      curl -fsSL https://dot.net/v1/dotnet-install.sh | bash -s -- --channel 9.0
    elif command -v wget >/dev/null 2>&1; then
      wget -qO- https://dot.net/v1/dotnet-install.sh | bash -s -- --channel 9.0
    else
      die ".NET SDK 9+ is required (found: ${version:-none}). Install/update from https://dot.net and rerun."
    fi
    export PATH="${HOME}/.dotnet:${PATH}"
    version="$(dotnet --version 2>/dev/null || true)"
    major="${version%%.*}"
    if [[ -z "${version}" || -z "${major}" || "${major}" -lt 9 ]]; then
      die ".NET SDK 9+ is required (found: ${version:-none}). Install/update from https://dot.net and rerun."
    fi
  fi
}

run_cmd() {
  if [[ "${DRY_RUN}" == "true" ]]; then
    echo "[dry-run] $*"
    return 0
  fi
  "$@"
}

run_in_repo() {
  local target_dir="$1"
  shift
  if [[ "${DRY_RUN}" == "true" ]]; then
    echo "[dry-run] (cd \"${target_dir}\" && $*)"
    return 0
  fi
  (
    cd "${target_dir}"
    "$@"
  )
}

sync_repo() {
  local target_dir="$1"
  if [[ -d "${target_dir}/.git" ]]; then
    run_cmd git -C "${target_dir}" fetch --all --tags
    if [[ -n "${BRANCH}" ]]; then
      run_cmd git -C "${target_dir}" checkout "${BRANCH}"
      run_cmd git -C "${target_dir}" pull --ff-only origin "${BRANCH}"
    else
      run_cmd git -C "${target_dir}" pull --ff-only
    fi
    return
  fi

  run_cmd mkdir -p "$(dirname "${target_dir}")"
  if [[ -n "${BRANCH}" ]]; then
    run_cmd git clone --branch "${BRANCH}" --single-branch "${REPO_URL}" "${target_dir}"
  else
    run_cmd git clone "${REPO_URL}" "${target_dir}"
  fi
}

host_prereq_bootstrap() {
  case "${NEXO_INSTALL_PLATFORM}" in
    linux)
      run_cmd bash "${SCRIPT_DIR}/../setup/setup.sh" apply --yes --guided
      ;;
    darwin)
      run_cmd bash "${SCRIPT_DIR}/../setup/setup.sh" apply --yes
      ;;
  esac
}

post_clone_setup() {
  local target_dir="$1"
  case "${NEXO_INSTALL_PLATFORM}" in
    linux)
      ensure_dotnet_ready
      run_in_repo "${target_dir}" bash scripts/setup/setup.sh restore --yes
      ;;
    darwin)
      if [[ "${YES}" == "true" ]]; then
        run_in_repo "${target_dir}" bash scripts/setup/setup.sh apply --yes
      else
        run_in_repo "${target_dir}" bash scripts/setup/setup.sh apply
      fi
      run_in_repo "${target_dir}" bash scripts/setup/setup.sh check
      run_in_repo "${target_dir}" bash scripts/setup/setup.sh restore
      ;;
  esac
}

run_build() {
  local target_dir="$1"
  if [[ "${SKIP_BUILD}" == "true" ]]; then
    return
  fi
  run_in_repo "${target_dir}" dotnet build src/Nexo.CLI/Nexo.CLI.csproj --no-restore
}

start_daemon() {
  local target_dir="$1"
  if [[ "${START_DAEMON}" != "true" ]]; then
    return
  fi

  local daemon_args=(run --project src/Nexo.CLI -- background-agent daemon)
  if [[ -n "${DAEMON_DURATION}" ]]; then
    daemon_args+=(--duration "${DAEMON_DURATION}")
  fi
  run_in_repo "${target_dir}" dotnet "${daemon_args[@]}"
}

run_hero_flow() {
  local target_dir="$1"
  if [[ "${RUN_HERO}" != "true" ]]; then
    return
  fi

  if [[ "${DRY_RUN}" == "true" ]]; then
    echo "[dry-run] (cd \"${target_dir}\" && dotnet run --project src/Nexo.CLI -- --help)"
    echo "[dry-run] (cd \"${target_dir}\" && dotnet run --project src/Nexo.CLI -- doctor --json)"
    echo "[dry-run] create quickstart template and run pipeline validate/run/diagnostics"
    return
  fi

  local tmp_dir
  tmp_dir="$(mktemp -d)"
  trap 'rm -rf "${tmp_dir}"' RETURN

  local template_path="${tmp_dir}/nexo_quickstart.json"
  cat > "${template_path}" <<'JSON'
{
  "templateId": "quickstart",
  "version": "1.0",
  "stages": [
    { "id": "ingest", "name": "Ingest", "mode": "Deterministic" },
    { "id": "hybrid", "name": "Hybrid", "mode": "Hybrid", "fallbackChain": ["Deterministic", "Agentic"] }
  ],
  "edges": [
    { "fromStageId": "ingest", "toStageId": "hybrid" }
  ]
}
JSON

  local run_id="hero-$(date +%s)"
  run_in_repo "${target_dir}" dotnet run --project src/Nexo.CLI -- --help
  run_in_repo "${target_dir}" dotnet run --project src/Nexo.CLI -- doctor --json
  run_in_repo "${target_dir}" dotnet run --project src/Nexo.CLI -- pipeline validate --template "${template_path}"
  run_in_repo "${target_dir}" dotnet run --project src/Nexo.CLI -- pipeline run --template "${template_path}" --run-id "${run_id}" --format-json
  run_in_repo "${target_dir}" dotnet run --project src/Nexo.CLI -- pipeline diagnostics --format-json
}

print_next_steps() {
  local target_dir="$1"
  echo ""
  echo "Install complete."
  echo "Repo: ${target_dir}"
  echo ""
  echo "Next commands:"
  echo "  cd \"${target_dir}\""
  echo "  dotnet run --project src/Nexo.CLI -- --help"
  echo "  dotnet run --project src/Nexo.CLI -- background-agent daemon --duration 30s"
}

platform_label() {
  case "${NEXO_INSTALL_PLATFORM}" in
    linux) echo "Linux" ;;
    darwin) echo "macOS" ;;
    *) echo "Unix" ;;
  esac
}

main() {
  require_os
  expand_install_dir

  echo "Nexo $(platform_label) native installer (escape hatch — prefer Dev Container)"
  echo "  repo-url: ${REPO_URL}"
  echo "  install-dir: ${INSTALL_DIR}"
  if [[ -n "${BRANCH}" ]]; then
    echo "  branch: ${BRANCH}"
  fi

  host_prereq_bootstrap
  sync_repo "${INSTALL_DIR}"
  post_clone_setup "${INSTALL_DIR}"
  run_build "${INSTALL_DIR}"
  run_hero_flow "${INSTALL_DIR}"
  start_daemon "${INSTALL_DIR}"
  print_next_steps "${INSTALL_DIR}"
}

main
