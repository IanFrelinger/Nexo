#!/usr/bin/env bash
# Run the agentic Guide test with Ollama provided via Docker.
# Starts an Ollama container if needed, pulls a text model and a vision model (llava),
# then runs the test. The vision model is used to "see" the screen and identify
# buttons, inputs, and coordinates for human-like interaction.
#
# Prerequisites: Docker, display (do not set NEXO_SKIP_UI_TESTS=1)
#
# Usage:
#   ./scripts/run-guide-agentic-test-docker.sh
#
# Optional env: OLLAMA_MODEL (text, default llama3.2:3b), OLLAMA_VISION_MODEL (default llava:7b)
#
# Memory: Ollama in Docker needs enough RAM to load models (e.g. llama3.2:3b ~2.3 GiB,
# llava:7b ~4.3 GiB). If you see 500 errors, check: docker logs ollama | grep -E "Load failed|memory"
# Fix: give the container more memory (Docker Desktop → Settings → Resources → Memory ≥ 8GB)
# or run Ollama natively: ollama serve && ollama pull llava:7b && ollama pull llama3.2:3b
#
set -e
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

OLLAMA_IMAGE="${OLLAMA_IMAGE:-ollama/ollama}"
OLLAMA_CONTAINER="${OLLAMA_CONTAINER:-ollama}"
OLLAMA_MODEL="${OLLAMA_MODEL:-llama3.2:3b}"
OLLAMA_VISION_MODEL="${OLLAMA_VISION_MODEL:-llava:7b}"

# Start Ollama in Docker if not already running
if ! docker ps --format '{{.Names}}' | grep -qx "$OLLAMA_CONTAINER"; then
  if docker ps -a --format '{{.Names}}' | grep -qx "$OLLAMA_CONTAINER"; then
    echo "Starting existing container $OLLAMA_CONTAINER..."
    docker start "$OLLAMA_CONTAINER"
  else
    echo "Pulling $OLLAMA_IMAGE and starting $OLLAMA_CONTAINER..."
    docker run -d -p 11434:11434 --name "$OLLAMA_CONTAINER" "$OLLAMA_IMAGE"
  fi
  echo "Waiting for Ollama to be ready..."
  for i in $(seq 1 30); do
    if curl -s -o /dev/null -w "%{http_code}" "http://localhost:11434/api/tags" 2>/dev/null | grep -q 200; then
      break
    fi
    sleep 1
  done
fi

# Ensure text model is available
if ! curl -s "http://localhost:11434/api/tags" 2>/dev/null | grep -q "\"name\":\"$OLLAMA_MODEL\""; then
  echo "Pulling text model $OLLAMA_MODEL in container..."
  docker exec "$OLLAMA_CONTAINER" ollama pull "$OLLAMA_MODEL"
fi

# Ensure vision model is available (used to view the screen and find buttons/inputs)
if ! curl -s "http://localhost:11434/api/tags" 2>/dev/null | grep -q "\"name\":\"$OLLAMA_VISION_MODEL\""; then
  echo "Pulling vision model $OLLAMA_VISION_MODEL (for screen understanding)..."
  docker exec "$OLLAMA_CONTAINER" ollama pull "$OLLAMA_VISION_MODEL"
fi

export NEXO_RUN_AGENTIC_GUIDE_TEST=1
export OLLAMA_MODEL
export OLLAMA_VISION_MODEL
unset NEXO_SKIP_UI_TESTS

echo "=== Agentic Guide test (Ollama via Docker) ==="
echo "  Text model:  $OLLAMA_MODEL"
echo "  Vision model: $OLLAMA_VISION_MODEL (views screen, finds buttons)"
echo ""

dotnet test src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj -f net8.0 \
  --filter "FullyQualifiedName~Guide_Agentic_UniversalTester_ShouldTestAllInteractions_WhenRunWithOllama" \
  -p:TreatWarningsAsErrors=false \
  --logger "console;verbosity=normal"
