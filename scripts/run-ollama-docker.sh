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

Start Nexo.API next (Ollama already up; sets OLLAMA_* env + NCR URL, then dotnet run):
  bash scripts/start-nexo-api-dev.sh --skip-ollama --model "${MODEL}" --ollama-port ${PORT}
  powershell -File scripts/start-nexo-api-dev.ps1 -SkipOllama -Model "${MODEL}" -OllamaPort ${PORT}

Or manually:
  export OLLAMA_BASE_URL=http://127.0.0.1:${PORT}
  export OLLAMA_MODEL=${MODEL}
  export Nexo__NodeCapabilityRuntime__Ollama__BaseUrl=http://127.0.0.1:${PORT}
  dotnet run --project application/src/Nexo.API/Nexo.API.csproj -f net10.0

Stop: docker compose -f deploy/compose/docker-compose.ollama.yml down
EOF
