#!/usr/bin/env bash
# Mesh director governance via commercial mesh director CLI CLI (Phase 7 + Product 5.3).
#
#   ./scripts/mesh-lab-verify-director-cli.sh .env.mesh-lab
#
# Prerequisite: mesh lab up on peer-a; dotnet SDK on host.

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

# shellcheck source=scripts/mesh-lab-fleet.sh
source "${ROOT}/scripts/mesh-lab-fleet.sh"

COMPOSE_ENV_FILE="${1:-${COMPOSE_ENV_FILE:-.env.mesh-lab}}"
PEER_A_HOST="${MESH_LAB_PEER_A_HOST:-127.0.0.1:18081}"
CLI_PEER_ID="${MESH_LAB_CLI_GOV_PEER_ID:-mesh-lab-cli-gov-peer}"
MESH_DIRECTOR_PROJECT="${MESH_LAB_MESH_DIRECTOR_PROJECT:-commercial/src/Ashlar.Commercial.MeshDirector/Ashlar.Commercial.MeshDirector.csproj}"

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

if [[ -z "$API_KEY" || -z "$MESH_LAB_PEER_REGISTRATION_KEY" ]]; then
  echo "(Skipping director CLI verify: need ApiKey and MESH_LAB_PEER_REGISTRATION_KEY in env file)"
  exit 0
fi

export ASHLAR_MESH_DIRECTOR_BASE_URL="http://${PEER_A_HOST}"
export ASHLAR_MESH_API_KEY="${API_KEY}"
export ASHLAR_MESH_PEER_REGISTRATION_KEY="${MESH_LAB_PEER_REGISTRATION_KEY}"

director() {
  dotnet run --project "$MESH_DIRECTOR_PROJECT" -- director "$@"
}

mesh_post() {
  curl -fsS -X POST -H "Content-Type: application/json" -H "X-Ashlar-Api-Key: ${API_KEY}" "$@"
}

mesh_delete() {
  curl -fsS -X DELETE -H "X-Ashlar-Api-Key: ${API_KEY}" "$@"
}

echo "== Mesh director CLI governance (${PEER_A_HOST}) =="

mesh_delete "http://${PEER_A_HOST}/api/mesh/fleet/nodes/${CLI_PEER_ID}" >/dev/null 2>&1 || true
mesh_delete "http://${PEER_A_HOST}/api/mesh/fleet/nodes/mesh-lab-verify-peer" >/dev/null 2>&1 || true

director register "$CLI_PEER_ID" \
  --api-base-url "http://peer-b:8080" \
  --trust-tier Trusted \
  --json >/dev/null

PRE_JSON="$(mesh_post -d '{"name":"mesh-lab-cli-pre-revoke","steps":1}' \
  "http://${PEER_A_HOST}/api/mesh/tasks")"
PRE_ID="$(echo "$PRE_JSON" | python3 -c 'import json,sys; print(json.load(sys.stdin)["taskId"])')"
PRE_SCHED="$(mesh_post -d '{}' "http://${PEER_A_HOST}/api/mesh/tasks/${PRE_ID}/schedule")"
echo "$PRE_SCHED" | python3 -c '
import json, sys
t = json.load(sys.stdin)
if t.get("status") not in (1, "Assigned"):
    sys.stderr.write("Expected Assigned\n")
    sys.exit(1)
if (t.get("assignedPeerId") or "") != sys.argv[1]:
    sys.stderr.write("Expected placement on CLI gov peer\n")
    sys.exit(1)
print("CLI register + schedule — OK")
' "$CLI_PEER_ID"

director revoke "$CLI_PEER_ID" --json >/dev/null

REV_JSON="$(mesh_post -d '{"name":"mesh-lab-cli-revoked","steps":1}' \
  "http://${PEER_A_HOST}/api/mesh/tasks")"
REV_ID="$(echo "$REV_JSON" | python3 -c 'import json,sys; print(json.load(sys.stdin)["taskId"])')"
REV_CODE="$(curl -s -o /dev/null -w '%{http_code}' -X POST \
  -H "Content-Type: application/json" \
  -H "X-Ashlar-Api-Key: ${API_KEY}" \
  -d '{}' \
  "http://${PEER_A_HOST}/api/mesh/tasks/${REV_ID}/schedule")"
if [[ "$REV_CODE" != "400" ]]; then
  echo "Expected HTTP 400 after director revoke, got ${REV_CODE}" >&2
  exit 1
fi
echo "CLI revoke blocked placement — OK"

director admit "$CLI_PEER_ID" --json >/dev/null

ADM_JSON="$(mesh_post -d '{"name":"mesh-lab-cli-admitted","steps":1}' \
  "http://${PEER_A_HOST}/api/mesh/tasks")"
ADM_ID="$(echo "$ADM_JSON" | python3 -c 'import json,sys; print(json.load(sys.stdin)["taskId"])')"
ADM_SCHED="$(mesh_post -d '{}' "http://${PEER_A_HOST}/api/mesh/tasks/${ADM_ID}/schedule")"
echo "$ADM_SCHED" | python3 -c '
import json, sys
t = json.load(sys.stdin)
if t.get("status") not in (1, "Assigned"):
    sys.stderr.write("Expected Assigned after admit\n")
    sys.exit(1)
if (t.get("assignedPeerId") or "") != sys.argv[1]:
    sys.stderr.write("Expected placement on CLI gov peer after admit\n")
    sys.exit(1)
print("CLI admit restored placement — OK")
' "$CLI_PEER_ID"

mesh_delete "http://${PEER_A_HOST}/api/mesh/fleet/nodes/${CLI_PEER_ID}" >/dev/null 2>&1 || true
mesh_post -d "$(mesh_lab_fleet_register_json mesh-lab-verify-peer http://peer-b:8080 Trusted)" \
  "http://${PEER_A_HOST}/api/mesh/fleet/nodes" >/dev/null

echo "== Mesh lab verify-director-cli: OK =="
