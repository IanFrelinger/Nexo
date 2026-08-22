#!/usr/bin/env bash
# Post-stress director checks: placement still works; LiteDB survives restart after worker ramp.
#
#   ./scripts/mesh-lab-verify-post-stress.sh .env.mesh-lab
#
# Prerequisite: lab up, stress ramp completed (workers profile).

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

# shellcheck source=scripts/mesh-lab-fleet.sh
source "${ROOT}/scripts/mesh-lab-fleet.sh"

COMPOSE_ENV_FILE="${1:-${COMPOSE_ENV_FILE:-.env.mesh-lab}}"
PEER_A_HOST="${MESH_LAB_PEER_A_HOST:-127.0.0.1:18081}"
POST_STRESS_PEER="${MESH_LAB_POST_STRESS_PEER_ID:-mesh-lab-post-stress-peer}"

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
  echo "(Skipping post-stress verify: no Ashlar__Security__ApiKey in env file)"
  exit 0
fi

mesh_post() {
  curl -fsS -X POST -H "Content-Type: application/json" -H "X-Ashlar-Api-Key: ${API_KEY}" "$@"
}

mesh_delete() {
  curl -fsS -X DELETE -H "X-Ashlar-Api-Key: ${API_KEY}" "$@"
}

echo "== Mesh lab post-stress: director placement (${PEER_A_HOST}) =="

mesh_delete "http://${PEER_A_HOST}/api/mesh/fleet/nodes/${POST_STRESS_PEER}" >/dev/null 2>&1 || true
mesh_delete "http://${PEER_A_HOST}/api/mesh/fleet/nodes/mesh-lab-verify-peer" >/dev/null 2>&1 || true

mesh_post -d "$(mesh_lab_fleet_register_json "$POST_STRESS_PEER" http://peer-b:8080 Trusted)" \
  "http://${PEER_A_HOST}/api/mesh/fleet/nodes" >/dev/null

TASK_JSON="$(mesh_post -d '{"name":"mesh-lab-post-stress-task","steps":1}' \
  "http://${PEER_A_HOST}/api/mesh/tasks")"
TASK_ID="$(echo "$TASK_JSON" | python3 -c 'import json,sys; print(json.load(sys.stdin)["taskId"])')"
SCHED_JSON="$(mesh_post -d '{}' "http://${PEER_A_HOST}/api/mesh/tasks/${TASK_ID}/schedule")"
echo "$SCHED_JSON" | python3 -c '
import json, sys
t = json.load(sys.stdin)
if t.get("status") not in (1, "Assigned"):
    sys.stderr.write("Expected Assigned after stress\n")
    sys.exit(1)
if (t.get("assignedPeerId") or "") != sys.argv[1]:
    sys.stderr.write("Expected post-stress peer placement\n")
    sys.exit(1)
print("Post-stress placement OK")
' "$POST_STRESS_PEER"

PERSIST_PROVIDER="$(source_env_kv Ashlar__Mesh__Persistence__Provider)"
[[ -n "$PERSIST_PROVIDER" ]] || PERSIST_PROVIDER="LiteDb"
if [[ "$(echo "${PERSIST_PROVIDER}" | tr '[:upper:]' '[:lower:]')" == "litedb" ]]; then
  echo "== Mesh lab post-stress: persistence re-check =="
  export MESH_LAB_PERSIST_PEER_ID="${MESH_LAB_POST_STRESS_PERSIST_PEER_ID:-mesh-lab-post-stress-persist}"
  "${ROOT}/scripts/mesh-lab-verify-persistence.sh" "$COMPOSE_ENV_FILE"
fi

mesh_post -d "$(mesh_lab_fleet_register_json mesh-lab-verify-peer http://peer-b:8080 Trusted)" \
  "http://${PEER_A_HOST}/api/mesh/fleet/nodes" >/dev/null 2>&1 || true

echo "== Mesh lab verify-post-stress: OK =="
