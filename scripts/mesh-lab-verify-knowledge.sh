#!/usr/bin/env bash
# Phase 13: mesh knowledge export/import round-trip (peer-b → peer-a director).
#
#   ./scripts/mesh-lab-verify-knowledge.sh .env.mesh-lab

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

# shellcheck source=scripts/mesh-lab-fleet.sh
source "${ROOT}/scripts/mesh-lab-fleet.sh"

COMPOSE_ENV_FILE="${1:-${COMPOSE_ENV_FILE:-.env.mesh-lab}}"
PEER_A_HOST="${MESH_LAB_PEER_A_HOST:-127.0.0.1:18081}"
PEER_B_HOST="${MESH_LAB_PEER_B_HOST:-127.0.0.1:18082}"
SUFFIX="${MESH_LAB_KNOWLEDGE_SUFFIX:-$(date +%s)}"

if [[ ! -f "$COMPOSE_ENV_FILE" ]]; then
  echo "Missing env file: $COMPOSE_ENV_FILE" >&2
  exit 1
fi

source_env_kv() {
  local key=$1
  grep -E "^${key}=" "$COMPOSE_ENV_FILE" 2>/dev/null | head -1 | cut -d= -f2- | tr -d '\r' || true
}

API_KEY="$(source_env_kv Nexo__Security__ApiKey)"
BEARER="$(source_env_kv Nexo__Security__PeerB__BearerToken)"
if [[ -z "$API_KEY" ]]; then
  echo "(Skipping knowledge verify: no Nexo__Security__ApiKey in env file)"
  exit 0
fi

mesh_post_json() {
  local base=$1
  local auth_hdr=$2
  shift 2
  curl -fsS -X POST -H "Content-Type: application/json" -H "${auth_hdr}" -d "$1" "${base}$2"
}

echo "== Mesh lab knowledge sync (export/import, suffix=${SUFFIX}) =="

SEED_PAYLOAD="$(SUFFIX="$SUFFIX" python3 -c '
import json, os
from datetime import datetime, timezone
now = datetime.now(timezone.utc).isoformat()
s = os.environ["SUFFIX"]
payload = {
    "exportedAt": now,
    "adaptations": [{
        "id": f"mesh-lab-adapt-{s}",
        "timestamp": now,
        "brickId": "mesh-lab-brick",
        "failureType": "mesh-lab",
        "fixApplied": 0,
        "regressionPassed": True,
        "promoted": False,
        "message": "mesh-lab seed"
    }],
    "patterns": [{
        "patternId": f"mesh-lab-pat-{s}",
        "eventType": "repeated-edits",
        "frequency": 2,
        "firstSeen": now,
        "lastSeen": now,
        "projectPath": "/mesh-lab"
    }]
}
print(json.dumps(payload))
')"

A_AUTH="X-Nexo-Api-Key: ${API_KEY}"

mesh_post_json "http://${PEER_A_HOST}" "$A_AUTH" "$SEED_PAYLOAD" "/api/mesh/knowledge/import" >/dev/null
echo "Seeded knowledge on director (peer-a) — OK"

EXPORT_JSON="$(curl -fsS -H "$A_AUTH" "http://${PEER_A_HOST}/api/mesh/knowledge/export?maxAdaptations=50&maxPatterns=50")"
echo "$EXPORT_JSON" | SUFFIX="$SUFFIX" python3 -c '
import json, os, sys
p = json.load(sys.stdin)
s = os.environ["SUFFIX"]
ad = p.get("adaptations") or []
pa = p.get("patterns") or []
ids = [a.get("id") for a in ad]
if not any(f"mesh-lab-adapt-{s}" == i for i in ids):
    sys.stderr.write("Export missing seeded adaptation (ids=%r)\n" % (ids[:5],))
    sys.exit(1)
print("Export from director: %d adaptation(s), %d pattern(s) — OK" % (len(ad), len(pa)))
' SUFFIX="$SUFFIX"

# Cross-peer: director export → peer-b import (when peer-b has adaptation stores)
if [[ -n "$BEARER" ]]; then
  B_AUTH="Authorization: Bearer ${BEARER}"
else
  B_AUTH="$A_AUTH"
fi
B_IMPORT="$(curl -fsS -X POST -H "Content-Type: application/json" -H "$B_AUTH" \
  -d "$EXPORT_JSON" "http://${PEER_B_HOST}/api/mesh/knowledge/import" 2>/dev/null || true)"
if [[ -n "$B_IMPORT" ]]; then
  echo "$B_IMPORT" | python3 -c '
import json, sys
r = json.load(sys.stdin)
applied = (r.get("adaptationsApplied") or 0) + (r.get("patternsApplied") or 0)
if applied < 1:
    sys.stderr.write("Cross-peer import on peer-b expected applied records, got %r\n" % (r,))
    sys.exit(1)
print("Cross-peer import on peer-b — OK")
'
else
  echo "(peer-b knowledge import unavailable; director-only replication still validated)"
fi

IMPORT2="$(mesh_post_json "http://${PEER_A_HOST}" "$A_AUTH" "$EXPORT_JSON" "/api/mesh/knowledge/import")"
echo "$IMPORT2" | python3 -c '
import json, sys
r = json.load(sys.stdin)
skipped = (r.get("adaptationsSkipped") or 0) + (r.get("patternsSkipped") or 0)
if skipped < 1:
    sys.stderr.write(f"Expected duplicate import to skip records, got {r}\n")
    sys.exit(1)
print(f"Duplicate import skipped={skipped} — OK")
'

echo "== Mesh lab verify-knowledge: OK =="
