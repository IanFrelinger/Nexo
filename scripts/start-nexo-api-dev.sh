#!/usr/bin/env bash
# Docker Ollama + Nexo.API on the host (Linux, macOS, WSL; Docker Desktop / Engine).
# Usage:
#   bash scripts/start-nexo-api-dev.sh
#   bash scripts/start-nexo-api-dev.sh --pull
#   bash scripts/start-nexo-api-dev.sh --model llama3.2 --api-port 8080
#   bash scripts/start-nexo-api-dev.sh --skip-ollama   # Ollama already running
set -euo pipefail

MODEL="llama3.1"
OLLAMA_PORT="11434"
API_PORT="8080"
SKIP_OLLAMA=0
PULL=0
COMPOSE_PROJECT_NAME="${COMPOSE_PROJECT_NAME:-}"
WAIT_SECONDS=120

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
COMPOSE_FILE="${ROOT}/docker-compose.ollama.yml"
OLLAMA_URL="http://127.0.0.1:${OLLAMA_PORT}"

usage() {
  echo "Usage: $0 [--model TAG] [--ollama-port N] [--api-port N] [--pull] [--skip-ollama] [--wait SEC] [--compose-project NAME]" >&2
  exit 2
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --model) MODEL="${2:-}"; shift 2 ;;
    --ollama-port) OLLAMA_PORT="${2:-}"; shift 2 ;;
    --api-port) API_PORT="${2:-}"; shift 2 ;;
    --pull) PULL=1; shift ;;
    --skip-ollama) SKIP_OLLAMA=1; shift ;;
    --wait) WAIT_SECONDS="${2:-}"; shift 2 ;;
    --compose-project) export COMPOSE_PROJECT_NAME="${2:-}"; shift 2 ;;
    -h|--help) usage ;;
    *) echo "Unknown option: $1" >&2; usage ;;
  esac
done

OLLAMA_URL="http://127.0.0.1:${OLLAMA_PORT}"
export NEXO_OLLAMA_HOST_PORT="${OLLAMA_PORT}"
cd "${ROOT}"

if [[ "${SKIP_OLLAMA}" -eq 0 ]]; then
  echo "Starting Ollama (Docker) on host port ${OLLAMA_PORT}..."
  docker compose -f "${COMPOSE_FILE}" up -d

  echo "Waiting for Ollama at ${OLLAMA_URL}..."
  ok=0
  for ((i = 0; i < WAIT_SECONDS; i++)); do
    if curl -sf "${OLLAMA_URL}/api/tags" >/dev/null 2>&1; then
      ok=1
      break
    fi
    sleep 1
  done
  if [[ "${ok}" -ne 1 ]]; then
    echo "Ollama did not become ready within ${WAIT_SECONDS}s. Try: docker compose -f docker-compose.ollama.yml logs ollama" >&2
    exit 1
  fi
  echo "Ollama is ready."

  if [[ "${PULL}" -eq 1 ]]; then
    echo "Pulling model '${MODEL}'..."
    docker compose -f "${COMPOSE_FILE}" exec ollama ollama pull "${MODEL}"
  fi
else
  echo "Skip Ollama: using ${OLLAMA_URL}"
fi

export OLLAMA_BASE_URL="${OLLAMA_URL}"
export OLLAMA_MODEL="${MODEL}"
export Nexo__NodeCapabilityRuntime__Ollama__BaseUrl="${OLLAMA_URL}"
export ASPNETCORE_URLS="http://127.0.0.1:${API_PORT}"
unset NEXO_BACKGROUND_AGENTS_CONFIG 2>/dev/null || true

cat <<EOF

Nexo.API → http://127.0.0.1:${API_PORT}  |  Ollama → ${OLLAMA_URL}  |  Model → ${MODEL}

Stop Ollama: docker compose -f docker-compose.ollama.yml down
Or: bash scripts/stop-nexo-api-dev.sh

EOF

dotnet run --project "${ROOT}/src/Nexo.API/Nexo.API.csproj"
