#!/usr/bin/env bash
# Start Ollama in Docker and pull a model. Usage: bash scripts/run-ollama-docker.sh [model] [port]
set -euo pipefail
MODEL="${1:-llama3.1:latest}"
PORT="${2:-11434}"
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
export NEXO_OLLAMA_HOST_PORT="${PORT}"
cd "${ROOT}"
echo "Starting Ollama container (port ${PORT})..."
docker compose -f deploy/compose/docker-compose.ollama.yml up -d
echo "Pulling model '${MODEL}'..."
docker compose -f deploy/compose/docker-compose.ollama.yml exec ollama ollama pull "${MODEL}"
cat <<EOF

Ollama: http://127.0.0.1:${PORT}

Start Nexo.API next (Ollama already up): bash scripts/start-nexo-api-dev.sh --skip-ollama

export OLLAMA_BASE_URL=http://127.0.0.1:${PORT}
export OLLAMA_MODEL=${MODEL}
export Nexo__NodeCapabilityRuntime__Ollama__BaseUrl=http://127.0.0.1:${PORT}

Stop: docker compose -f deploy/compose/docker-compose.ollama.yml down
EOF
