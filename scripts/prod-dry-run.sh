#!/usr/bin/env bash
# Production-shaped container dry run: build & start existing Compose stacks (portal or agent-server),
# wait until Ashlar.API responds, hit /health + /api/status, then tear down unless --keep-up.
#
# Usage:
#   ./scripts/prod-dry-run.sh [--portal|--agent-server] [--keep-up] [--no-build]
#
# Env:
#   ASHLAR_AGENT_SERVER_HTTP_PORT   Host port (default 8080; matches compose defaults)
#   ASHLAR_PROD_DRY_RUN_HOST        Loopback host (default 127.0.0.1)
#   ASHLAR_REPO_ROOT                  For agent-server: host path to repo root (default: script parent dir)

set -euo pipefail

usage() {
  echo "Usage: $0 [--portal|--agent-server] [--keep-up] [--no-build]"
  echo "  --portal         deploy/compose/docker-compose.portal.yml (default)"
  echo "  --agent-server   deploy/compose/docker-compose.agent-server.yml (mounted workspace)"
  echo "  --keep-up        leave containers running after checks"
  echo "  --no-build       skip docker compose build"
  exit 1
}

COMPOSE_FILE="deploy/compose/docker-compose.portal.yml"
KEEP_UP=""
NO_BUILD=""

while [[ $# -gt 0 ]]; do
  case "$1" in
    --portal) COMPOSE_FILE="deploy/compose/docker-compose.portal.yml"; shift ;;
    --agent-server) COMPOSE_FILE="deploy/compose/docker-compose.agent-server.yml"; shift ;;
    --keep-up) KEEP_UP=1; shift ;;
    --no-build) NO_BUILD=1; shift ;;
    -h|--help) usage ;;
    *) echo "Unknown option: $1" >&2; usage ;;
  esac
done

REPO_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$REPO_ROOT"

# grpc.tools protoc may segfault on linux_arm64; amd64 build is CI-default (QEMU on ARM Macs).
export DOCKER_DEFAULT_PLATFORM="${DOCKER_DEFAULT_PLATFORM:-linux/amd64}"

export ASHLAR_REPO_ROOT="${ASHLAR_REPO_ROOT:-$REPO_ROOT}"

PORT="${ASHLAR_AGENT_SERVER_HTTP_PORT:-8080}"
HOST="${ASHLAR_PROD_DRY_RUN_HOST:-127.0.0.1}"
BASE_URL="http://${HOST}:${PORT}"

DC=(docker compose -f "$COMPOSE_FILE")

if ! command -v docker >/dev/null 2>&1; then
  echo "prod-dry-run requires Docker (docker compose). Install Docker Engine and the Compose plugin, then retry." >&2
  exit 2
fi

echo "=== Prod-shaped dry run ==="
echo "Compose: $COMPOSE_FILE"
echo "URL:     $BASE_URL"
echo ""

if [[ -z "${NO_BUILD:-}" ]]; then
  echo "Building..."
  "${DC[@]}" build
fi

echo "Starting stack..."
# Keep stderr: a HEALTHCHECK that never goes healthy used to be swallowed here and burn ~90 s
# in the fallback loop with no hint why.
set +e
"${DC[@]}" up -d --wait
WAIT_RC=$?
set -e

if [[ "$WAIT_RC" -ne 0 ]]; then
  "${DC[@]}" up -d
  echo "Waiting for ${BASE_URL}/health (fallback; first boot may pull models)..."
  ok=""
  for _ in $(seq 1 90); do
    if curl -sf "${BASE_URL}/health" >/dev/null 2>&1; then
      ok=1
      break
    fi
    sleep 2
  done
  if [[ -z "$ok" ]]; then
    echo "Timed out waiting for API. Recent ashlar-api logs:" >&2
    "${DC[@]}" logs --tail 80 ashlar-api >&2 || true
    exit 1
  fi
fi

echo "Checking /health ..."
curl -sfS "${BASE_URL}/health" | head -c 500 || {
  echo "FAIL: /health" >&2
  "${DC[@]}" logs --tail 80 ashlar-api >&2 || true
  exit 1
}
echo ""

echo "Checking /api/status ..."
curl -sfS "${BASE_URL}/api/status" | head -c 800 || {
  echo "FAIL: /api/status" >&2
  exit 1
}
echo ""

# The default (MEAI) model path resolves its Ollama endpoint separately from the provider factory
# that backs the "ollama" provider flag on this endpoint. Inside a container "localhost" is the
# container itself, so a loopback endpoint here means model calls get connection-refused while the
# status page still says Ollama is available. POST /api/orchestrate cannot expose that (non-Development
# hosts mask exception detail), so assert on the resolved endpoint instead.
echo "Checking /api/onboarding/status (default model path endpoint) ..."
ONBOARDING_JSON="$(curl -sfS "${BASE_URL}/api/onboarding/status")" || {
  echo "FAIL: /api/onboarding/status" >&2
  exit 1
}
MODEL_BASE_URL="$(printf '%s' "$ONBOARDING_JSON" | sed -n 's/.*"meaiOllamaBaseUrl":"\([^"]*\)".*/\1/p')"
if [[ -z "$MODEL_BASE_URL" ]]; then
  echo "Default model path reports no Ollama endpoint (MEAI pipeline opted out); loopback check skipped."
else
  echo "Default model path Ollama endpoint: ${MODEL_BASE_URL}"
  case "$MODEL_BASE_URL" in
    *://localhost|*://localhost:*|*://localhost/*|*://127.0.0.1|*://127.0.0.1:*|*://127.0.0.1/*|*://\[::1\]*)
      echo "FAIL: default model path would dial the container's own loopback (${MODEL_BASE_URL})." >&2
      echo "      Set Ashlar__Meai__OllamaBaseUrl (or OLLAMA_BASE_URL) to the ollama service in ${COMPOSE_FILE}." >&2
      exit 1 ;;
  esac
fi
echo ""

echo "Prod-shaped dry run OK (${COMPOSE_FILE})."

if [[ -z "${KEEP_UP:-}" ]]; then
  echo "Stopping stack..."
  "${DC[@]}" down
  echo "Done. Use --keep-up to leave containers running for manual checks."
else
  echo "Stack left running (--keep-up). Tear down with:"
  echo "  docker compose -f ${COMPOSE_FILE} down"
fi
