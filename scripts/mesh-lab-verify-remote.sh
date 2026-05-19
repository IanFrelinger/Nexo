#!/usr/bin/env bash
# Remote director verify: run mesh checks against NEXO_MESH_DIRECTOR_BASE_URL (Tailscale / TLS host).
#
#   export NEXO_MESH_DIRECTOR_BASE_URL=https://100.x.y.z:8080
#   export NEXO_MESH_API_KEY=...
#   export MESH_LAB_PEER_REGISTRATION_KEY=...   # distinct from API key
#   export MESH_LAB_REMOTE_WORKER_URL=http://100.x.w.z:8080
#   ./scripts/mesh-lab-verify-remote.sh
#
# No Docker required on this machine if the remote stack is already up.

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

# shellcheck source=scripts/mesh-lab-fleet.sh
source "${ROOT}/scripts/mesh-lab-fleet.sh"

DIRECTOR_BASE="${NEXO_MESH_DIRECTOR_BASE_URL:-}"
API_KEY="${NEXO_MESH_API_KEY:-}"
WORKER_BASE="${MESH_LAB_REMOTE_WORKER_URL:-}"
export MESH_LAB_PEER_REGISTRATION_KEY="${MESH_LAB_PEER_REGISTRATION_KEY:-}"
CURL_EXTRA=()
if [[ "${NEXO_MESH_TLS_INSECURE:-}" == "1" || "${NEXO_MESH_TLS_INSECURE:-}" == "true" ]]; then
  CURL_EXTRA=(-k)
fi

if [[ -z "$DIRECTOR_BASE" || -z "$API_KEY" ]]; then
  echo "Set NEXO_MESH_DIRECTOR_BASE_URL and NEXO_MESH_API_KEY" >&2
  exit 1
fi
if [[ -z "$WORKER_BASE" ]]; then
  echo "Set MESH_LAB_REMOTE_WORKER_URL (worker apiBaseUrl for fleet register)" >&2
  exit 1
fi
if [[ -z "$MESH_LAB_PEER_REGISTRATION_KEY" ]]; then
  echo "Set MESH_LAB_PEER_REGISTRATION_KEY" >&2
  exit 1
fi

DIRECTOR_BASE="${DIRECTOR_BASE%/}"

mesh_curl() {
  curl -fsS "${CURL_EXTRA[@]}" -H "X-Nexo-Api-Key: ${API_KEY}" "$@"
}

mesh_post() {
  curl -fsS "${CURL_EXTRA[@]}" -X POST -H "Content-Type: application/json" \
    -H "X-Nexo-Api-Key: ${API_KEY}" "$@"
}

echo "== Mesh remote verify (${DIRECTOR_BASE}) =="

mesh_curl "${DIRECTOR_BASE}/health" | head -c 200
echo

mesh_post -d "$(mesh_lab_fleet_register_json mesh-lab-remote-peer "${WORKER_BASE}" Trusted)" \
  "${DIRECTOR_BASE}/api/mesh/fleet/nodes" >/dev/null

TASK_JSON="$(mesh_post -d '{"name":"mesh-lab-remote-task","steps":1}' \
  "${DIRECTOR_BASE}/api/mesh/tasks")"
TASK_ID="$(echo "$TASK_JSON" | python3 -c 'import json,sys; print(json.load(sys.stdin)["taskId"])')"
SCHED_JSON="$(mesh_post -d '{}' "${DIRECTOR_BASE}/api/mesh/tasks/${TASK_ID}/schedule")"
echo "$SCHED_JSON" | python3 -c '
import json, sys
if json.load(sys.stdin).get("status") not in (1, "Assigned"):
    sys.exit("Expected Assigned on remote director")
print("Remote schedule → Assigned — OK")
'

echo "== Mesh lab verify-remote: OK =="
