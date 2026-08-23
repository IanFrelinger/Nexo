#!/usr/bin/env bash
# Phase 13: federated brick catalog on peer-a (RemoteCatalogBaseUrls → peer-b) + execute via director.
#
#   ./scripts/mesh-lab-verify-federation.sh .env.mesh-lab

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

COMPOSE_FILE="${COMPOSE_FILE:-deploy/compose/docker-compose.mesh-lab.yml}"
COMPOSE_ENV_FILE="${1:-${COMPOSE_ENV_FILE:-.env.mesh-lab}}"
PEER_A_HOST="${MESH_LAB_PEER_A_HOST:-127.0.0.1:18081}"
CURL_IMAGE="${MESH_LAB_CURL_IMAGE:-curlimages/curl:8.5.0}"

if [[ ! -f "$COMPOSE_ENV_FILE" ]]; then
  echo "Missing env file: $COMPOSE_ENV_FILE" >&2
  exit 1
fi

source_env_kv() {
  local key=$1
  grep -E "^${key}=" "$COMPOSE_ENV_FILE" 2>/dev/null | head -1 | cut -d= -f2- | tr -d '\r' || true
}

API_KEY="$(source_env_kv Ashlar__Security__ApiKey)"
if [[ -z "$API_KEY" ]]; then
  echo "(Skipping federation verify: no Ashlar__Security__ApiKey in env file)"
  exit 0
fi

compose() {
  docker compose -f "$COMPOSE_FILE" --env-file "$COMPOSE_ENV_FILE" "$@"
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
  [[ -n "$pick" ]] || pick="$(echo "$nets" | head -1 | tr -d '\r\n')"
  echo "$pick"
}

curl_on_lab_net() {
  local net
  net="$(lab_network)"
  docker run --rm --network "$net" "$CURL_IMAGE" "$@"
}

echo "== Mesh lab brick federation (peer-a RemoteCatalog → peer-b) =="

# Catalog on peer-b (direct)
B_REMOTE="$(curl_on_lab_net -fsS -H "X-Ashlar-Api-Key: ${API_KEY}" "http://peer-b:8080/api/bricks")"
REMOTE_ID="$(echo "$B_REMOTE" | python3 -c '
import json, sys
data = json.load(sys.stdin)
bricks = data.get("bricks") or []
if not bricks:
    raise SystemExit(1)
b0 = bricks[0]
print(b0.get("id") or b0.get("Id") or b0.get("brickId") or "")
' 2>/dev/null || true)"

if [[ -z "$REMOTE_ID" ]]; then
  REMOTE_ID="${MESH_LAB_VERIFY_BRICK_ID:-}"
fi
if [[ -z "$REMOTE_ID" ]]; then
  echo "(Skipping federation: empty peer-b catalog; set MESH_LAB_VERIFY_BRICK_ID)" >&2
  exit 0
fi

# Catalog on peer-a should include remote brick id when federation is configured
A_CATALOG="$(curl_on_lab_net -fsS -H "X-Ashlar-Api-Key: ${API_KEY}" "http://peer-a:8080/api/bricks")"
echo "$A_CATALOG" | REMOTE_ID="$REMOTE_ID" python3 -c '
import json, os, sys
remote_id = os.environ["REMOTE_ID"]
data = json.load(sys.stdin)
bricks = data.get("bricks") or []
ids = {str(b.get("id") or b.get("Id") or "") for b in bricks}
if remote_id not in ids:
    sys.stderr.write(f"peer-a catalog missing remote brick {remote_id!r} (have {len(ids)} entries)\n")
    sys.stderr.write("Ensure Ashlar__BrickHost__RemoteCatalogBaseUrls points at peer-b with auth.\n")
    sys.exit(1)
print(f"peer-a federated catalog includes {remote_id!r} — OK")
'

EXEC_BODY="$(REMOTE_ID="$REMOTE_ID" python3 -c 'import json,os; print(json.dumps({"brickId":os.environ["REMOTE_ID"],"implementation":"Deterministic","input":{}}))')"
curl_on_lab_net -fsS -X POST \
  -H "Content-Type: application/json" \
  -H "X-Ashlar-Api-Key: ${API_KEY}" \
  -d "$EXEC_BODY" \
  "http://peer-a:8080/api/bricks/${REMOTE_ID}/execute" | python3 -c '
import json, sys
r = json.load(sys.stdin)
ok = r.get("success") if "success" in r else r.get("Success")
if ok is False:
    sys.stderr.write(f"Federated execute returned success=false: {r}\n")
    sys.exit(1)
print("Federated execute via peer-a (RemoteBrick → peer-b) — OK")
' || {
  echo "Federated brick execute via peer-a failed (brickId=${REMOTE_ID})" >&2
  exit 1
}

# Host-published peer-a catalog sanity
curl -fsS -H "X-Ashlar-Api-Key: ${API_KEY}" "http://${PEER_A_HOST}/api/bricks" | REMOTE_ID="$REMOTE_ID" python3 -c '
import json, os, sys
remote_id = os.environ["REMOTE_ID"]
data = json.load(sys.stdin)
ids = {str(b.get("id") or "") for b in (data.get("bricks") or [])}
if remote_id not in ids:
    sys.stderr.write("Host peer-a catalog missing federated brick\n")
    sys.exit(1)
print("Host peer-a catalog federation — OK")
'

echo "== Mesh lab verify-federation: OK =="
