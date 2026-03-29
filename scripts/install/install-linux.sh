#!/usr/bin/env bash
set -euo pipefail

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
  echo "Usage: scripts/install/install-linux.sh [options]"
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

require_linux() {
  if [[ "$(uname -s)" != "Linux" ]]; then
    die "This installer only supports Linux hosts."
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
  if [[ "${DRY_RUN}" == "true" ]]; then
    echo "[dry-run] verify dotnet SDK >= 9 is already installed"
    return
  fi

  if ! command -v dotnet >/dev/null 2>&1; then
    die ".NET SDK 9+ is required but 'dotnet' was not found. Install it via your IDE and rerun."
  fi

  local version major
  version="$(dotnet --version 2>/dev/null || true)"
  major="${version%%.*}"
  if [[ -z "${version}" || -z "${major}" || "${major}" -lt 9 ]]; then
    die ".NET SDK 9+ is required (found: ${version:-none}). Install/update via your IDE and rerun."
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

run_restore() {
  local target_dir="$1"
  run_in_repo "${target_dir}" bash scripts/setup/setup.sh restore --yes
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

main() {
  require_linux
  expand_install_dir

  echo "Nexo Linux installer"
  echo "  repo-url: ${REPO_URL}"
  echo "  install-dir: ${INSTALL_DIR}"
  if [[ -n "${BRANCH}" ]]; then
    echo "  branch: ${BRANCH}"
  fi

  # Fire-and-forget bootstrap: install missing host prerequisites first.
  run_cmd bash "${SCRIPT_DIR}/../setup/setup.sh" apply --yes --guided

  sync_repo "${INSTALL_DIR}"
  ensure_dotnet_ready
  run_restore "${INSTALL_DIR}"
  run_build "${INSTALL_DIR}"
  run_hero_flow "${INSTALL_DIR}"
  start_daemon "${INSTALL_DIR}"
  print_next_steps "${INSTALL_DIR}"
}

main
