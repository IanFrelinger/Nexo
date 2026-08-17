#!/usr/bin/env bash
# Stop deploy/compose/docker-compose.ollama.yml; optionally free API port (Linux/macOS/WSL).
# Usage:
#   bash scripts/stop-nexo-api-dev.sh
#   bash scripts/stop-nexo-api-dev.sh --kill-api 8080
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
COMPOSE_FILE="${ROOT}/deploy/compose/docker-compose.ollama.yml"
KILL_API=0
API_PORT="8080"
KEEP_OLLAMA=0

while [[ $# -gt 0 ]]; do
  case "$1" in
    --kill-api)
      KILL_API=1
      API_PORT="${2:-8080}"
      shift 2
      ;;
    --keep-ollama) KEEP_OLLAMA=1; shift ;;
    *)
      echo "Unknown option: $1 (use --kill-api PORT, --keep-ollama)" >&2
      exit 2
      ;;
  esac
done

if [[ "${KEEP_OLLAMA}" -eq 0 ]]; then
  echo "Stopping Ollama stack..."
  docker compose -f "${COMPOSE_FILE}" down 2>/dev/null || true
  echo "Done (named volume ollama-models is kept)."
fi

if [[ "${KILL_API}" -eq 1 ]]; then
  if command -v fuser >/dev/null 2>&1; then
    fuser -k "${API_PORT}/tcp" 2>/dev/null || true
  elif command -v lsof >/dev/null 2>&1; then
    pids="$(lsof -t -i ":${API_PORT}" -sTCP:LISTEN 2>/dev/null || true)"
    if [[ -n "${pids}" ]]; then
      kill ${pids} 2>/dev/null || true
    fi
  else
    echo "Install fuser or lsof to use --kill-api on this system." >&2
  fi
fi
