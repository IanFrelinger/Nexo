#!/usr/bin/env bash
# Phase 13: mesh task retry (alternate peer) + GET /result download.
#
#   ./scripts/mesh-lab-verify-retry-result.sh .env.mesh-lab

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

# shellcheck source=scripts/mesh-lab-fleet.sh
source "${ROOT}/scripts/mesh-lab-fleet.sh"

COMPOSE_FILE="${COMPOSE_FILE:-deploy/compose/docker-compose.mesh-lab.yml}"
COMPOSE_ENV_FILE="${1:-${COMPOSE_ENV_FILE:-.env.mesh-lab}}"
PEER_A_HOST="${MESH_LAB_PEER_A_HOST:-127.0.0.1:18081}"

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
if [[ -z "$API_KEY" ]]; then
  echo "(Skipping retry/result verify: no Nexo__Security__ApiKey in env file)"
  exit 0
fi

compose() {
  docker compose -f "$COMPOSE_FILE" --env-file "$COMPOSE_ENV_FILE" "$@"
}

mesh_post() {
  curl -fsS -X POST -H "Content-Type: application/json" -H "X-Nexo-Api-Key: ${API_KEY}" "$@"
}

mesh_delete() {
  curl -fsS -X DELETE -H "X-Nexo-Api-Key: ${API_KEY}" "$@" >/dev/null 2>&1 || true
}

echo "== Mesh lab retry + result artifact =="

for pid in mesh-lab-retry-a mesh-lab-retry-b mesh-lab-verify-peer; do
  mesh_delete "http://${PEER_A_HOST}/api/mesh/fleet/nodes/${pid}"
done

# peer-b (low queue) vs peer-a self (high queue) — retry should skip first pee
mesh_post -d "$(mesh_lab_fleet_register_json mesh-lab-retry-a http://peer-b:8080 Trusted 0)" \
  "http://${PEER_A_HOST}/api/mesh/fleet/nodes" >/dev/null
mesh_post -d "$(mesh_lab_fleet_register_json mesh-lab-retry-b http://peer-a:8080 Trusted 99)" \
  "http://${PEER_A_HOST}/api/mesh/fleet/nodes" >/dev/null

TASK_JSON="$(mesh_post -d '{"name":"mesh-lab-retry-task","steps":1}' \
  "http://${PEER_A_HOST}/api/mesh/tasks")"
TASK_ID="$(echo "$TASK_JSON" | python3 -c 'import json,sys; print(json.load(sys.stdin)["taskId"])')"

SCHED_JSON="$(mesh_post -d '{}' "http://${PEER_A_HOST}/api/mesh/tasks/${TASK_ID}/schedule")"
FIRST_PEER="$(echo "$SCHED_JSON" | python3 -c 'import json,sys; print(json.load(sys.stdin).get("assignedPeerId") or "")')"
if [[ "$FIRST_PEER" != "mesh-lab-retry-a" ]]; then
  echo "Expected first schedule on mesh-lab-retry-a (queue 0), got ${FIRST_PEER}" >&2
  exit 1
fi
echo "Initial placement on mesh-lab-retry-a — OK"

RETRY_JSON="$(mesh_post -d '{}' "http://${PEER_A_HOST}/api/mesh/tasks/${TASK_ID}/retry")"
echo "$RETRY_JSON" | python3 -c '
import json, sys
t = json.load(sys.stdin)
st = t.get("status")
if st not in (1, "Assigned"):
    sys.stderr.write("Retry expected Assigned, got %r\n" % (st,))
    sys.exit(1)
peer = t.get("assignedPeerId") or ""
if peer != "mesh-lab-retry-b":
    sys.stderr.write("Retry expected mesh-lab-retry-b, got %r\n" % (peer,))
    sys.exit(1)
if (t.get("attemptCount") or 0) < 2:
    sys.stderr.write("Expected attemptCount >= 2 after retry, got %s\n" % (t.get("attemptCount"),))
    sys.exit(1)
print("Retry placed on alternate peer — OK")
'

LEASE="$(echo "$RETRY_JSON" | python3 -c 'import json,sys; print(json.load(sys.stdin).get("leaseToken") or "")')"
curl -fsS -X PATCH -H "Content-Type: application/json" -H "X-Nexo-Api-Key: ${API_KEY}" \
  -d "{\"status\":2,\"leaseToken\":\"${LEASE}\"}" \
  "http://${PEER_A_HOST}/api/mesh/tasks/${TASK_ID}/status" >/dev/null

RESULT_FILE="/tmp/mesh-lab-result-${TASK_ID}.bin"
RESULT_BYTES="mesh-lab-result-payload-${TASK_ID}"
PEER_A_CID="$(compose ps -q peer-a | head -1 | tr -d '\r\n')"
if [[ -z "$PEER_A_CID" ]]; then
  echo "peer-a container not running for result file seed" >&2
  exit 1
fi
printf '%s' "$RESULT_BYTES" | docker exec -i "$PEER_A_CID" tee "$RESULT_FILE" >/dev/null

curl -fsS -X PATCH -H "Content-Type: application/json" -H "X-Nexo-Api-Key: ${API_KEY}" \
  -d "{\"status\":3,\"leaseToken\":\"${LEASE}\",\"resultSummary\":\"mesh-lab-retry-result\",\"resultHandle\":\"${RESULT_FILE}\"}" \
  "http://${PEER_A_HOST}/api/mesh/tasks/${TASK_ID}/status" >/dev/null

DOWNLOAD="$(curl -fsS -H "X-Nexo-Api-Key: ${API_KEY}" \
  "http://${PEER_A_HOST}/api/mesh/tasks/${TASK_ID}/result")"
if [[ "$DOWNLOAD" != "$RESULT_BYTES" ]]; then
  echo "Result download mismatch (expected ${#RESULT_BYTES} bytes, got ${#DOWNLOAD})" >&2
  exit 1
fi
echo "GET /api/mesh/tasks/{id}/result download — OK"

# Verify missing handle on fresh task:
TASK2="$(mesh_post -d '{"name":"mesh-lab-no-result","steps":1}' \
  "http://${PEER_A_HOST}/api/mesh/tasks")"
TASK2_ID="$(echo "$TASK2" | python3 -c 'import json,sys; print(json.load(sys.stdin)["taskId"])')"
BAD_CODE="$(curl -s -o /dev/null -w '%{http_code}' -H "X-Nexo-Api-Key: ${API_KEY}" \
  "http://${PEER_A_HOST}/api/mesh/tasks/${TASK2_ID}/result")"
if [[ "$BAD_CODE" != "400" ]]; then
  echo "Expected 400 downloading result without handle, got ${BAD_CODE}" >&2
  exit 1
fi
echo "Result without handle returns 400 — OK"

for pid in mesh-lab-retry-a mesh-lab-retry-b mesh-lab-verify-peer; do
  mesh_delete "http://${PEER_A_HOST}/api/mesh/fleet/nodes/${pid}"
done
mesh_post -d "$(mesh_lab_fleet_register_json mesh-lab-verify-peer http://peer-b:8080 Trusted)" \
  "http://${PEER_A_HOST}/api/mesh/fleet/nodes" >/dev/null

echo "== Mesh lab verify-retry-result: OK =="
