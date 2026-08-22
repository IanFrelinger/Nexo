#!/usr/bin/env bash
# Smoke test for Ashlar.Commercial.Fleet.Host fleet director endpoints.
#
#   ./scripts/commercial-fleet-host-smoke.sh
#   FLEET_HOST_URL=http://127.0.0.1:18090 ./scripts/commercial-fleet-host-smoke.sh
#
# The host ships with AuthorizationMode=ApiKey and NO default key: FLEET_HOST_API_KEY is passed to the
# host as Ashlar__Security__ApiKey when this script starts it, and used for the mutating call either way.
# Defaults to a per-run random value when the script starts the host itself.

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

HOST_PROJECT="commercial/src/Ashlar.Commercial.Fleet.Host/Ashlar.Commercial.Fleet.Host.csproj"
BASE_URL="${FLEET_HOST_URL:-http://127.0.0.1:18090}"
API_KEY="${FLEET_HOST_API_KEY:-}"
PEER_ID="${FLEET_HOST_SMOKE_PEER_ID:-commercial-fleet-smoke-peer}"
STARTED_HOST=0
HOST_PID=""

cleanup() {
  if [[ "$STARTED_HOST" == "1" && -n "$HOST_PID" ]]; then
    kill "$HOST_PID" >/dev/null 2>&1 || true
    wait "$HOST_PID" 2>/dev/null || true
  fi
}
trap cleanup EXIT

if ! curl -fsS "${BASE_URL}/health" >/dev/null 2>&1; then
  if [[ -z "$API_KEY" ]]; then
    API_KEY="fleet-smoke-$(od -An -N16 -tx1 /dev/urandom | tr -d ' \n')"
  fi
  echo "Starting commercial fleet host on ${BASE_URL} ..."
  ASPNETCORE_URLS="${BASE_URL}" Ashlar__Security__ApiKey="${API_KEY}" dotnet run --project "$HOST_PROJECT" --no-launch-profile >/tmp/commercial-fleet-host-smoke.log 2>&1 &
  HOST_PID=$!
  STARTED_HOST=1
  for _ in $(seq 1 60); do
    if curl -fsS "${BASE_URL}/health" >/dev/null 2>&1; then
      break
    fi
    sleep 1
  done
fi

if [[ -z "$API_KEY" ]]; then
  echo "Set FLEET_HOST_API_KEY to the Ashlar__Security__ApiKey the running host was started with (no default key is shipped)." >&2
  exit 2
fi

curl -fsS "${BASE_URL}/health" >/dev/null
echo "Health — OK"

LIST_JSON="$(curl -fsS "${BASE_URL}/api/mesh/fleet/nodes")"
python3 -c 'import json,sys; json.loads(sys.argv[1]); print("Fleet list JSON — OK")' "$LIST_JSON"

curl -fsS -X POST \
  -H "Content-Type: application/json" \
  -H "X-Ashlar-Api-Key: ${API_KEY}" \
  -d "{\"peerId\":\"${PEER_ID}\",\"apiBaseUrl\":\"http://127.0.0.1:8080\",\"trustTier\":\"Trusted\"}" \
  "${BASE_URL}/api/mesh/fleet/nodes" >/dev/null
echo "Fleet register — OK"

echo "Commercial fleet host smoke passed (${BASE_URL})"
