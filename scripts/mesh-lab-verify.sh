#!/usr/bin/env bash
# Verify heterogeneous mesh lab: peer-a (ApiKey), peer-b (ApiKey or Bearer), optional worker /health.
#
#   ./scripts/mesh-lab-verify.sh .env.mesh-lab

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

COMPOSE_FILE="${COMPOSE_FILE:-docker-compose.mesh-lab.yml}"
COMPOSE_ENV_FILE="${1:-${COMPOSE_ENV_FILE:-.env.mesh-lab}}"
PEER_A_HOST="${MESH_LAB_PEER_A_HOST:-127.0.0.1:18081}"
PEER_B_HOST="${MESH_LAB_PEER_B_HOST:-127.0.0.1:18082}"
CURL_IMAGE="${MESH_LAB_CURL_IMAGE:-curlimages/curl:8.5.0}"

if [[ ! -f "$COMPOSE_ENV_FILE" ]]; then
  echo "Missing env file: $COMPOSE_ENV_FILE" >&2
  echo "Copy docs/config/mesh-lab.env.example to .env.mesh-lab and set secrets." >&2
  exit 1
fi

# shellcheck disable=SC1090
source_env_kv() {
  local key=$1
  grep -E "^${key}=" "$COMPOSE_ENV_FILE" 2>/dev/null | head -1 | cut -d= -f2- | tr -d '\r' || true
}

API_KEY="$(source_env_kv Nexo__Security__ApiKey)"
BEARER="$(source_env_kv Nexo__Security__PeerB__BearerToken)"
BASIC_USER="$(source_env_kv Nexo__Security__Worker__BasicAuthUsername)"
BASIC_PASS="$(source_env_kv Nexo__Security__Worker__BasicAuthPassword)"

compose() {
  docker compose -f "$COMPOSE_FILE" --env-file "$COMPOSE_ENV_FILE" "$@"
}

compose_workers() {
  docker compose --profile workers -f "$COMPOSE_FILE" --env-file "$COMPOSE_ENV_FILE" "$@"
}

lab_network() {
  local cid
  cid="$(compose ps -q peer-a)"
  if [[ -z "$cid" ]]; then
    echo "peer-a container not running" >&2
    exit 1
  fi
  docker inspect "$cid" --format '{{range $k,$v := .NetworkSettings.Networks}}{{$k}} {{end}}' | awk '{print $1; exit}'
}

curl_on_lab_net() {
  local url=$1
  local net
  net="$(lab_network)"
  if [[ -z "$net" ]]; then
    echo "Could not resolve lab Docker network from peer-a" >&2
    exit 1
  fi
  docker run --rm --network "$net" "$CURL_IMAGE" -fsS "$url"
}

echo "== Wait: host → peer-a ($PEER_A_HOST/health) [expect ApiKey path optional for GET] =="
for i in $(seq 1 90); do
  if curl -fsS "http://${PEER_A_HOST}/health" >/dev/null 2>&1; then
    echo "peer-a reachable on host after ${i} attempt(s)"
    break
  fi
  if [[ "$i" -eq 90 ]]; then
    echo "peer-a not reachable on http://${PEER_A_HOST}/health" >&2
    compose logs --tail 80 peer-a
    exit 1
  fi
  sleep 2
done

echo "== Wait: host → peer-b ($PEER_B_HOST/health) =="
for i in $(seq 1 90); do
  if [[ -n "$API_KEY" ]] && curl -fsS -H "X-Nexo-Api-Key: ${API_KEY}" "http://${PEER_B_HOST}/health" >/dev/null 2>&1; then
    echo "peer-b reachable (API key) after ${i} attempt(s)"
    break
  fi
  if [[ -n "$BEARER" ]] && curl -fsS -H "Authorization: Bearer ${BEARER}" "http://${PEER_B_HOST}/health" >/dev/null 2>&1; then
    echo "peer-b reachable (Bearer) after ${i} attempt(s)"
    break
  fi
  if curl -fsS "http://${PEER_B_HOST}/health" >/dev/null 2>&1; then
    echo "peer-b reachable (no auth) after ${i} attempt(s)"
    break
  fi
  if [[ "$i" -eq 90 ]]; then
    echo "peer-b not reachable on http://${PEER_B_HOST}/health" >&2
    compose logs --tail 80 peer-b
    exit 1
  fi
  sleep 2
done

echo "== Cross-container: curl → peer-a:8080/health =="
curl_on_lab_net "http://peer-a:8080/health" | head -c 240
echo

echo "== Cross-container: curl → peer-b:8080/health =="
curl_on_lab_net "http://peer-b:8080/health" | head -c 240
echo

if [[ -n "$BEARER" ]]; then
  echo "== Host → peer-b /health with Bearer only (validates second auth path) =="
  curl -fsS -H "Authorization: Bearer ${BEARER}" "http://${PEER_B_HOST}/health" | head -c 200
  echo
fi

if compose_workers ps -q worker 2>/dev/null | grep -q .; then
  echo "== Worker tier: in-network /health (GET, no auth) =="
  curl_on_lab_net "http://worker:8080/health" | head -c 200
  echo
else
  echo "(worker tier not running; use --profile workers to include workers)"
fi

echo "== Mesh lab verify: OK =="
