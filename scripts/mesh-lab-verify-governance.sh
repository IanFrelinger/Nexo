#!/usr/bin/env bash
# Mesh director fleet governance: peer registration keys, admit/revoke placement.
#
#   ./scripts/mesh-lab-verify-governance.sh .env.mesh-lab
#
# Requires peer-a with Ashlar__Mesh__Fleet__RequirePeerRegistrationKey=true (compose default).

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

# shellcheck source=scripts/mesh-lab-fleet.sh
source "${ROOT}/scripts/mesh-lab-fleet.sh"

COMPOSE_ENV_FILE="${1:-${COMPOSE_ENV_FILE:-.env.mesh-lab}}"
PEER_A_HOST="${MESH_LAB_PEER_A_HOST:-127.0.0.1:18081}"
GOV_PEER_ID="${MESH_LAB_GOV_PEER_ID:-mesh-lab-gov-peer}"

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
ROTATED_KEY="$(source_env_kv MESH_LAB_PEER_REGISTRATION_KEY_ROTATED)"

if [[ -z "$API_KEY" ]]; then
  echo "(Skipping governance verify: no Ashlar__Security__ApiKey in env file)"
  exit 0
fi

if [[ -z "$MESH_LAB_PEER_REGISTRATION_KEY" ]]; then
  echo "(Skipping governance verify: set MESH_LAB_PEER_REGISTRATION_KEY in env file)"
  exit 0
fi

[[ -n "$ROTATED_KEY" ]] || ROTATED_KEY="${MESH_LAB_PEER_REGISTRATION_KEY}-rotated-v2"

mesh_post() {
  curl -fsS -X POST -H "Content-Type: application/json" -H "X-Ashlar-Api-Key: ${API_KEY}" "$@"
}

mesh_post_code() {
  curl -s -o /dev/null -w '%{http_code}' -X POST -H "Content-Type: application/json" -H "X-Ashlar-Api-Key: ${API_KEY}" "$@"
}

mesh_delete() {
  curl -fsS -X DELETE -H "X-Ashlar-Api-Key: ${API_KEY}" "$@"
}

echo "== Mesh fleet governance (director ${PEER_A_HOST}) =="

mesh_delete "http://${PEER_A_HOST}/api/mesh/fleet/nodes/${GOV_PEER_ID}" >/dev/null 2>&1 || true

BAD_CODE="$(mesh_post_code -d '{"peerId":"mesh-lab-gov-missing-key","apiBaseUrl":"http://peer-b:8080"}' \
  "http://${PEER_A_HOST}/api/mesh/fleet/nodes")"
if [[ "$BAD_CODE" != "400" ]]; then
  echo "Expected HTTP 400 registering without peerRegistrationKey, got ${BAD_CODE}" >&2
  exit 1
fi
echo "Registration without peer key rejected — OK"

SAME_KEY_CODE="$(mesh_post_code -d "$(python3 -c 'import json,sys; print(json.dumps({"peerId":"mesh-lab-gov-same-key","apiBaseUrl":"http://peer-b:8080","peerRegistrationKey":sys.argv[1]}))' "$API_KEY")" \
  "http://${PEER_A_HOST}/api/mesh/fleet/nodes")"
if [[ "$SAME_KEY_CODE" != "400" ]]; then
  echo "Expected HTTP 400 when peerRegistrationKey equals director API key, got ${SAME_KEY_CODE}" >&2
  exit 1
fi
echo "Registration with operator API key as peer key rejected — OK"

REG_JSON="$(mesh_post -d "$(mesh_lab_fleet_register_json "$GOV_PEER_ID" "http://peer-b:8080" Trusted)" \
  "http://${PEER_A_HOST}/api/mesh/fleet/nodes")"
FP1="$(echo "$REG_JSON" | python3 -c 'import json,sys; print(json.load(sys.stdin).get("registrationKeyFingerprint") or "")')"
[[ -n "$FP1" ]] || { echo "Expected registrationKeyFingerprint on register response" >&2; exit 1; }
echo "Registered ${GOV_PEER_ID} fingerprint=${FP1} — OK"

MESH_LAB_PEER_REGISTRATION_KEY="$ROTATED_KEY" \
  REG2_JSON="$(mesh_post -d "$(mesh_lab_fleet_register_json "$GOV_PEER_ID" "http://peer-b:8080" Trusted)" \
  "http://${PEER_A_HOST}/api/mesh/fleet/nodes")"
FP2="$(echo "$REG2_JSON" | python3 -c 'import json,sys; print(json.load(sys.stdin).get("registrationKeyFingerprint") or "")')"
if [[ "$FP2" == "$FP1" ]]; then
  echo "Expected fingerprint to change after credential rotation, still ${FP1}" >&2
  exit 1
fi
echo "Credential rotation updated fingerprint — OK"

mesh_delete "http://${PEER_A_HOST}/api/mesh/fleet/nodes/mesh-lab-verify-peer" >/dev/null 2>&1 || true

PRE_JSON="$(mesh_post -d '{"name":"mesh-lab-gov-pre-revoke","steps":1}' \
  "http://${PEER_A_HOST}/api/mesh/tasks")"
PRE_ID="$(echo "$PRE_JSON" | python3 -c 'import json,sys; print(json.load(sys.stdin)["taskId"])')"
PRE_SCHED="$(mesh_post -d '{}' "http://${PEER_A_HOST}/api/mesh/tasks/${PRE_ID}/schedule")"
echo "$PRE_SCHED" | python3 -c '
import json, sys
t = json.load(sys.stdin)
if t.get("status") not in (1, "Assigned"):
    sys.stderr.write("Expected Assigned pre-revoke\n")
    sys.exit(1)
peer = t.get("assignedPeerId") or ""
if peer != "mesh-lab-gov-peer":
    sys.stderr.write("Expected gov peer placement, got %r\n" % (peer,))
    sys.exit(1)
print("Pre-revoke schedule on gov peer — OK")
'

mesh_post "http://${PEER_A_HOST}/api/mesh/fleet/nodes/${GOV_PEER_ID}/revoke" >/dev/null

REV_TASK_JSON="$(mesh_post -d '{"name":"mesh-lab-gov-revoked","steps":1}' \
  "http://${PEER_A_HOST}/api/mesh/tasks")"
REV_ID="$(echo "$REV_TASK_JSON" | python3 -c 'import json,sys; print(json.load(sys.stdin)["taskId"])')"
REV_CODE="$(curl -s -o /dev/null -w '%{http_code}' -X POST \
  -H "Content-Type: application/json" \
  -H "X-Ashlar-Api-Key: ${API_KEY}" \
  -d '{}' \
  "http://${PEER_A_HOST}/api/mesh/tasks/${REV_ID}/schedule")"
if [[ "$REV_CODE" != "400" ]]; then
  echo "Expected HTTP 400 scheduling after revoke, got ${REV_CODE}" >&2
  exit 1
fi
REV_GET="$(curl -fsS -H "X-Ashlar-Api-Key: ${API_KEY}" "http://${PEER_A_HOST}/api/mesh/tasks/${REV_ID}")"
echo "$REV_GET" | python3 -c '
import json, sys
t = json.load(sys.stdin)
reason = t.get("placementReason") or ""
if "not_admitted" not in reason:
    sys.stderr.write(f"Expected placement.peer_not_admitted, got {reason!r}\n")
    sys.exit(1)
print("Revoked peer blocked from placement — OK")
'

mesh_post "http://${PEER_A_HOST}/api/mesh/fleet/nodes/${GOV_PEER_ID}/admit" >/dev/null

ADM_TASK_JSON="$(mesh_post -d '{"name":"mesh-lab-gov-admitted","steps":1}' \
  "http://${PEER_A_HOST}/api/mesh/tasks")"
ADM_ID="$(echo "$ADM_TASK_JSON" | python3 -c 'import json,sys; print(json.load(sys.stdin)["taskId"])')"
ADM_JSON="$(mesh_post -d '{}' "http://${PEER_A_HOST}/api/mesh/tasks/${ADM_ID}/schedule")"
echo "$ADM_JSON" | python3 -c '
import json, sys
t = json.load(sys.stdin)
if t.get("status") not in (1, "Assigned"):
    sys.stderr.write("Expected Assigned after admit, got %r\n" % (t.get("status"),))
    sys.exit(1)
peer = t.get("assignedPeerId") or ""
if peer != "mesh-lab-gov-peer":
    sys.stderr.write("Expected gov peer after admit, got %r\n" % (peer,))
    sys.exit(1)
print("Admitted peer eligible for placement — OK")
'

mesh_delete "http://${PEER_A_HOST}/api/mesh/fleet/nodes/${GOV_PEER_ID}" >/dev/null 2>&1 || true
mesh_delete "http://${PEER_A_HOST}/api/mesh/fleet/nodes/mesh-lab-gov-same-key" >/dev/null 2>&1 || true

mesh_post -d "$(mesh_lab_fleet_register_json mesh-lab-verify-peer http://peer-b:8080 Trusted)" \
  "http://${PEER_A_HOST}/api/mesh/fleet/nodes" >/dev/null

echo "== Mesh lab verify-governance: OK =="
