#!/usr/bin/env bash
# Verify heterogeneous mesh lab: peer-a (ApiKey), peer-b (ApiKey or Bearer), optional worker auth paths,
# mesh fleet mutate + mesh task create→schedule→placement (Assigned).
#
#   ./scripts/mesh-lab-verify.sh .env.mesh-lab

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

if ! command -v python3 >/dev/null 2>&1; then
  echo "python3 is required for mesh task JSON checks (install Python 3)." >&2
  exit 1
fi

COMPOSE_FILE="${COMPOSE_FILE:-docker-compose.mesh-lab.yml}"
COMPOSE_ENV_FILE="${1:-${COMPOSE_ENV_FILE:-.env.mesh-lab}}"
PEER_A_HOST="${MESH_LAB_PEER_A_HOST:-127.0.0.1:18081}"
PEER_B_HOST="${MESH_LAB_PEER_B_HOST:-127.0.0.1:18082}"
CURL_IMAGE="${MESH_LAB_CURL_IMAGE:-curlimages/curl:8.5.0}"

if [[ ! -f "$COMPOSE_ENV_FILE" ]]; then
  echo "Missing env file: $COMPOSE_ENV_FILE" >&2
  echo "Copy docs/config/mesh-lab.env.example to .env.mesh-lab and set secrets." >&2
  exit 1
fi

# shellcheck disable=SC1090
source_env_kv() {
  local key=$1
  grep -E "^${key}=" "$COMPOSE_ENV_FILE" 2>/dev/null | head -1 | cut -d= -f2- | tr -d '\r' || true
}

# shellcheck source=scripts/mesh-lab-fleet.sh
source "${ROOT}/scripts/mesh-lab-fleet.sh"

API_KEY="$(source_env_kv Nexo__Security__ApiKey)"
MESH_LAB_PEER_REGISTRATION_KEY="$(source_env_kv MESH_LAB_PEER_REGISTRATION_KEY)"
export MESH_LAB_PEER_REGISTRATION_KEY
BEARER="$(source_env_kv Nexo__Security__PeerB__BearerToken)"
BASIC_USER="$(source_env_kv Nexo__Security__Worker__BasicAuthUsername)"
BASIC_PASS="$(source_env_kv Nexo__Security__Worker__BasicAuthPassword)"
WORKER_PUBLISH_FALLBACK="$(source_env_kv MESH_LAB_WORKER_PUBLISH)"
[[ -n "$WORKER_PUBLISH_FALLBACK" ]] || WORKER_PUBLISH_FALLBACK="127.0.0.1:18083"

compose() {
  docker compose -f "$COMPOSE_FILE" --env-file "$COMPOSE_ENV_FILE" "$@"
}

compose_workers() {
  docker compose --profile workers -f "$COMPOSE_FILE" --env-file "$COMPOSE_ENV_FILE" "$@"
}

lab_network() {
  local cid nets pick
  cid="$(compose ps -q peer-a)"
  if [[ -z "$cid" ]]; then
    echo "peer-a container not running" >&2
    exit 1
  fi
  nets="$(docker inspect "$cid" --format '{{range $k,$v := .NetworkSettings.Networks}}{{printf "%s\n" $k}}{{end}}')"
  pick="$(echo "$nets" | grep mesh_lab | head -1 | tr -d '\r\n')"
  if [[ -z "$pick" ]]; then
    pick="$(echo "$nets" | head -1 | tr -d '\r\n')"
  fi
  if [[ -z "$pick" ]]; then
    echo "Could not determine Docker network name from peer-a" >&2
    exit 1
  fi
  echo "$pick"
}

curl_on_lab_net() {
  local url=$1
  local net
  net="$(lab_network)"
  if [[ -z "$net" ]]; then
    echo "Could not resolve lab Docker network from peer-a" >&2
    exit 1
  fi
  docker run --rm --network "$net" "$CURL_IMAGE" -fsS "$url"
}

# Same bridge network as curl_on_lab_net; pass curl args (URL is typically last).
curl_on_lab_net_extra() {
  local net
  net="$(lab_network)"
  if [[ -z "$net" ]]; then
    echo "Could not resolve lab Docker network from peer-a" >&2
    exit 1
  fi
  docker run --rm --network "$net" "$CURL_IMAGE" -fsS "$@"
}

# Prefer published host port (docker compose port) so checks use ordinary host curl.
# Fallback: bridge IPv4 from inspect (Docker Desktop sometimes omits service DNS for ephemeral curl).
worker_primary_http_base() {
  local peer_net wid ip
  peer_net="$(lab_network)"
  wid="$(compose_workers ps -q worker | head -1)"
  [[ -n "$wid" ]] || return 1
  ip="$(
    docker inspect "$wid" | python3 -c '
import json, re, sys

peer = sys.argv[1]
ns = json.load(sys.stdin)[0]["NetworkSettings"]["Networks"]
ordered = []
cfg = ns.get(peer)
if cfg is not None:
    ordered.append(cfg)
ordered.extend(v for k, v in ns.items() if k != peer)

for c in ordered:
    # Docker usually fills IPAddress for bridge attachments; IPv4Address may be absent.
    for raw in (c.get("IPAddress"), c.get("IPv4Address")):
        if not raw:
            continue
        s = str(raw).split("/", 1)[0].strip()
        if re.fullmatch(r"\d+\.\d+\.\d+\.\d+", s):
            print(s)
            raise SystemExit(0)
raise SystemExit(1)
' "$peer_net"
  )" || return 1
  printf 'http://%s:8080' "$ip"
}

echo "== Wait: host → peer-a ($PEER_A_HOST/health) [expect ApiKey path optional for GET] =="
for i in $(seq 1 90); do
  if curl -fsS "http://${PEER_A_HOST}/health" >/dev/null 2>&1; then
    echo "peer-a reachable on host after ${i} attempt(s)"
    break
  fi
  if [[ "$i" -eq 90 ]]; then
    echo "peer-a not reachable on http://${PEER_A_HOST}/health" >&2
    compose logs --tail 80 peer-a
    exit 1
  fi
  sleep 2
done

echo "== Wait: host → peer-b ($PEER_B_HOST/health) =="
for i in $(seq 1 90); do
  if [[ -n "$API_KEY" ]] && curl -fsS -H "X-Nexo-Api-Key: ${API_KEY}" "http://${PEER_B_HOST}/health" >/dev/null 2>&1; then
    echo "peer-b reachable (API key) after ${i} attempt(s)"
    break
  fi
  if [[ -n "$BEARER" ]] && curl -fsS -H "Authorization: Bearer ${BEARER}" "http://${PEER_B_HOST}/health" >/dev/null 2>&1; then
    echo "peer-b reachable (Bearer) after ${i} attempt(s)"
    break
  fi
  if curl -fsS "http://${PEER_B_HOST}/health" >/dev/null 2>&1; then
    echo "peer-b reachable (no auth) after ${i} attempt(s)"
    break
  fi
  if [[ "$i" -eq 90 ]]; then
    echo "peer-b not reachable on http://${PEER_B_HOST}/health" >&2
    compose logs --tail 80 peer-b
    exit 1
  fi
  sleep 2
done

echo "== Cross-container: curl → peer-a:8080/health =="
curl_on_lab_net "http://peer-a:8080/health" | head -c 240
echo

echo "== Cross-container: curl → peer-b:8080/health =="
curl_on_lab_net "http://peer-b:8080/health" | head -c 240
echo

if [[ -n "$BEARER" ]]; then
  echo "== Host → peer-b /health with Bearer only (validates second auth path) =="
  curl -fsS -H "Authorization: Bearer ${BEARER}" "http://${PEER_B_HOST}/health" | head -c 200
  echo
fi

echo "== Mesh API: host → peer-a GET /api/mesh/fleet/nodes (read path, JSON array) =="
FA_NODES="$(curl -fsS "http://${PEER_A_HOST}/api/mesh/fleet/nodes")"
echo "$FA_NODES" | head -c 500
echo
if [[ ! "$FA_NODES" =~ ^[[:space:]]*\[ ]]; then
  echo "Expected JSON array from GET /api/mesh/fleet/nodes on peer-a" >&2
  exit 1
fi

echo "== Mesh API: cross-container → peer-a /api/mesh/fleet/nodes =="
curl_on_lab_net_extra "http://peer-a:8080/api/mesh/fleet/nodes" | head -c 500
echo

if [[ -n "$API_KEY" ]]; then
  echo "== Mesh API: host → peer-a GET /api/mesh/elastic/status (fleet director) =="
  ELASTIC_JSON="$(curl -fsS -H "X-Nexo-Api-Key: ${API_KEY}" "http://${PEER_A_HOST}/api/mesh/elastic/status")"
  echo "$ELASTIC_JSON" | head -c 500
  echo
  if [[ ! "$ELASTIC_JSON" =~ ^[[:space:]]*\{ ]]; then
    echo "Expected JSON object from GET /api/mesh/elastic/status on peer-a (fleet host)" >&2
    exit 1
  fi
fi

if [[ -n "$API_KEY" ]]; then
  echo "== Mesh API: POST register fleet node → peer-b DNS name (mutating + ApiKey) =="
  curl -fsS -X POST \
    -H "Content-Type: application/json" \
    -H "X-Nexo-Api-Key: ${API_KEY}" \
    -d "$(mesh_lab_fleet_register_json mesh-lab-verify-peer http://peer-b:8080 Trusted)" \
    "http://${PEER_A_HOST}/api/mesh/fleet/nodes" | head -c 500
  echo
  FA_NODES2="$(curl -fsS "http://${PEER_A_HOST}/api/mesh/fleet/nodes")"
  if [[ "$FA_NODES2" != *mesh-lab-verify-peer* ]]; then
    echo "Fleet registry did not retain mesh-lab-verify-peer (got: ${FA_NODES2:0:200}…)" >&2
    exit 1
  fi
  echo "Fleet registry lists mesh-lab-verify-peer — OK"

  if [[ "${MESH_LAB_SKIP_WORKER_EXECUTOR:-}" != "1" && "${MESH_LAB_SKIP_WORKER_EXECUTOR:-}" != "true" ]] \
    && compose_workers ps -q worker 2>/dev/null | grep -q .; then
    echo "== Mesh worker executor: schedule task → worker completes without manual PATCH =="
    EXEC_TASK_JSON="$(curl -fsS -X POST \
      -H "Content-Type: application/json" \
      -H "X-Nexo-Api-Key: ${API_KEY}" \
      -d '{"name":"mesh-lab-worker-exec-task","steps":1,"requiredBrickIds":[]}' \
      "http://${PEER_A_HOST}/api/mesh/tasks")"
    EXEC_TASK_ID="$(echo "$EXEC_TASK_JSON" | python3 -c 'import json,sys; print(json.load(sys.stdin)["taskId"])')"
    curl -fsS -X POST \
      -H "Content-Type: application/json" \
      -H "X-Nexo-Api-Key: ${API_KEY}" \
      -d '{}' \
      "http://${PEER_A_HOST}/api/mesh/tasks/${EXEC_TASK_ID}/schedule" >/dev/null
    EXEC_OK=false
    for _w in $(seq 1 90); do
      EXEC_FINAL="$(curl -fsS -H "X-Nexo-Api-Key: ${API_KEY}" "http://${PEER_A_HOST}/api/mesh/tasks/${EXEC_TASK_ID}")"
      if echo "$EXEC_FINAL" | python3 -c '
import json, sys
t = json.load(sys.stdin)
st = t.get("status")
if st not in (3, "Succeeded"):
    sys.exit(1)
rs = t.get("resultSummary") or ""
if rs != "mesh-lab-worker-executor-complete":
    sys.stderr.write("Unexpected resultSummary: %r\n" % (rs,))
    sys.exit(1)
if t.get("leaseToken"):
    sys.stderr.write("Expected lease cleared after worker executor\n")
    sys.exit(1)
'; then
        EXEC_OK=true
        break
      fi
      sleep 1
    done
    if [[ "$EXEC_OK" != true ]]; then
      echo "Worker executor did not reach Succeeded within 90s (last: ${EXEC_FINAL:0:400}…)" >&2
      exit 1
    fi
    echo "Mesh worker executor OK — task ${EXEC_TASK_ID} Succeeded autonomously"
  else
    echo "(Skipping mesh worker executor: set MESH_LAB_SKIP_WORKER_EXECUTOR=1 to silence; needs workers profile)"
  fi

  echo "== Mesh task placement: POST /tasks → POST /tasks/{id}/schedule → Assigned =="
  TASK_JSON="$(curl -fsS -X POST \
    -H "Content-Type: application/json" \
    -H "X-Nexo-Api-Key: ${API_KEY}" \
    -d '{"name":"mesh-lab-verify-task","steps":1,"requiredBrickIds":[]}' \
    "http://${PEER_A_HOST}/api/mesh/tasks")"
  echo "$TASK_JSON" | head -c 500
  echo
  TASK_ID="$(echo "$TASK_JSON" | python3 -c 'import json,sys; print(json.load(sys.stdin)["taskId"])')"
  SCHED_JSON="$(curl -fsS -X POST \
    -H "Content-Type: application/json" \
    -H "X-Nexo-Api-Key: ${API_KEY}" \
    -d '{}' \
    "http://${PEER_A_HOST}/api/mesh/tasks/${TASK_ID}/schedule")"
  echo "$SCHED_JSON" | head -c 500
  echo
  if ! echo "$SCHED_JSON" | python3 -c 'import json,sys; s=json.load(sys.stdin).get("status"); sys.exit(0 if s in (1,"Assigned") else 1)'; then
    echo "Schedule response did not yield Assigned status (see JSON above)." >&2
    exit 1
  fi
  GET_JSON="$(curl -fsS -H "X-Nexo-Api-Key: ${API_KEY}" "http://${PEER_A_HOST}/api/mesh/tasks/${TASK_ID}")"
  echo "$GET_JSON" | python3 -c '
import json, sys
t = json.load(sys.stdin)
st = t.get("status")
if st not in (1, "Assigned"):
    sys.stderr.write(f"Expected Assigned, got status={st!r}\n")
    sys.exit(1)
if not str(t.get("assignedPeerId") or "").strip():
    sys.stderr.write("assignedPeerId missing or empty\n")
    sys.exit(1)
if not str(t.get("assignedApiBaseUrl") or "").strip():
    sys.stderr.write("assignedApiBaseUrl missing or empty\n")
    sys.exit(1)
print("Mesh task placement OK — peerId=%r base=%r" % (t.get("assignedPeerId"), t.get("assignedApiBaseUrl")))
' || exit 1

  LEASE_TOKEN="$(echo "$SCHED_JSON" | python3 -c 'import json,sys; print(json.load(sys.stdin).get("leaseToken") or "")')"
  if [[ -z "$LEASE_TOKEN" ]]; then
    echo "Schedule response missing leaseToken (required for lifecycle checks)." >&2
    exit 1
  fi

  echo "== Mesh task lifecycle: PATCH Running → Succeeded (leaseToken on director) =="
  BAD_PATCH_CODE="$(curl -s -o /dev/null -w '%{http_code}' -X PATCH \
    -H "Content-Type: application/json" \
    -H "X-Nexo-Api-Key: ${API_KEY}" \
    -d "{\"status\":2,\"leaseToken\":\"wrong-mesh-lab-token\"}" \
    "http://${PEER_A_HOST}/api/mesh/tasks/${TASK_ID}/status")"
  if [[ "$BAD_PATCH_CODE" != "409" ]]; then
    echo "Expected HTTP 409 for wrong leaseToken on PATCH Running, got ${BAD_PATCH_CODE}" >&2
    exit 1
  fi
  echo "Wrong leaseToken rejected with 409 — OK"

  RUN_JSON="$(curl -fsS -X PATCH \
    -H "Content-Type: application/json" \
    -H "X-Nexo-Api-Key: ${API_KEY}" \
    -d "{\"status\":2,\"leaseToken\":\"${LEASE_TOKEN}\",\"reason\":\"mesh-lab-verify-running\"}" \
    "http://${PEER_A_HOST}/api/mesh/tasks/${TASK_ID}/status")"
  echo "$RUN_JSON" | head -c 400
  echo
  if ! echo "$RUN_JSON" | python3 -c 'import json,sys; s=json.load(sys.stdin).get("status"); sys.exit(0 if s in (2,"Running") else 1)'; then
    echo "PATCH Running did not yield Running status." >&2
    exit 1
  fi

  DONE_JSON="$(curl -fsS -X PATCH \
    -H "Content-Type: application/json" \
    -H "X-Nexo-Api-Key: ${API_KEY}" \
    -d "{\"status\":3,\"leaseToken\":\"${LEASE_TOKEN}\",\"resultSummary\":\"mesh-lab-verify-complete\"}" \
    "http://${PEER_A_HOST}/api/mesh/tasks/${TASK_ID}/status")"
  echo "$DONE_JSON" | head -c 400
  echo
  FINAL_JSON="$(curl -fsS -H "X-Nexo-Api-Key: ${API_KEY}" "http://${PEER_A_HOST}/api/mesh/tasks/${TASK_ID}")"
  echo "$FINAL_JSON" | python3 -c '
import json, sys
t = json.load(sys.stdin)
st = t.get("status")
if st not in (3, "Succeeded"):
    sys.stderr.write(f"Expected Succeeded, got status={st!r}\n")
    sys.exit(1)
if t.get("leaseToken"):
    sys.stderr.write("Expected leaseToken cleared after Succeeded\n")
    sys.exit(1)
rs = t.get("resultSummary") or ""
if rs != "mesh-lab-verify-complete":
    sys.stderr.write("Unexpected resultSummary: %r\n" % (rs,))
    sys.exit(1)
print("Mesh task lifecycle OK — Succeeded, lease cleared")
' || exit 1

  VERIFY_BRICK_ID="${MESH_LAB_VERIFY_BRICK_ID:-}"
  echo "== Mesh brick HTTP: GET catalog + POST execute on peer-b (bridge) =="
  BRICKS_JSON="$(curl_on_lab_net_extra -H "X-Nexo-Api-Key: ${API_KEY}" "http://peer-b:8080/api/bricks")"
  echo "$BRICKS_JSON" | head -c 500
  echo
  if [[ -z "$VERIFY_BRICK_ID" ]]; then
    VERIFY_BRICK_ID="$(echo "$BRICKS_JSON" | python3 -c '
import json, sys
data = json.load(sys.stdin)
bricks = data.get("bricks") or data.get("Bricks") or []
if not bricks:
    raise SystemExit(1)
b0 = bricks[0]
bid = b0.get("id") or b0.get("Id") or b0.get("brickId") or b0.get("BrickId")
if not bid:
    raise SystemExit(1)
print(bid)
' 2>/dev/null || true)"
  fi
  if [[ -z "$VERIFY_BRICK_ID" ]]; then
    echo "(Skipping brick execute: empty catalog; set MESH_LAB_VERIFY_BRICK_ID to force a brick id)"
  else
    EXEC_BODY="$(VERIFY_BRICK_ID="$VERIFY_BRICK_ID" python3 -c 'import json,os; print(json.dumps({"brickId":os.environ["VERIFY_BRICK_ID"],"implementation":"Deterministic","input":{}}))')"
    curl_on_lab_net_extra -X POST \
      -H "Content-Type: application/json" \
      -H "X-Nexo-Api-Key: ${API_KEY}" \
      -d "$EXEC_BODY" \
      "http://peer-b:8080/api/bricks/${VERIFY_BRICK_ID}/execute" | head -c 500
    echo
    echo "Brick execute HTTP round-trip OK — brickId=${VERIFY_BRICK_ID}"
  fi
else
  echo "(Skipping mesh fleet POST test: no Nexo__Security__ApiKey in env file)"
fi

if [[ -n "$API_KEY" ]]; then
  echo "== Mesh API: cross-container GET elastic/status → peer-a (fleet director) =="
  curl_on_lab_net_extra -H "X-Nexo-Api-Key: ${API_KEY}" "http://peer-a:8080/api/mesh/elastic/status" | head -c 500
  echo
fi

if [[ "${MESH_LAB_SKIP_GOV_VERIFY:-}" != "1" && "${MESH_LAB_SKIP_GOV_VERIFY:-}" != "true" ]]; then
  chmod +x scripts/mesh-lab-verify-governance.sh 2>/dev/null || true
  ./scripts/mesh-lab-verify-governance.sh "$COMPOSE_ENV_FILE"
fi

if [[ "${MESH_LAB_SKIP_DIRECTOR_CLI_VERIFY:-}" != "1" && "${MESH_LAB_SKIP_DIRECTOR_CLI_VERIFY:-}" != "true" ]]; then
  chmod +x scripts/mesh-lab-verify-director-cli.sh 2>/dev/null || true
  ./scripts/mesh-lab-verify-director-cli.sh "$COMPOSE_ENV_FILE"
fi

if [[ "${MESH_LAB_SKIP_PERSISTENCE_VERIFY:-}" != "1" && "${MESH_LAB_SKIP_PERSISTENCE_VERIFY:-}" != "true" ]]; then
  chmod +x scripts/mesh-lab-verify-persistence.sh 2>/dev/null || true
  ./scripts/mesh-lab-verify-persistence.sh "$COMPOSE_ENV_FILE"
fi

if [[ "${MESH_LAB_SKIP_TRUST_VERIFY:-}" != "1" && "${MESH_LAB_SKIP_TRUST_VERIFY:-}" != "true" ]]; then
  chmod +x scripts/mesh-lab-verify-trust.sh 2>/dev/null || true
  ./scripts/mesh-lab-verify-trust.sh "$COMPOSE_ENV_FILE"
fi

if [[ "${MESH_LAB_SKIP_NETWORK_NEGATIVE_VERIFY:-}" != "1" && "${MESH_LAB_SKIP_NETWORK_NEGATIVE_VERIFY:-}" != "true" ]]; then
  chmod +x scripts/mesh-lab-verify-network-negative.sh scripts/mesh-lab-net.sh 2>/dev/null || true
  ./scripts/mesh-lab-verify-network-negative.sh "$COMPOSE_ENV_FILE"
fi

if [[ "${MESH_LAB_SKIP_KNOWLEDGE_VERIFY:-}" != "1" && "${MESH_LAB_SKIP_KNOWLEDGE_VERIFY:-}" != "true" ]]; then
  chmod +x scripts/mesh-lab-verify-knowledge.sh 2>/dev/null || true
  ./scripts/mesh-lab-verify-knowledge.sh "$COMPOSE_ENV_FILE"
fi

if [[ "${MESH_LAB_SKIP_FEDERATION_VERIFY:-}" != "1" && "${MESH_LAB_SKIP_FEDERATION_VERIFY:-}" != "true" ]]; then
  chmod +x scripts/mesh-lab-verify-federation.sh 2>/dev/null || true
  ./scripts/mesh-lab-verify-federation.sh "$COMPOSE_ENV_FILE"
fi

if [[ "${MESH_LAB_SKIP_RETRY_RESULT_VERIFY:-}" != "1" && "${MESH_LAB_SKIP_RETRY_RESULT_VERIFY:-}" != "true" ]]; then
  chmod +x scripts/mesh-lab-verify-retry-result.sh 2>/dev/null || true
  ./scripts/mesh-lab-verify-retry-result.sh "$COMPOSE_ENV_FILE"
fi

if [[ "${MESH_LAB_SKIP_ELASTIC_VERIFY:-}" != "1" && "${MESH_LAB_SKIP_ELASTIC_VERIFY:-}" != "true" ]]; then
  chmod +x scripts/mesh-lab-verify-elastic.sh 2>/dev/null || true
  ./scripts/mesh-lab-verify-elastic.sh "$COMPOSE_ENV_FILE"
fi

if compose_workers ps -q worker 2>/dev/null | grep -q .; then
  chmod +x scripts/mesh-lab-verify-entitlements.sh 2>/dev/null || true
  ./scripts/mesh-lab-verify-entitlements.sh "$COMPOSE_ENV_FILE"
fi

if compose_workers ps -q worker 2>/dev/null | grep -q .; then
  WORKER_PUBLISHED=""
  wid="$(compose_workers ps -q worker | head -1 | tr -d '\r\n[:space:]')"
  if [[ -n "$wid" ]]; then
    WORKER_PUBLISHED="$(docker port "$wid" 8080/tcp 2>/dev/null || docker port "$wid" 8080 2>/dev/null || true)"
    WORKER_PUBLISHED="$(echo "$WORKER_PUBLISHED" | head -1 | tr -d '\r\n[:space:]')"
  fi
  if [[ -z "$WORKER_PUBLISHED" ]]; then
    WORKER_PUBLISHED="$(compose_workers port worker 8080 2>/dev/null | tr -d '\r\n[:space:]')" || true
  fi
  WORKER_PUBLISHED="${WORKER_PUBLISHED//0.0.0.0/127.0.0.1}"
  if [[ -z "$WORKER_PUBLISHED" ]]; then
    for _i in $(seq 1 20); do
      if curl -fsS -m 3 "http://${WORKER_PUBLISH_FALLBACK}/health" >/dev/null 2>&1; then
        WORKER_PUBLISHED="${WORKER_PUBLISH_FALLBACK}"
        break
      fi
      sleep 1
    done
  fi
  if [[ -n "$WORKER_PUBLISHED" ]]; then
    WORKER_HTTP="http://${WORKER_PUBLISHED}"
    WORKER_BRIDGE_MODE=0
  else
    WORKER_HTTP="$(worker_primary_http_base)" || {
      echo "Could not resolve worker HTTP base (publish worker 8080 or ensure bridge IPv4); is the worker container healthy?" >&2
      exit 1
    }
    WORKER_BRIDGE_MODE=1
  fi
  worker_via_note="$([[ "$WORKER_BRIDGE_MODE" == 1 ]] && echo '[via bridge]' || echo '[via published port]')"
  echo "== Worker tier: /health (GET, no auth) → ${WORKER_HTTP}/health ${worker_via_note} =="
  if [[ "$WORKER_BRIDGE_MODE" == 1 ]]; then
    curl_on_lab_net "${WORKER_HTTP}/health" | head -c 200
  else
    curl -fsS "${WORKER_HTTP}/health" | head -c 200
  fi
  echo
  if [[ -n "$API_KEY" ]]; then
    echo "== Worker tier: GET /api/mesh/fleet/nodes with X-Nexo-Api-Key =="
    if [[ "$WORKER_BRIDGE_MODE" == 1 ]]; then
      curl_on_lab_net_extra -H "X-Nexo-Api-Key: ${API_KEY}" "${WORKER_HTTP}/api/mesh/fleet/nodes" | head -c 400
    else
      curl -fsS -H "X-Nexo-Api-Key: ${API_KEY}" "${WORKER_HTTP}/api/mesh/fleet/nodes" | head -c 400
    fi
    echo
  fi
  if [[ -n "$BASIC_PASS" ]]; then
    WUSER="${BASIC_USER:-nexo}"
    echo "== Worker tier: GET /api/mesh/fleet/nodes with Basic auth =="
    if [[ "$WORKER_BRIDGE_MODE" == 1 ]]; then
      curl_on_lab_net_extra -u "${WUSER}:${BASIC_PASS}" "${WORKER_HTTP}/api/mesh/fleet/nodes" | head -c 400
    else
      curl -fsS -u "${WUSER}:${BASIC_PASS}" "${WORKER_HTTP}/api/mesh/fleet/nodes" | head -c 400
    fi
    echo
  else
    echo "(Skipping worker Basic-auth mesh read: Nexo__Security__Worker__BasicAuthPassword unset)"
  fi
else
  echo "(worker tier not running; use --profile workers to include workers)"
fi

echo "== Mesh lab verify: OK =="
