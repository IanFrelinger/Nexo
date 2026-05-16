#!/usr/bin/env bash
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../../.." && pwd)"
CONFIG_PATH="${REPO_ROOT}/apps/runtime-studio/config/agent_set.game_director.local.json"

DURATION="15m"
DISABLE_OBSERVATION=0
FORMAT_JSON=0
TEST_FILTER=""
OLLAMA_MODEL_OVERRIDE=""
if [[ -n "${NEXO_GAME_TEST_FILTER:-}" ]]; then
  TEST_FILTER="${NEXO_GAME_TEST_FILTER}"
fi

while [[ $# -gt 0 ]]; do
  case "$1" in
    --duration)
      DURATION="${2:-15m}"
      shift 2
      ;;
    --test-filter)
      TEST_FILTER="${2:-}"
      shift 2
      ;;
    --ollama-model)
      OLLAMA_MODEL_OVERRIDE="${2:-}"
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
      echo "Usage: $0 [--duration <30s|5m|1h>] [--test-filter <expr>] [--ollama-model <model>] [--disable-observation] [--format-json]" >&2
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
if [[ -n "${OLLAMA_MODEL_OVERRIDE}" ]]; then
  export OLLAMA_MODEL="${OLLAMA_MODEL_OVERRIDE}"
else
  export OLLAMA_MODEL="${OLLAMA_MODEL:-llama3.1:latest}"
fi

TMP_CONFIG="${REPO_ROOT}/.nexo/agents/workspaces/runtime-studio/agent_set.game_director.runtime.json"
cp "${CONFIG_PATH}" "${TMP_CONFIG}"

if [[ -n "${TEST_FILTER}" ]]; then
  export TEST_FILTER
  python - "$TMP_CONFIG" <<'PY'
import json
import os
import sys

config_path = sys.argv[1]
test_filter = os.environ.get("TEST_FILTER", "")

with open(config_path, "r", encoding="utf-8") as f:
    payload = json.load(f)

for agent in payload.get("BackgroundAgents", {}).get("Agents", []):
    if agent.get("Id") == "game-worker-test-automation":
        params = agent.setdefault("Parameters", {})
        params["Filter"] = test_filter

with open(config_path, "w", encoding="utf-8") as f:
    json.dump(payload, f, indent=2)
PY
fi

DAEMON_CMD=(
  dotnet run --project application/src/Nexo.CLI -- background-agent daemon
  --config "${TMP_CONFIG}"
  --duration "${DURATION}"
)

if [[ "${DISABLE_OBSERVATION}" -eq 1 ]]; then
  DAEMON_CMD+=(--disable-observation)
fi
if [[ "${FORMAT_JSON}" -eq 1 ]]; then
  DAEMON_CMD+=(--format-json)
fi

echo "Running Runtime Studio game-director agent set"
echo "  repo: ${REPO_ROOT}"
echo "  config: ${TMP_CONFIG}"
echo "  duration: ${DURATION}"
echo "  observation: $([[ "${DISABLE_OBSERVATION}" -eq 1 ]] && echo "disabled" || echo "enabled")"
echo "  ollama: ${OLLAMA_BASE_URL} (${OLLAMA_MODEL})"
if [[ -n "${TEST_FILTER}" ]]; then
  echo "  test-filter: ${TEST_FILTER}"
fi
echo

cd "${REPO_ROOT}"
"${DAEMON_CMD[@]}"
