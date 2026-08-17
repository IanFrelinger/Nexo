#!/usr/bin/env bash
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../../.." && pwd)"
# Prefer the gitignored, hardware-tuned copy written by optimize_agent_cluster.sh; fall back to
# the tracked definitions when no tune has been run.
CONFIG_PATH="${REPO_ROOT}/.nexo/runtime-studio/agent_set.local.json"
if [[ ! -f "${CONFIG_PATH}" ]]; then
  CONFIG_PATH="${REPO_ROOT}/apps/runtime-studio/config/agent_set.local.json"
fi

DURATION="10m"
DISABLE_OBSERVATION=0
FORMAT_JSON=0

while [[ $# -gt 0 ]]; do
  case "$1" in
    --duration)
      DURATION="${2:-10m}"
      shift 2
      ;;
    --disable-observation)
      DISABLE_OBSERVATION=1
      shift
      ;;
    --format-json)
      FORMAT_JSON=1
      shift
      ;;
    *)
      echo "Unknown argument: $1" >&2
      echo "Usage: $0 [--duration <30s|5m|1h>] [--disable-observation] [--format-json]" >&2
      exit 2
      ;;
  esac
done

if [[ ! -f "${CONFIG_PATH}" ]]; then
  echo "Missing config file: ${CONFIG_PATH}" >&2
  exit 1
fi

bash "${REPO_ROOT}/apps/runtime-studio/scripts/bootstrap_runtime_studio.sh"

export OLLAMA_BASE_URL="${OLLAMA_BASE_URL:-http://127.0.0.1:11434}"
export OLLAMA_MODEL="${OLLAMA_MODEL:-llama3.1:latest}"

DAEMON_CMD=(
  dotnet run --project application/src/Nexo.CLI -- background-agent daemon
  --config "${CONFIG_PATH}"
  --duration "${DURATION}"
)

if [[ "${DISABLE_OBSERVATION}" -eq 1 ]]; then
  DAEMON_CMD+=(--disable-observation)
fi
if [[ "${FORMAT_JSON}" -eq 1 ]]; then
  DAEMON_CMD+=(--format-json)
fi

echo "Running Runtime Studio local agent set"
echo "  repo: ${REPO_ROOT}"
echo "  config: ${CONFIG_PATH}"
echo "  duration: ${DURATION}"
echo "  observation: $([[ "${DISABLE_OBSERVATION}" -eq 1 ]] && echo "disabled" || echo "enabled")"
echo "  ollama: ${OLLAMA_BASE_URL} (${OLLAMA_MODEL})"
echo

cd "${REPO_ROOT}"
"${DAEMON_CMD[@]}"
