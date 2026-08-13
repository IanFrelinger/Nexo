#!/usr/bin/env bash
# Mesh lab TLS: director reachable via HTTPS (Caddy reverse_proxy → peer-a).
#
#   ./scripts/mesh-lab-verify-tls.sh .env.mesh-lab
#
# Prerequisite: compose up with deploy/compose/docker-compose.mesh-lab-tls.override.yml

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

# shellcheck source=scripts/mesh-lab-fleet.sh
source "${ROOT}/scripts/mesh-lab-fleet.sh"
# shellcheck source=scripts/mesh-lab-tls-curl.sh
source "${ROOT}/scripts/mesh-lab-tls-curl.sh"

COMPOSE_ENV_FILE="${1:-${COMPOSE_ENV_FILE:-.env.mesh-lab}}"
TLS_HOST="${MESH_LAB_TLS_HOST:-127.0.0.1:18443}"
DIRECTOR_BASE="https://${TLS_HOST}"

if [[ ! -f "$COMPOSE_ENV_FILE" ]]; then
  echo "Missing env file: $COMPOSE_ENV_FILE" >&2
  exit 1
fi

if ! command -v python3 >/dev/null 2>&1; then
  echo "python3 is required." >&2
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
  echo "(Skipping TLS verify: no Nexo__Security__ApiKey in env file)"
  exit 0
fi

mesh_post_tls() {
  mesh_lab_tls_curl -X POST -H "Content-Type: application/json" -H "X-Nexo-Api-Key: ${API_KEY}" "$@"
}

echo "== Mesh lab TLS director (${DIRECTOR_BASE}, self-signed via mesh-lab-tls-certs.sh) =="

mesh_lab_tls_curl "${DIRECTOR_BASE}/health" | head -c 200
echo

mesh_lab_tls_curl "${DIRECTOR_BASE}/api/mesh/fleet/nodes" | head -c 200
echo

mesh_post_tls -d "$(mesh_lab_fleet_register_json mesh-lab-tls-peer "http://peer-b:8080" Trusted)" \
  "${DIRECTOR_BASE}/api/mesh/fleet/nodes" | head -c 300
echo

TASK_JSON="$(mesh_post_tls -d '{"name":"mesh-lab-tls-task","steps":1}' \
  "${DIRECTOR_BASE}/api/mesh/tasks")"
TASK_ID="$(echo "$TASK_JSON" | python3 -c 'import json,sys; print(json.load(sys.stdin)["taskId"])')"
SCHED_JSON="$(mesh_post_tls -d '{}' "${DIRECTOR_BASE}/api/mesh/tasks/${TASK_ID}/schedule")"
echo "$SCHED_JSON" | python3 -c '
import json, sys
t = json.load(sys.stdin)
if t.get("status") not in (1, "Assigned"):
    sys.stderr.write("Expected Assigned over HTTPS\n")
    sys.exit(1)
print("TLS mesh schedule → Assigned — OK")
'

CODE="$(mesh_lab_tls_curl_status -X POST \
  -H "Content-Type: application/json" \
  -d '{"name":"no-key"}' \
  "${DIRECTOR_BASE}/api/mesh/tasks")"
if [[ "$CODE" != "401" && "$CODE" != "403" ]]; then
  echo "Expected 401/403 mesh POST without API key over TLS, got ${CODE}" >&2
  exit 1
fi
echo "TLS mesh POST without API key rejected — OK"

echo "== Mesh lab verify-tls: OK =="
