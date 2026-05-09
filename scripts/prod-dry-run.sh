#!/usr/bin/env bash
# Production-shaped container dry run: build & start existing Compose stacks (portal or agent-server),
# wait until Nexo.API responds, hit /health + /api/status, then tear down unless --keep-up.
#
# Usage:
#   ./scripts/prod-dry-run.sh [--portal|--agent-server] [--keep-up] [--no-build]
#
# Env:
#   NEXO_AGENT_SERVER_HTTP_PORT   Host port (default 8080; matches compose defaults)
#   NEXO_PROD_DRY_RUN_HOST        Loopback host (default 127.0.0.1)
#   NEXO_REPO_ROOT                  For agent-server: host path to repo root (default: script parent dir)

set -euo pipefail

usage() {
  echo "Usage: $0 [--portal|--agent-server] [--keep-up] [--no-build]"
  echo "  --portal         docker-compose.portal.yml (default)"
  echo "  --agent-server   docker-compose.agent-server.yml (mounted workspace)"
  echo "  --keep-up        leave containers running after checks"
  echo "  --no-build       skip docker compose build"
  exit 1
}

COMPOSE_FILE="docker-compose.portal.yml"
KEEP_UP=""
NO_BUILD=""

while [[ $# -gt 0 ]]; do
  case "$1" in
    --portal) COMPOSE_FILE="docker-compose.portal.yml"; shift ;;
    --agent-server) COMPOSE_FILE="docker-compose.agent-server.yml"; shift ;;
    --keep-up) KEEP_UP=1; shift ;;
    --no-build) NO_BUILD=1; shift ;;
    -h|--help) usage ;;
    *) echo "Unknown option: $1" >&2; usage ;;
  esac
done

REPO_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$REPO_ROOT"

export NEXO_REPO_ROOT="${NEXO_REPO_ROOT:-$REPO_ROOT}"

PORT="${NEXO_AGENT_SERVER_HTTP_PORT:-8080}"
HOST="${NEXO_PROD_DRY_RUN_HOST:-127.0.0.1}"
BASE_URL="http://${HOST}:${PORT}"

DC=(docker compose -f "$COMPOSE_FILE")

echo "=== Prod-shaped dry run ==="
echo "Compose: $COMPOSE_FILE"
echo "URL:     $BASE_URL"
echo ""

if [[ -z "${NO_BUILD:-}" ]]; then
  echo "Building..."
  "${DC[@]}" build
fi

echo "Starting stack..."
set +e
"${DC[@]}" up -d --wait 2>/dev/null
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
    echo "Timed out waiting for API. Recent nexo-api logs:" >&2
    "${DC[@]}" logs --tail 80 nexo-api >&2 || true
    exit 1
  fi
fi

echo "Checking /health ..."
curl -sfS "${BASE_URL}/health" | head -c 500 || {
  echo "FAIL: /health" >&2
  "${DC[@]}" logs --tail 80 nexo-api >&2 || true
  exit 1
}
echo ""

echo "Checking /api/status ..."
curl -sfS "${BASE_URL}/api/status" | head -c 800 || {
  echo "FAIL: /api/status" >&2
  exit 1
}
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
