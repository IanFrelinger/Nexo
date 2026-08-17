#!/usr/bin/env bash
# Mesh lab negative networking: unreachable peers, DNS failure, drained fleet, peer-b outage, director restart + lease.
#
#   ./scripts/mesh-lab-verify-network-negative.sh .env.mesh-lab
#
# Prerequisite: mesh lab running (peer-a + peer-b minimum).

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

# shellcheck source=scripts/mesh-lab-fleet.sh
source "${ROOT}/scripts/mesh-lab-fleet.sh"
# shellcheck source=scripts/mesh-lab-net.sh
source "${ROOT}/scripts/mesh-lab-net.sh"

COMPOSE_ENV_FILE="${1:-${COMPOSE_ENV_FILE:-.env.mesh-lab}}"
COMPOSE_FILE="${COMPOSE_FILE:-deploy/compose/docker-compose.mesh-lab.yml}"
PEER_A_HOST="${MESH_LAB_PEER_A_HOST:-127.0.0.1:18081}"

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
  echo "(Skipping network-negative verify: no Nexo__Security__ApiKey in env file)"
  exit 0
fi

mesh_post() {
  curl -fsS -X POST -H "Content-Type: application/json" -H "X-Nexo-Api-Key: ${API_KEY}" "$@"
}

mesh_post_code() {
  curl -s -o /dev/null -w '%{http_code}' -X POST -H "Content-Type: application/json" -H "X-Nexo-Api-Key: ${API_KEY}" "$@"
}

mesh_delete() {
  curl -fsS -X DELETE -H "X-Nexo-Api-Key: ${API_KEY}" "$@"
}

cleanup_test_peers() {
  local peer_id
  for peer_id in mesh-lab-net-blackhole mesh-lab-net-dns-ghost mesh-lab-net-drained mesh-lab-net-live mesh-lab-net-restart; do
    mesh_delete "http://${PEER_A_HOST}/api/mesh/fleet/nodes/${peer_id}" >/dev/null 2>&1 || true
  done
}

mesh_patch() {
  curl -fsS -X PATCH -H "Content-Type: application/json" -H "X-Nexo-Api-Key: ${API_KEY}" "$@"
}

restore_verify_peer() {
  mesh_post -d "$(mesh_lab_fleet_register_json mesh-lab-verify-peer http://peer-b:8080 Trusted)" \
    "http://${PEER_A_HOST}/api/mesh/fleet/nodes" >/dev/null 2>&1 || true
}

echo "== Mesh lab network-negative (${PEER_A_HOST}) =="

cleanup_test_peers

# --- 1) Blackhole assigned base URL: placement OK, HTTP to worker fails ---
for peer_id in mesh-lab-verify-peer mesh-lab-net-blackhole; do
  mesh_delete "http://${PEER_A_HOST}/api/mesh/fleet/nodes/${peer_id}" >/dev/null 2>&1 || true
done

BLACKHOLE_BASE="http://10.255.255.254:59999"
mesh_post -d "$(mesh_lab_fleet_register_json mesh-lab-net-blackhole "${BLACKHOLE_BASE}" Trusted)" \
  "http://${PEER_A_HOST}/api/mesh/fleet/nodes" >/dev/null

TASK_JSON="$(mesh_post -d '{"name":"mesh-lab-net-blackhole","steps":1}' \
  "http://${PEER_A_HOST}/api/mesh/tasks")"
TASK_ID="$(echo "$TASK_JSON" | python3 -c 'import json,sys; print(json.load(sys.stdin)["taskId"])')"
SCHED_JSON="$(mesh_post -d '{}' "http://${PEER_A_HOST}/api/mesh/tasks/${TASK_ID}/schedule")"
echo "$SCHED_JSON" | python3 -c '
import json, sys
t = json.load(sys.stdin)
if t.get("status") not in (1, "Assigned"):
    sys.stderr.write("Expected Assigned on blackhole peer\n")
    sys.exit(1)
base = (t.get("assignedApiBaseUrl") or "").rstrip("/")
if not base.startswith("http://10.255.255.254"):
    sys.stderr.write("Unexpected assigned base %r\n" % (base,))
    sys.exit(1)
print("Blackhole peer placement OK (director does not preflight HTTP)")
'

mesh_lab_curl_on_net_must_fail "${BLACKHOLE_BASE}/health"
echo "Blackhole worker HTTP unreachable from bridge — OK"

# --- 2) DNS ghost host: placement OK, resolve fails on bridge ---
mesh_delete "http://${PEER_A_HOST}/api/mesh/fleet/nodes/mesh-lab-net-blackhole" >/dev/null 2>&1 || true
mesh_delete "http://${PEER_A_HOST}/api/mesh/fleet/nodes/mesh-lab-net-dns-ghost" >/dev/null 2>&1 || true

DNS_BASE="http://mesh-lab-ghost.invalid:8080"
mesh_post -d "$(mesh_lab_fleet_register_json mesh-lab-net-dns-ghost "${DNS_BASE}" Trusted)" \
  "http://${PEER_A_HOST}/api/mesh/fleet/nodes" >/dev/null

DNS_TASK="$(mesh_post -d '{"name":"mesh-lab-net-dns","steps":1}' \
  "http://${PEER_A_HOST}/api/mesh/tasks")"
DNS_ID="$(echo "$DNS_TASK" | python3 -c 'import json,sys; print(json.load(sys.stdin)["taskId"])')"
mesh_post -d '{}' "http://${PEER_A_HOST}/api/mesh/tasks/${DNS_ID}/schedule" >/dev/null
mesh_lab_curl_on_net_must_fail "${DNS_BASE}/health"
echo "DNS ghost worker HTTP unreachable from bridge — OK"

# --- 3) Drained-only fleet blocks placement ---
for peer_id in mesh-lab-net-dns-ghost mesh-lab-net-drained mesh-lab-verify-peer; do
  mesh_delete "http://${PEER_A_HOST}/api/mesh/fleet/nodes/${peer_id}" >/dev/null 2>&1 || true
done

mesh_post -d "$(python3 -c '
import json, os
key = (os.environ.get("MESH_LAB_PEER_REGISTRATION_KEY") or "").strip()
body = {"peerId": "mesh-lab-net-drained", "apiBaseUrl": "http://peer-b:8080", "trustTier": "Trusted", "drained": True}
if key:
    body["peerRegistrationKey"] = key
print(json.dumps(body))
')" "http://${PEER_A_HOST}/api/mesh/fleet/nodes" >/dev/null

DRAIN_TASK="$(mesh_post -d '{"name":"mesh-lab-net-drained","steps":1}' \
  "http://${PEER_A_HOST}/api/mesh/tasks")"
DRAIN_ID="$(echo "$DRAIN_TASK" | python3 -c 'import json,sys; print(json.load(sys.stdin)["taskId"])')"
DRAIN_CODE="$(mesh_post_code -d '{}' "http://${PEER_A_HOST}/api/mesh/tasks/${DRAIN_ID}/schedule")"
if [[ "$DRAIN_CODE" != "400" ]]; then
  echo "Expected HTTP 400 scheduling with only drained peer, got ${DRAIN_CODE}" >&2
  exit 1
fi
DRAIN_GET="$(curl -fsS -H "X-Nexo-Api-Key: ${API_KEY}" "http://${PEER_A_HOST}/api/mesh/tasks/${DRAIN_ID}")"
echo "$DRAIN_GET" | python3 -c '
import json, sys
reason = json.load(sys.stdin).get("placementReason") or ""
if "eligible" not in reason and "drained" not in reason.lower():
    sys.stderr.write("Expected placement blocked for drained-only fleet, got %r\n" % (reason,))
    sys.exit(1)
print("Drained-only fleet blocked placement — OK")
'

# --- 4) peer-b stopped: bridge cannot reach assigned worker ---
mesh_delete "http://${PEER_A_HOST}/api/mesh/fleet/nodes/mesh-lab-net-drained" >/dev/null 2>&1 || true
mesh_delete "http://${PEER_A_HOST}/api/mesh/fleet/nodes/mesh-lab-net-live" >/dev/null 2>&1 || true

mesh_post -d "$(mesh_lab_fleet_register_json mesh-lab-net-live http://peer-b:8080 Trusted)" \
  "http://${PEER_A_HOST}/api/mesh/fleet/nodes" >/dev/null

LIVE_TASK="$(mesh_post -d '{"name":"mesh-lab-net-peer-b-down","steps":1}' \
  "http://${PEER_A_HOST}/api/mesh/tasks")"
LIVE_ID="$(echo "$LIVE_TASK" | python3 -c 'import json,sys; print(json.load(sys.stdin)["taskId"])')"
LIVE_SCHED="$(mesh_post -d '{}' "http://${PEER_A_HOST}/api/mesh/tasks/${LIVE_ID}/schedule")"
LEASE_TOKEN="$(echo "$LIVE_SCHED" | python3 -c 'import json,sys; print(json.load(sys.stdin).get("leaseToken") or "")')"
[[ -n "$LEASE_TOKEN" ]] || { echo "Missing leaseToken for peer-b outage test" >&2; exit 1; }

mesh_patch -d "{\"status\":2,\"leaseToken\":\"${LEASE_TOKEN}\",\"reason\":\"mesh-lab-net-running\"}" \
  "http://${PEER_A_HOST}/api/mesh/tasks/${LIVE_ID}/status" >/dev/null

echo "Stopping peer-b container (simulate worker network partition)…"
mesh_lab_compose stop peer-b >/dev/null
sleep 2
mesh_lab_curl_on_net_must_fail "http://peer-b:8080/health"
echo "peer-b unreachable on bridge while stopped — OK"

echo "Starting peer-b…"
mesh_lab_compose start peer-b >/dev/null
for _i in $(seq 1 45); do
  if mesh_lab_curl_on_net -fsS --connect-timeout 3 "http://peer-b:8080/health" >/dev/null 2>&1; then
    break
  fi
  sleep 1
done
mesh_lab_curl_on_net -fsS --connect-timeout 5 "http://peer-b:8080/health" >/dev/null
echo "peer-b reachable again after start — OK"

# Director still accepts lease PATCH after partition (control plane on peer-a)
mesh_patch -d "{\"status\":3,\"leaseToken\":\"${LEASE_TOKEN}\",\"resultSummary\":\"mesh-lab-net-recovered\"}" \
  "http://${PEER_A_HOST}/api/mesh/tasks/${LIVE_ID}/status" >/dev/null
echo "Director PATCH after peer-b recovery — OK"

# --- 5) Director restart: LiteDB task + lease survive (when persistence enabled) ---
PERSIST_PROVIDER="$(source_env_kv Nexo__Mesh__Persistence__Provider)"
[[ -n "$PERSIST_PROVIDER" ]] || PERSIST_PROVIDER="LiteDb"
if [[ "$(echo "${PERSIST_PROVIDER}" | tr '[:upper:]' '[:lower:]')" == "litedb" ]]; then
  mesh_delete "http://${PEER_A_HOST}/api/mesh/fleet/nodes/mesh-lab-net-live" >/dev/null 2>&1 || true
  mesh_post -d "$(mesh_lab_fleet_register_json mesh-lab-net-restart http://peer-b:8080 Trusted)" \
    "http://${PEER_A_HOST}/api/mesh/fleet/nodes" >/dev/null

  RST_TASK="$(mesh_post -d '{"name":"mesh-lab-net-director-restart","steps":1}' \
    "http://${PEER_A_HOST}/api/mesh/tasks")"
  RST_ID="$(echo "$RST_TASK" | python3 -c 'import json,sys; print(json.load(sys.stdin)["taskId"])')"
  RST_SCHED="$(mesh_post -d '{}' "http://${PEER_A_HOST}/api/mesh/tasks/${RST_ID}/schedule")"
  RST_LEASE="$(echo "$RST_SCHED" | python3 -c 'import json,sys; print(json.load(sys.stdin).get("leaseToken") or "")')"
  [[ -n "$RST_LEASE" ]] || { echo "Missing lease before director restart" >&2; exit 1; }

  echo "Restarting peer-a director…"
  mesh_lab_compose restart peer-a >/dev/null
  mesh_lab_wait_peer_a

  RST_GET="$(curl -fsS -H "X-Nexo-Api-Key: ${API_KEY}" "http://${PEER_A_HOST}/api/mesh/tasks/${RST_ID}")"
  echo "$RST_GET" | python3 -c '
import json, sys
t = json.load(sys.stdin)
if t.get("status") not in (1, "Assigned"):
    sys.stderr.write("Expected Assigned after director restart, got %r\n" % (t.get("status"),))
    sys.exit(1)
if not (t.get("leaseToken") or "").strip():
    sys.stderr.write("Expected leaseToken persisted across restart\n")
    sys.exit(1)
print("Task + lease survived director restart — OK")
'

  curl -fsS -X PATCH -H "Content-Type: application/json" -H "X-Nexo-Api-Key: ${API_KEY}" \
    -d "{\"status\":2,\"leaseToken\":\"${RST_LEASE}\",\"reason\":\"mesh-lab-net-after-restart\"}" \
    "http://${PEER_A_HOST}/api/mesh/tasks/${RST_ID}/status" >/dev/null
  echo "Lease still valid for PATCH after director restart — OK"

  mesh_delete "http://${PEER_A_HOST}/api/mesh/fleet/nodes/mesh-lab-net-restart" >/dev/null 2>&1 || true
else
  echo "(Skipping director-restart lease test: persistence not LiteDb)"
fi

cleanup_test_peers
restore_verify_peer

echo "== Mesh lab verify-network-negative: OK =="
