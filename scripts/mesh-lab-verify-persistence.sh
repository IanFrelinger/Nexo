#!/usr/bin/env bash
# Director LiteDB persistence: fleet + tasks survive peer-a container restart.
#
#   ./scripts/mesh-lab-verify-persistence.sh .env.mesh-lab
#
# Requires peer-a with Nexo__Mesh__Persistence__Provider=LiteDb (compose default).

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

# shellcheck source=scripts/mesh-lab-fleet.sh
source "${ROOT}/scripts/mesh-lab-fleet.sh"

COMPOSE_FILE="${COMPOSE_FILE:-docker-compose.mesh-lab.yml}"
COMPOSE_ENV_FILE="${1:-${COMPOSE_ENV_FILE:-.env.mesh-lab}}"
PEER_A_HOST="${MESH_LAB_PEER_A_HOST:-127.0.0.1:18081}"
PERSIST_PEER_ID="${MESH_LAB_PERSIST_PEER_ID:-mesh-lab-persist-peer}"

if [[ ! -f "$COMPOSE_ENV_FILE" ]]; then
  echo "Missing env file: $COMPOSE_ENV_FILE" >&2
  exit 1
fi

source_env_kv() {
  local key=$1
  grep -E "^${key}=" "$COMPOSE_ENV_FILE" 2>/dev/null | head -1 | cut -d= -f2- | tr -d '\r' || true
}

API_KEY="$(source_env_kv Nexo__Security__ApiKey)"
MESH_LAB_PEER_REGISTRATION_KEY="$(source_env_kv MESH_LAB_PEER_REGISTRATION_KEY)"
export MESH_LAB_PEER_REGISTRATION_KEY
PERSIST_PROVIDER="$(source_env_kv Nexo__Mesh__Persistence__Provider)"
[[ -n "$PERSIST_PROVIDER" ]] || PERSIST_PROVIDER="LiteDb"

if [[ -z "$API_KEY" ]]; then
  echo "(Skipping persistence verify: no Nexo__Security__ApiKey in env file)"
  exit 0
fi

if [[ "$(echo "${PERSIST_PROVIDER}" | tr '[:upper:]' '[:lower:]')" != "litedb" ]]; then
  echo "(Skipping persistence verify: Nexo__Mesh__Persistence__Provider is not LiteDb)"
  exit 0
fi

compose() {
  docker compose -f "$COMPOSE_FILE" --env-file "$COMPOSE_ENV_FILE" "$@"
}

mesh_post() {
  curl -fsS -X POST -H "Content-Type: application/json" -H "X-Nexo-Api-Key: ${API_KEY}" "$@"
}

mesh_delete() {
  curl -fsS -X DELETE -H "X-Nexo-Api-Key: ${API_KEY}" "$@"
}

wait_peer_a() {
  local i
  for i in $(seq 1 60); do
    if curl -fsS -m 3 "http://${PEER_A_HOST}/health" >/dev/null 2>&1; then
      return 0
    fi
    sleep 1
  done
  echo "peer-a not healthy after restart" >&2
  return 1
}

echo "== Mesh director persistence (restart peer-a, ${PEER_A_HOST}) =="

mesh_delete "http://${PEER_A_HOST}/api/mesh/fleet/nodes/${PERSIST_PEER_ID}" >/dev/null 2>&1 || true

mesh_post -d "$(mesh_lab_fleet_register_json "$PERSIST_PEER_ID" http://peer-b:8080 Trusted)" \
  "http://${PEER_A_HOST}/api/mesh/fleet/nodes" >/dev/null

TASK_JSON="$(mesh_post -d '{"name":"mesh-lab-persist-task","steps":1,"idempotencyKey":"mesh-lab-persist-idem"}' \
  "http://${PEER_A_HOST}/api/mesh/tasks")"
TASK_ID="$(echo "$TASK_JSON" | python3 -c 'import json,sys; print(json.load(sys.stdin)["taskId"])')"

echo "Seeded fleet node ${PERSIST_PEER_ID} and task ${TASK_ID} — restarting peer-a"
compose restart peer-a >/dev/null
wait_peer_a

NODES_JSON="$(curl -fsS -H "X-Nexo-Api-Key: ${API_KEY}" "http://${PEER_A_HOST}/api/mesh/fleet/nodes")"
if [[ "$NODES_JSON" != *"${PERSIST_PEER_ID}"* ]]; then
  echo "Fleet node missing after restart (got: ${NODES_JSON:0:300}…)" >&2
  exit 1
fi
echo "Fleet registry survived restart — OK"

TASK_GET="$(curl -fsS -H "X-Nexo-Api-Key: ${API_KEY}" "http://${PEER_A_HOST}/api/mesh/tasks/${TASK_ID}")"
echo "$TASK_GET" | python3 -c '
import json, sys
t = json.load(sys.stdin)
if t.get("taskId") != sys.argv[1]:
    sys.stderr.write("Task id mismatch after restart\n")
    sys.exit(1)
if t.get("name") != "mesh-lab-persist-task":
    sys.stderr.write("Task name mismatch after restart\n")
    sys.exit(1)
if t.get("idempotencyKey") != "mesh-lab-persist-idem":
    sys.stderr.write("Idempotency key mismatch after restart\n")
    sys.exit(1)
print("Mesh task survived restart — OK")
' "$TASK_ID"

mesh_delete "http://${PEER_A_HOST}/api/mesh/fleet/nodes/${PERSIST_PEER_ID}" >/dev/null 2>&1 || true

echo "== Mesh lab verify-persistence: OK =="
