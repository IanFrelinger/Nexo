#!/usr/bin/env bash
# Mesh director trust-tier placement (trusted-only policy on peer-a).
#
#   ./scripts/mesh-lab-verify-trust.sh .env.mesh-lab
#
# Requires peer-a with Nexo__Mesh__Placement__PeerTrustPolicy=trusted-only (compose default).

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

API_KEY="$(source_env_kv Nexo__Security__ApiKey)"
MESH_LAB_PEER_REGISTRATION_KEY="$(source_env_kv MESH_LAB_PEER_REGISTRATION_KEY)"
export MESH_LAB_PEER_REGISTRATION_KEY
if [[ -z "$API_KEY" ]]; then
  echo "(Skipping trust verify: no Nexo__Security__ApiKey in env file)"
  exit 0
fi

mesh_post() {
  curl -fsS -X POST -H "Content-Type: application/json" -H "X-Nexo-Api-Key: ${API_KEY}" "$@"
}

mesh_delete() {
  curl -fsS -X DELETE -H "X-Nexo-Api-Key: ${API_KEY}" "$@"
}

echo "== Mesh trust placement (director ${PEER_A_HOST}, trusted-only) =="

for peer_id in mesh-lab-verify-peer mesh-lab-trust-untrusted mesh-lab-trust-trusted; do
  curl -fsS -X DELETE -H "X-Nexo-Api-Key: ${API_KEY}" \
    "http://${PEER_A_HOST}/api/mesh/fleet/nodes/${peer_id}" >/dev/null 2>&1 || true
done

mesh_post -d "$(mesh_lab_fleet_register_json mesh-lab-trust-untrusted http://peer-b:8080 Untrusted)" \
  "http://${PEER_A_HOST}/api/mesh/fleet/nodes" >/dev/null
mesh_post -d "$(mesh_lab_fleet_register_json mesh-lab-trust-trusted http://peer-b:8080 Trusted)" \
  "http://${PEER_A_HOST}/api/mesh/fleet/nodes" >/dev/null

TASK_JSON="$(mesh_post -d '{"name":"mesh-lab-trust-pick","steps":1}' \
  "http://${PEER_A_HOST}/api/mesh/tasks")"
TASK_ID="$(echo "$TASK_JSON" | python3 -c 'import json,sys; print(json.load(sys.stdin)["taskId"])')"
SCHED_JSON="$(mesh_post -d '{}' "http://${PEER_A_HOST}/api/mesh/tasks/${TASK_ID}/schedule")"
echo "$SCHED_JSON" | python3 -c '
import json, sys
t = json.load(sys.stdin)
st = t.get("status")
if st not in (1, "Assigned"):
    sys.stderr.write(f"Expected Assigned, got {st!r}\n")
    sys.exit(1)
peer = t.get("assignedPeerId") or ""
if peer != "mesh-lab-trust-trusted":
    sys.stderr.write(f"Expected assignedPeerId mesh-lab-trust-trusted, got {peer!r}\n")
    sys.exit(1)
print("Trust placement picked trusted peer — OK")
'

mesh_delete "http://${PEER_A_HOST}/api/mesh/fleet/nodes/mesh-lab-trust-trusted" >/dev/null

BLOCK_JSON="$(mesh_post -d '{"name":"mesh-lab-trust-blocked","steps":1}' \
  "http://${PEER_A_HOST}/api/mesh/tasks")"
BLOCK_ID="$(echo "$BLOCK_JSON" | python3 -c 'import json,sys; print(json.load(sys.stdin)["taskId"])')"
BLOCK_CODE="$(curl -s -o /dev/null -w '%{http_code}' -X POST \
  -H "Content-Type: application/json" \
  -H "X-Nexo-Api-Key: ${API_KEY}" \
  -d '{}' \
  "http://${PEER_A_HOST}/api/mesh/tasks/${BLOCK_ID}/schedule")"
if [[ "$BLOCK_CODE" != "400" ]]; then
  echo "Expected HTTP 400 scheduling with only untrusted peer, got ${BLOCK_CODE}" >&2
  exit 1
fi
GET_JSON="$(curl -fsS -H "X-Nexo-Api-Key: ${API_KEY}" "http://${PEER_A_HOST}/api/mesh/tasks/${BLOCK_ID}")"
echo "$GET_JSON" | python3 -c '
import json, sys
t = json.load(sys.stdin)
reason = t.get("placementReason") or ""
if "trust_policy" not in reason:
    sys.stderr.write(f"Expected placement.trust_policy_blocked on GET, got {reason!r}\n")
    sys.exit(1)
print("Untrusted-only fleet blocked by trust policy — OK")
'

for peer_id in mesh-lab-trust-untrusted mesh-lab-trust-trusted; do
  curl -fsS -X DELETE -H "X-Nexo-Api-Key: ${API_KEY}" \
    "http://${PEER_A_HOST}/api/mesh/fleet/nodes/${peer_id}" >/dev/null 2>&1 || true
done
mesh_post -d "$(mesh_lab_fleet_register_json mesh-lab-verify-peer http://peer-b:8080 Trusted)" \
  "http://${PEER_A_HOST}/api/mesh/fleet/nodes" >/dev/null

echo "== Mesh lab verify-trust: OK =="
