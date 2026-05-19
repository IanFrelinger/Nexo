#!/usr/bin/env bash
# Shared fleet register JSON for mesh lab scripts (peer registration key when required).

mesh_lab_fleet_register_json() {
  local peer_id=$1 api_base=$2 trust_tier=${3:-Trusted} queue_depth=${4:-}
  MESH_LAB_PEER_REGISTRATION_KEY="${MESH_LAB_PEER_REGISTRATION_KEY:-}" \
    MESH_LAB_FLEET_QUEUE_DEPTH="${queue_depth}" \
    python3 -c '
import json, os, sys
peer_id, api_base, trust = sys.argv[1:4]
body = {"peerId": peer_id, "apiBaseUrl": api_base, "trustTier": trust}
key = (os.environ.get("MESH_LAB_PEER_REGISTRATION_KEY") or "").strip()
if key:
    body["peerRegistrationKey"] = key
q = (os.environ.get("MESH_LAB_FLEET_QUEUE_DEPTH") or "").strip()
if q:
    body["reportedQueueDepth"] = int(q)
print(json.dumps(body))
' "$peer_id" "$api_base" "$trust_tier"
}
