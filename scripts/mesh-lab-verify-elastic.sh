#!/usr/bin/env bash
# Phase 13: elastic placement (queue depth + heartbeat) and optional rebalancer.
#
#   ./scripts/mesh-lab-verify-elastic.sh .env.mesh-lab
#   MESH_LAB_ELASTIC_REBALANCER=1  # enable rebalancer check (slower; compose must set Elastic:Enabled)

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

# shellcheck source=scripts/mesh-lab-fleet.sh
source "${ROOT}/scripts/mesh-lab-fleet.sh"

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

API_KEY="$(source_env_kv Ashlar__Security__ApiKey)"
MESH_LAB_PEER_REGISTRATION_KEY="$(source_env_kv MESH_LAB_PEER_REGISTRATION_KEY)"
export MESH_LAB_PEER_REGISTRATION_KEY
if [[ -z "$API_KEY" ]]; then
  echo "(Skipping elastic verify: no Ashlar__Security__ApiKey in env file)"
  exit 0
fi

mesh_post() {
  curl -fsS -X POST -H "Content-Type: application/json" -H "X-Ashlar-Api-Key: ${API_KEY}" "$@"
}

mesh_delete() {
  curl -fsS -X DELETE -H "X-Ashlar-Api-Key: ${API_KEY}" "$@" >/dev/null 2>&1 || true
}

echo "== Mesh lab elastic placement (queue depth + heartbeat) =="

for pid in mesh-lab-elastic-low mesh-lab-elastic-high mesh-lab-verify-peer \
  mesh-lab-retry-a mesh-lab-retry-b mesh-lab-trust-trusted mesh-lab-trust-untrusted \
  mesh-lab-gov-peer mesh-lab-persist-peer; do
  mesh_delete "http://${PEER_A_HOST}/api/mesh/fleet/nodes/${pid}"
done

mesh_post -d "$(mesh_lab_fleet_register_json mesh-lab-elastic-high http://peer-b:8080 Trusted 50)" \
  "http://${PEER_A_HOST}/api/mesh/fleet/nodes" >/dev/null
mesh_post -d "$(mesh_lab_fleet_register_json mesh-lab-elastic-low http://peer-a:8080 Trusted 0)" \
  "http://${PEER_A_HOST}/api/mesh/fleet/nodes" >/dev/null

TASK_JSON="$(mesh_post -d '{"name":"mesh-lab-elastic-pick","steps":1}' \
  "http://${PEER_A_HOST}/api/mesh/tasks")"
TASK_ID="$(echo "$TASK_JSON" | python3 -c 'import json,sys; print(json.load(sys.stdin)["taskId"])')"
SCHED_JSON="$(mesh_post -d '{}' "http://${PEER_A_HOST}/api/mesh/tasks/${TASK_ID}/schedule")"
PICK="$(echo "$SCHED_JSON" | python3 -c 'import json,sys; print(json.load(sys.stdin).get("assignedPeerId") or "")')"
if [[ "$PICK" != "mesh-lab-elastic-low" ]]; then
  echo "Expected placement on lowest queue depth (mesh-lab-elastic-low), got ${PICK}" >&2
  exit 1
fi
echo "Queue-depth placement picked mesh-lab-elastic-low — OK"

curl -fsS -X POST -H "Content-Type: application/json" -H "X-Ashlar-Api-Key: ${API_KEY}" \
  -d '{"queueDepth":0}' \
  "http://${PEER_A_HOST}/api/mesh/fleet/nodes/mesh-lab-elastic-high/heartbeat" >/dev/null
curl -fsS -X POST -H "Content-Type: application/json" -H "X-Ashlar-Api-Key: ${API_KEY}" \
  -d '{"queueDepth":80}' \
  "http://${PEER_A_HOST}/api/mesh/fleet/nodes/mesh-lab-elastic-low/heartbeat" >/dev/null

TASK2_JSON="$(mesh_post -d '{"name":"mesh-lab-elastic-heartbeat","steps":1}' \
  "http://${PEER_A_HOST}/api/mesh/tasks")"
TASK2_ID="$(echo "$TASK2_JSON" | python3 -c 'import json,sys; print(json.load(sys.stdin)["taskId"])')"
SCHED2="$(mesh_post -d '{}' "http://${PEER_A_HOST}/api/mesh/tasks/${TASK2_ID}/schedule")"
PICK2="$(echo "$SCHED2" | python3 -c 'import json,sys; print(json.load(sys.stdin).get("assignedPeerId") or "")')"
if [[ "$PICK2" != "mesh-lab-elastic-high" ]]; then
  echo "After heartbeat swap, expected mesh-lab-elastic-high, got ${PICK2}" >&2
  exit 1
fi
echo "Heartbeat queueDepth influences placement — OK"

ELASTIC_JSON="$(curl -fsS -H "X-Ashlar-Api-Key: ${API_KEY}" \
  "http://${PEER_A_HOST}/api/mesh/elastic/status")"
echo "$ELASTIC_JSON" | python3 -c '
import json, sys
d = json.load(sys.stdin)
workers = d.get("workers") or d.get("Workers") or []
if len(workers) < 2:
    sys.stderr.write(f"elastic/status expected >= 2 workers, got {len(workers)}\n")
    sys.exit(1)
print(f"GET /api/mesh/elastic/status ({len(workers)} workers) — OK")
'

REBALANCER="$(source_env_kv MESH_LAB_ELASTIC_REBALANCER)"
if [[ "${REBALANCER:-}" == "1" || "${REBALANCER:-}" == "true" ]]; then
  echo "-- Elastic rebalancer (MESH_LAB_ELASTIC_REBALANCER=1) --"
  mesh_delete "http://${PEER_A_HOST}/api/mesh/fleet/nodes/mesh-lab-elastic-low"
  mesh_delete "http://${PEER_A_HOST}/api/mesh/fleet/nodes/mesh-lab-elastic-high"

  PEND_JSON="$(mesh_post -d '{"name":"mesh-lab-elastic-rebal","steps":1}' \
    "http://${PEER_A_HOST}/api/mesh/tasks")"
  PEND_ID="$(echo "$PEND_JSON" | python3 -c 'import json,sys; print(json.load(sys.stdin)["taskId"])')"
  BLOCK="$(curl -s -o /dev/null -w '%{http_code}' -X POST \
    -H "Content-Type: application/json" -H "X-Ashlar-Api-Key: ${API_KEY}" \
    -d '{}' "http://${PEER_A_HOST}/api/mesh/tasks/${PEND_ID}/schedule")"
  if [[ "$BLOCK" != "400" ]]; then
    echo "Expected 400 schedule with empty fleet, got ${BLOCK}" >&2
    exit 1
  fi

  mesh_post -d "$(mesh_lab_fleet_register_json mesh-lab-elastic-low http://peer-a:8080 Trusted 0)" \
    "http://${PEER_A_HOST}/api/mesh/fleet/nodes" >/dev/null

  ASSIGNED=false
  for _i in $(seq 1 45); do
    GET="$(curl -fsS -H "X-Ashlar-Api-Key: ${API_KEY}" \
      "http://${PEER_A_HOST}/api/mesh/tasks/${PEND_ID}")"
    ST="$(echo "$GET" | python3 -c 'import json,sys; print(json.load(sys.stdin).get("status"))')"
    if [[ "$ST" == "1" || "$ST" == "Assigned" ]]; then
      ASSIGNED=true
      break
    fi
    sleep 2
  done
  if [[ "$ASSIGNED" != true ]]; then
    echo "Rebalancer did not assign pending task within 90s (last status=${ST})" >&2
    exit 1
  fi
  echo "Elastic rebalancer assigned stale pending task — OK"
fi

for pid in mesh-lab-elastic-low mesh-lab-elastic-high; do
  mesh_delete "http://${PEER_A_HOST}/api/mesh/fleet/nodes/${pid}"
done
mesh_post -d "$(mesh_lab_fleet_register_json mesh-lab-verify-peer http://peer-b:8080 Trusted)" \
  "http://${PEER_A_HOST}/api/mesh/fleet/nodes" >/dev/null

echo "== Mesh lab verify-elastic: OK =="
