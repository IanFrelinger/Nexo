#!/usr/bin/env bash
# Entitlements + auth tiers on the mesh-lab worker: CopilotScoped key, mesh deny, hourly copilot quota.
#
#   ./scripts/mesh-lab-verify-entitlements.sh .env.mesh-lab
#
# Requires: worker profile running; env must define Nexo__Security__ApiKey and Nexo__Security__CopilotScopedApiKey.

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

COMPOSE_FILE="${COMPOSE_FILE:-docker-compose.mesh-lab.yml}"
COMPOSE_ENV_FILE="${1:-${COMPOSE_ENV_FILE:-.env.mesh-lab}}"

if [[ ! -f "$COMPOSE_ENV_FILE" ]]; then
  echo "Missing env file: $COMPOSE_ENV_FILE" >&2
  exit 1
fi

source_env_kv() {
  local key=$1
  grep -E "^${key}=" "$COMPOSE_ENV_FILE" 2>/dev/null | head -1 | cut -d= -f2- | tr -d '\r' || true
}

API_KEY="$(source_env_kv Nexo__Security__ApiKey)"
COPILOT_KEY="$(source_env_kv Nexo__Security__CopilotScopedApiKey)"
PEER_A_HOST="${MESH_LAB_PEER_A_HOST:-127.0.0.1:18081}"
DIRECTOR_HTTP="http://${PEER_A_HOST}"
WORKER_PUBLISH_FALLBACK="$(source_env_kv MESH_LAB_WORKER_PUBLISH)"
[[ -n "$WORKER_PUBLISH_FALLBACK" ]] || WORKER_PUBLISH_FALLBACK="127.0.0.1:18083"
QUOTA_MAX="$(source_env_kv Nexo__Entitlements__MaxCopilotSubmissionsPerHour)"
[[ -n "$QUOTA_MAX" ]] || QUOTA_MAX="2"

if [[ -z "$COPILOT_KEY" ]]; then
  echo "(Skipping entitlements verify: Nexo__Security__CopilotScopedApiKey not set in env file)"
  exit 0
fi

compose_workers() {
  docker compose --profile workers -f "$COMPOSE_FILE" --env-file "$COMPOSE_ENV_FILE" "$@"
}

if ! compose_workers ps -q worker 2>/dev/null | grep -q .; then
  echo "(Skipping entitlements verify: workers profile not running)"
  exit 0
fi

resolve_worker_http() {
  local published="" wid ip peer_net nets pick
  wid="$(compose_workers ps -q worker | head -1 | tr -d '\r\n[:space:]')"
  if [[ -n "$wid" ]]; then
    published="$(docker port "$wid" 8080/tcp 2>/dev/null || docker port "$wid" 8080 2>/dev/null || true)"
    published="$(echo "$published" | head -1 | tr -d '\r\n[:space:]')"
    published="${published//0.0.0.0/127.0.0.1}"
  fi
  if [[ -z "$published" ]]; then
    published="$(compose_workers port worker 8080 2>/dev/null | tr -d '\r\n[:space:]')" || true
    published="${published//0.0.0.0/127.0.0.1}"
  fi
  if [[ -z "$published" ]] && curl -fsS -m 3 "http://${WORKER_PUBLISH_FALLBACK}/health" >/dev/null 2>&1; then
    published="${WORKER_PUBLISH_FALLBACK}"
  fi
  if [[ -n "$published" ]]; then
    echo "http://${published}"
    return 0
  fi
  cid="$(compose_workers ps -q peer-a | head -1 | tr -d '\r\n[:space:]')"
  [[ -n "$cid" ]] || return 1
  nets="$(docker inspect "$cid" --format '{{range $k,$v := .NetworkSettings.Networks}}{{printf "%s\n" $k}}{{end}}')"
  pick="$(echo "$nets" | grep mesh_lab | head -1 | tr -d '\r\n')"
  [[ -n "$pick" ]] || pick="$(echo "$nets" | head -1 | tr -d '\r\n')"
  wid="$(compose_workers ps -q worker | head -1 | tr -d '\r\n[:space:]')"
  ip="$(docker inspect "$wid" | python3 -c '
import json, re, sys
peer = sys.argv[1]
ns = json.load(sys.stdin)[0]["NetworkSettings"]["Networks"]
cfg = ns.get(peer) or next(iter(ns.values()), {})
for raw in (cfg.get("IPAddress"), cfg.get("IPv4Address")):
    if not raw: continue
    s = str(raw).split("/", 1)[0].strip()
    if re.fullmatch(r"\d+\.\d+\.\d+\.\d+", s):
        print(s); raise SystemExit(0)
raise SystemExit(1)
' "$pick" 2>/dev/null || true)"
  if [[ -n "$ip" ]]; then
    echo "http://${ip}:8080"
    return 0
  fi
  return 1
}

WORKER_HTTP="$(resolve_worker_http || true)"
if [[ -z "$WORKER_HTTP" ]]; then
  echo "Could not resolve worker HTTP endpoint for entitlements checks" >&2
  exit 1
fi

echo "== Mesh entitlements: worker ${WORKER_HTTP} (quota/hour=${QUOTA_MAX}) =="

http_code() {
  curl -s -o /dev/null -w '%{http_code}' -m 25 "$@"
}

echo "-- CopilotScoped key: GET /api/status (allowed) --"
SC_STATUS="$(http_code -H "X-Nexo-Api-Key: ${COPILOT_KEY}" "${WORKER_HTTP}/api/status")"
if [[ "$SC_STATUS" != "200" ]]; then
  echo "Expected 200 for copilot-scoped GET /api/status, got ${SC_STATUS}" >&2
  exit 1
fi

echo "-- CopilotScoped key: POST /api/mesh/tasks on director (forbidden) --"
SC_MESH="$(http_code -X POST \
  -H "Content-Type: application/json" \
  -H "X-Nexo-Api-Key: ${COPILOT_KEY}" \
  -d '{"name":"mesh-lab-entitlements-deny","steps":1}' \
  "${DIRECTOR_HTTP}/api/mesh/tasks")"
if [[ "$SC_MESH" != "403" ]]; then
  echo "Expected 403 for copilot-scoped POST /api/mesh/tasks on director, got ${SC_MESH}" >&2
  exit 1
fi

echo "-- Full API key: POST /api/mesh/tasks on director (allowed) --"
FULL_MESH="$(http_code -X POST \
  -H "Content-Type: application/json" \
  -H "X-Nexo-Api-Key: ${API_KEY}" \
  -d '{"name":"mesh-lab-entitlements-allow","steps":1}' \
  "${DIRECTOR_HTTP}/api/mesh/tasks")"
if [[ "$FULL_MESH" != "200" ]]; then
  echo "Expected 200 for full key POST /api/mesh/tasks on director, got ${FULL_MESH}" >&2
  exit 1
fi

echo "-- CopilotScoped: hourly submission quota (tenant mesh-lab-quota) --"
TENANT_HDR=(-H "X-Nexo-Tenant: mesh-lab-quota")
COPILOT_POST=( -X POST -H "Content-Type: application/json" -H "X-Nexo-Api-Key: ${COPILOT_KEY}" "${TENANT_HDR[@]}"
  -d '{"task":"mesh-lab entitlements quota probe","auditCount":1}' )
ALLOWED=0
DENIED=false
for i in 1 2 3 4; do
  CODE="$(http_code "${COPILOT_POST[@]}" "${WORKER_HTTP}/api/copilot/task")"
  if [[ "$CODE" == "429" ]]; then
    DENIED=true
    break
  fi
  if [[ "$CODE" == "401" || "$CODE" == "403" ]]; then
    echo "Unexpected auth failure on copilot POST (attempt ${i}): ${CODE}" >&2
    exit 1
  fi
  ALLOWED=$((ALLOWED + 1))
done

if [[ "$QUOTA_MAX" -gt 0 ]]; then
  if [[ "$ALLOWED" -lt "$QUOTA_MAX" ]]; then
    echo "Expected at least ${QUOTA_MAX} copilot submissions before 429, only ${ALLOWED} non-429 responses" >&2
    exit 1
  fi
  if [[ "$DENIED" != true ]]; then
    echo "Expected HTTP 429 after ${QUOTA_MAX} copilot submissions/hour, quota not enforced" >&2
    exit 1
  fi
  echo "Copilot hourly quota enforced after ${ALLOWED} submission(s) — OK"
else
  echo "Quota disabled (MaxCopilotSubmissionsPerHour=0); skipped 429 assertion"
fi

echo "== Mesh lab verify-entitlements: OK =="
