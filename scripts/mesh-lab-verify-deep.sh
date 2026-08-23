#!/usr/bin/env bash
# Deep mesh lab checks: multi-step task, migrate-for-checkpoint, reschedule, lease extend.
# Prerequisite: lab running (peers up) with same env as mesh-lab-verify.sh.
#
#   ./scripts/mesh-lab-verify-deep.sh .env.mesh-lab
#   MESH_LAB_VERIFY_DEEP=1 ./scripts/run-mesh-lab-e2e.sh --workers   # if e2e invokes deep (optional)

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

# shellcheck source=scripts/mesh-lab-fleet.sh
source "${ROOT}/scripts/mesh-lab-fleet.sh"

if ! command -v python3 >/dev/null 2>&1; then
  echo "python3 is required." >&2
  exit 1
fi

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
  echo "Ashlar__Security__ApiKey required in $COMPOSE_ENV_FILE" >&2
  exit 1
fi

mesh_curl() {
  curl -fsS -H "X-Ashlar-Api-Key: ${API_KEY}" -H "Content-Type: application/json" "$@"
}

status_ok() {
  local json=$1
  shift
  echo "$json" | python3 -c '
import json, sys
s = json.load(sys.stdin).get("status")
want = set(sys.argv[1:])
sys.exit(0 if str(s) in want or s in want else 1)
' "$@"
}

echo "== Mesh lab verify-deep: multi-step + checkpoint migrate + reschedule =="

mesh_curl -X POST \
  -d "$(mesh_lab_fleet_register_json mesh-lab-verify-peer http://peer-b:8080 Trusted)" \
  "http://${PEER_A_HOST}/api/mesh/fleet/nodes" >/dev/null

TASK_JSON="$(mesh_curl -X POST \
  -d '{"name":"mesh-lab-verify-deep","steps":2,"requiredBrickIds":[]}' \
  "http://${PEER_A_HOST}/api/mesh/tasks")"
TASK_ID="$(echo "$TASK_JSON" | python3 -c 'import json,sys; print(json.load(sys.stdin)["taskId"])')"

SCHED_JSON="$(mesh_curl -X POST -d '{}' "http://${PEER_A_HOST}/api/mesh/tasks/${TASK_ID}/schedule")"
status_ok "$SCHED_JSON" 1 Assigned || {
  echo "First schedule did not reach Assigned." >&2
  exit 1
}
LEASE_TOKEN="$(echo "$SCHED_JSON" | python3 -c 'import json,sys; print(json.load(sys.stdin).get("leaseToken") or "")')"
[[ -n "$LEASE_TOKEN" ]] || { echo "Missing leaseToken after schedule." >&2; exit 1; }

mesh_curl -X PATCH \
  -d "{\"status\":2,\"leaseToken\":\"${LEASE_TOKEN}\",\"reason\":\"mesh-lab-deep-running\"}" \
  "http://${PEER_A_HOST}/api/mesh/tasks/${TASK_ID}/status" >/dev/null

EXT_JSON="$(mesh_curl -X POST \
  -d "{\"leaseToken\":\"${LEASE_TOKEN}\",\"extendSeconds\":120}" \
  "http://${PEER_A_HOST}/api/mesh/tasks/${TASK_ID}/lease/extend")"
echo "$EXT_JSON" | python3 -c '
import json, sys
t = json.load(sys.stdin)
if not t.get("leaseExpiresUtc"):
    sys.stderr.write("lease extend did not set leaseExpiresUtc\n")
    sys.exit(1)
print("Lease extended — OK")
' || exit 1

MIG_JSON="$(mesh_curl -X POST \
  -d "{\"leaseToken\":\"${LEASE_TOKEN}\",\"checkpointHandle\":\"mesh-lab-ckpt-1\"}" \
  "http://${PEER_A_HOST}/api/mesh/tasks/${TASK_ID}/migrate-for-checkpoint")"
echo "$MIG_JSON" | head -c 400
echo
status_ok "$MIG_JSON" 0 Pending || {
  echo "migrate-for-checkpoint did not yield Pending." >&2
  exit 1
}
echo "$MIG_JSON" | python3 -c '
import json, sys
t = json.load(sys.stdin)
if (t.get("checkpointHandle") or "") != "mesh-lab-ckpt-1":
    sys.stderr.write("checkpointHandle not set\n")
    sys.exit(1)
if t.get("assignedPeerId"):
    sys.stderr.write("expected assignment cleared after migrate\n")
    sys.exit(1)
print("Checkpoint migrate OK")
' || exit 1

SCHED2_JSON="$(mesh_curl -X POST -d '{}' "http://${PEER_A_HOST}/api/mesh/tasks/${TASK_ID}/schedule")"
status_ok "$SCHED2_JSON" 1 Assigned || {
  echo "Second schedule after migrate did not reach Assigned." >&2
  exit 1
}
LEASE2="$(echo "$SCHED2_JSON" | python3 -c 'import json,sys; print(json.load(sys.stdin).get("leaseToken") or "")')"
ATTEMPT="$(echo "$SCHED2_JSON" | python3 -c 'import json,sys; print(json.load(sys.stdin).get("attemptCount",0))')"
if [[ "${ATTEMPT:-0}" -lt 2 ]]; then
  echo "Expected attemptCount >= 2 after reschedule, got ${ATTEMPT}" >&2
  exit 1
fi
echo "Reschedule after migrate OK — attemptCount=${ATTEMPT}"

mesh_curl -X PATCH \
  -d "{\"status\":2,\"leaseToken\":\"${LEASE2}\",\"reason\":\"mesh-lab-deep-running-2\"}" \
  "http://${PEER_A_HOST}/api/mesh/tasks/${TASK_ID}/status" >/dev/null

mesh_curl -X PATCH \
  -d "{\"status\":3,\"leaseToken\":\"${LEASE2}\",\"resultSummary\":\"mesh-lab-deep-complete\"}" \
  "http://${PEER_A_HOST}/api/mesh/tasks/${TASK_ID}/status" >/dev/null

FINAL="$(mesh_curl "http://${PEER_A_HOST}/api/mesh/tasks/${TASK_ID}")"
status_ok "$FINAL" 3 Succeeded || {
  echo "Final task status not Succeeded." >&2
  exit 1
}

echo "== Mesh lab verify-deep: OK =="
