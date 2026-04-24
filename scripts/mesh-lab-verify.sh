#!/usr/bin/env bash
# Verify the virtual mesh lab: host-published /health + cross-container HTTP via ephemeral curl.
# Prerequisites: Docker Compose v2, lab already `up` (see docs/MeshVirtualLab.md).
#
#   cp docs/config/mesh-lab.env.example .env.mesh-lab
#   docker compose -f docker-compose.mesh-lab.yml --env-file .env.mesh-lab up -d --build
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
  echo "Copy docs/config/mesh-lab.env.example to .env.mesh-lab and set Nexo__Security__ApiKey." >&2
  exit 1
fi

compose() {
  docker compose -f "$COMPOSE_FILE" --env-file "$COMPOSE_ENV_FILE" "$@"
}

lab_network() {
  local cid
  cid="$(compose ps -q peer-a)"
  if [[ -z "$cid" ]]; then
    echo "peer-a container not running" >&2
    exit 1
  fi
  # First attached network (lab compose attaches only to the mesh_lab bridge).
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

echo "== Wait: host → peer-a ($PEER_A_HOST/health) =="
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
  if curl -fsS "http://${PEER_B_HOST}/health" >/dev/null 2>&1; then
    echo "peer-b reachable on host after ${i} attempt(s)"
    break
  fi
  if [[ "$i" -eq 90 ]]; then
    echo "peer-b not reachable on http://${PEER_B_HOST}/health" >&2
    compose logs --tail 80 peer-b
    exit 1
  fi
  sleep 2
done

echo "== Cross-container: ephemeral curl → peer-a:8080/health =="
curl_on_lab_net "http://peer-a:8080/health" | head -c 240
echo

echo "== Cross-container: ephemeral curl → peer-b:8080/health =="
curl_on_lab_net "http://peer-b:8080/health" | head -c 240
echo

echo "== Cross-container: peer-b service name from same network (peer-b → peer-a) =="
# Re-use peer-b's network namespace via a one-off exec on peer-b if wget/curl exists; else skip.
if compose exec -T peer-b sh -c 'command -v wget >/dev/null 2>&1' 2>/dev/null; then
  compose exec -T peer-b wget -qO- "http://peer-a:8080/health" | head -c 240
  echo
elif compose exec -T peer-b sh -c 'command -v curl >/dev/null 2>&1' 2>/dev/null; then
  compose exec -T peer-b curl -fsS "http://peer-a:8080/health" | head -c 240
  echo
else
  echo "(skipped: no wget/curl inside peer-b image; ephemeral curl checks above are sufficient)"
fi

echo "== Mesh lab verify: OK =="
