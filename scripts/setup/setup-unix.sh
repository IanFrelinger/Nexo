#!/usr/bin/env bash
# Cross-platform macOS + Linux setup entry, mirroring scripts/setup/setup.ps1 /
# setup-windows.ps1 on Windows. Dispatches to setup-macos.sh or setup-linux.sh.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
UNAME_S="$(uname -s)"

usage() {
  cat <<'EOF'
Usage:
  bash scripts/setup/setup-unix.sh [<check|restore|all|apply>] [options]
  bash scripts/setup/setup-unix.sh -Mode <check|restore|all|apply> [options]

macOS and Linux only. On Windows use PowerShell:
  powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\setup\setup.ps1 -Mode check

Modes (same as setup.ps1 -Mode):
  check   — verify required tools
  apply   — install missing host dependencies where supported
  restore — dotnet restore for the setup-gate project graph
  all     — apply + restore + optional Runtime Studio tune

Options (GNU-style or PowerShell-style for parity with setup.ps1):
  --include-optional / -IncludeOptional
      Treat missing optional tools as failures in check/apply (where supported).
  --yes / -Yes
      Non-interactive apply/all (no prompts).
  --guided / -Guided
      Guided apply output (where supported).
  --skip-runtime-studio-tune / -SkipRuntimeStudioTune
      Skip apps/runtime-studio auto-tune (same as NEXO_SKIP_RUNTIME_STUDIO_TUNE=1).

Environment:
  NEXO_SKIP_RUNTIME_STUDIO_TUNE=1 — skip Runtime Studio benchmark during "all".
EOF
}

MODE=""
INCLUDE_OPTIONAL=false
AUTO_YES=false
GUIDED=false
SKIP_TUNE=false

# Flags and mode can appear in any order (e.g. --yes all or all --yes).
while [[ $# -gt 0 ]]; do
  case "$1" in
    check | restore | all | apply)
      if [[ -n "${MODE}" ]]; then
        echo "error: mode specified twice (${MODE} and $1)" >&2
        exit 1
      fi
      MODE="$1"
      shift
      ;;
    -h | --help | -\? | help)
      usage
      exit 0
      ;;
    -Mode | -mode)
      if [[ $# -lt 2 ]]; then
        echo "error: $1 requires a value" >&2
        exit 1
      fi
      if [[ -n "${MODE}" ]]; then
        echo "error: mode specified twice" >&2
        exit 1
      fi
      MODE="$2"
      shift 2
      ;;
    --mode=*)
      if [[ -n "${MODE}" ]]; then
        echo "error: mode specified twice" >&2
        exit 1
      fi
      MODE="${1#*=}"
      shift
      ;;
    --include-optional | -IncludeOptional)
      INCLUDE_OPTIONAL=true
      shift
      ;;
    --yes | -Yes)
      AUTO_YES=true
      shift
      ;;
    --guided | -Guided)
      GUIDED=true
      shift
      ;;
    --skip-runtime-studio-tune | -SkipRuntimeStudioTune)
      SKIP_TUNE=true
      shift
      ;;
    *)
      echo "Unknown argument: $1" >&2
      usage
      exit 1
      ;;
  esac
done

if [[ -z "${MODE}" ]]; then
  MODE="check"
fi

case "${MODE}" in
  check | restore | all | apply) ;;
  *)
    echo "Invalid mode: ${MODE} (expected check, restore, all, or apply)" >&2
    usage
    exit 1
    ;;
esac

# Git Bash / Cygwin: point users at PowerShell (winget-based apply).
if [[ "${OSTYPE:-}" == msys* || "${OSTYPE:-}" == cygwin* ]]; then
  echo "setup-unix.sh is for native macOS/Linux shells." >&2
  echo "From Windows, run:" >&2
  echo '  powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\setup\setup.ps1 -Mode check' >&2
  exit 1
fi

if [[ "${SKIP_TUNE}" == "true" ]]; then
  export NEXO_SKIP_RUNTIME_STUDIO_TUNE=1
fi

FORWARD=("${MODE}")
if [[ "${INCLUDE_OPTIONAL}" == "true" ]]; then
  FORWARD+=(--include-optional)
fi
if [[ "${AUTO_YES}" == "true" ]]; then
  FORWARD+=(--yes)
fi
if [[ "${GUIDED}" == "true" ]]; then
  FORWARD+=(--guided)
fi

case "${UNAME_S}" in
  Darwin)
    exec "${SCRIPT_DIR}/setup-macos.sh" "${FORWARD[@]}"
    ;;
  Linux)
    exec "${SCRIPT_DIR}/setup-linux.sh" "${FORWARD[@]}"
    ;;
  *)
    echo "Unsupported OS for setup-unix.sh: ${UNAME_S}" >&2
    echo "Use scripts/setup/setup.ps1 on Windows." >&2
    exit 1
    ;;
esac
