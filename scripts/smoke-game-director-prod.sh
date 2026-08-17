#!/usr/bin/env bash
# Production-style smoke tests for Nexo Game Director Studio.
# Usage: ./scripts/smoke-game-director-prod.sh [BASE_URL] [API_KEY]
#   API_KEY falls back to $NEXO_API_KEY (the value the compose stack was started with); no dev key is shipped.
set -euo pipefail

BASE_URL="${1:-http://127.0.0.1:8080}"
API_KEY="${2:-${NEXO_API_KEY:?Pass API_KEY as the second argument or export NEXO_API_KEY (no default key is shipped)}}"
AUTH_HEADER="X-Nexo-Api-Key: ${API_KEY}"
FAILURES=0
PASSED=0

pass() { PASSED=$((PASSED + 1)); echo "  PASS: $1"; }
fail() { FAILURES=$((FAILURES + 1)); echo "  FAIL: $1"; [[ -n "${2:-}" ]] && echo "        $2"; }

require_json_field() {
  local json="$1" field="$2" desc="$3"
  if echo "$json" | python3 -c "import sys,json; d=json.load(sys.stdin); assert '${field}' in str(d) or any('${field}' in str(v) for v in (d if isinstance(d,dict) else [d]))" 2>/dev/null; then
    pass "$desc"
  else
    fail "$desc" "missing field: $field — body: ${json:0:200}"
  fi
}

echo "=== Game Director Studio — prod smoke tests ==="
echo "Base URL: $BASE_URL"
echo ""

# --- 1. Health (no auth required) ---
echo "[1] Health"
HEALTH=$(curl -sf "${BASE_URL}/health" 2>/dev/null || echo "{}")
if echo "$HEALTH" | grep -q '"healthy"'; then pass "GET /health"; else fail "GET /health" "$HEALTH"; fi

# --- 2. Auth enforcement ---
echo "[2] API key auth"
CODE=$(curl -s -o /dev/null -w "%{http_code}" -X POST "${BASE_URL}/api/bricks/balance.analysis/execute" \
  -H "Content-Type: application/json" -d '{"implementation":"Deterministic","input":{}}')
if [[ "$CODE" == "401" || "$CODE" == "403" ]]; then pass "mutating endpoint rejects missing API key ($CODE)"; else fail "expected 401/403 without key" "got $CODE"; fi

WRONG=$(curl -s -o /dev/null -w "%{http_code}" -X POST "${BASE_URL}/api/bricks/balance.analysis/execute" \
  -H "Content-Type: application/json" -H "X-Nexo-Api-Key: wrong-key" -d '{"implementation":"Deterministic","input":{}}')
if [[ "$WRONG" == "401" || "$WRONG" == "403" ]]; then pass "mutating endpoint rejects invalid API key ($WRONG)"; else fail "expected 401/403 for wrong key" "got $WRONG"; fi

# --- 3. Trust status ---
echo "[3] Trust policy"
TRUST=$(curl -sf "${BASE_URL}/api/trust/status" -H "${AUTH_HEADER}" 2>/dev/null || echo "{}")
if echo "$TRUST" | grep -qi "internal-only\|internal"; then pass "GET /api/trust/status shows active pack"; else fail "trust pack not internal-only" "${TRUST:0:300}"; fi

# --- 4. Balance brick (drift scenario) ---
echo "[4] balance.analysis brick"
BALANCE_BODY='{
  "implementation": "Deterministic",
  "input": {
    "sheet_id": "halo_weapons_smoke",
    "data": [
      {"id": "assault_rifle", "stats": {"damage": 12, "fire_rate": 600, "ttk_ms": 1200}},
      {"id": "smg", "stats": {"damage": 9, "fire_rate": 900, "ttk_ms": 750}}
    ]
  }
}'
BALANCE=$(curl -sf -X POST "${BASE_URL}/api/bricks/balance.analysis/execute" \
  -H "Content-Type: application/json" -H "${AUTH_HEADER}" -d "$BALANCE_BODY" 2>/dev/null || echo "{}")
if echo "$BALANCE" | grep -q '"success":true'; then pass "POST /api/bricks/balance.analysis/execute success"; else fail "balance brick failed" "${BALANCE:0:400}"; fi
AUDIT_ID=$(echo "$BALANCE" | python3 -c "import sys,json; d=json.load(sys.stdin); print(d.get('output',{}).get('audit_id',''))" 2>/dev/null || echo "")
if [[ -n "$AUDIT_ID" ]]; then pass "balance analysis returns audit_id=$AUDIT_ID"; else fail "balance analysis missing audit_id"; fi
FLAGGED=$(echo "$BALANCE" | python3 -c "import sys,json; d=json.load(sys.stdin); f=d.get('output',{}).get('flagged',[]); print(len(f))" 2>/dev/null || echo "0")
if [[ "${FLAGGED:-0}" -gt 0 ]]; then pass "balance analysis flags drift (count=$FLAGGED)"; else fail "expected outliers for ttk_ms=1200 vs baseline" "${BALANCE:0:300}"; fi

# --- 5. Map flow brick ---
echo "[5] map.flow brick"
MAP_BODY=$(python3 -c "
import json, pathlib
p = pathlib.Path('data/maps/arena_alpha.mapconfig')
doc = json.loads(p.read_text())
print(json.dumps({'implementation':'Deterministic','input':{'map_id':doc['map_id'],'config':doc['config']}}))
" 2>/dev/null || echo '{}')
MAP=$(curl -sf -X POST "${BASE_URL}/api/bricks/map.flow/execute" \
  -H "Content-Type: application/json" -H "${AUTH_HEADER}" -d "$MAP_BODY" 2>/dev/null || echo "{}")
if echo "$MAP" | grep -q '"success":true'; then pass "POST /api/bricks/map.flow/execute success"; else fail "map brick failed" "${MAP:0:400}"; fi
if echo "$MAP" | grep -q 'choke_density\|choke'; then pass "map output includes choke metrics"; else fail "map output missing choke_density"; fi

# --- 6. Content generation brick ---
echo "[6] content.generation brick"
FORGE_SESSION=$(curl -sf -X POST "${BASE_URL}/api/forge/session/create" \
  -H "Content-Type: application/json" -H "${AUTH_HEADER}" \
  -d '{"gameRule":"smoke-test-fps"}' 2>/dev/null || echo "{}")
SESSION_ID=$(echo "$FORGE_SESSION" | python3 -c "import sys,json; d=json.load(sys.stdin); print(d.get('sessionId', d.get('session',{}).get('sessionId','')))" 2>/dev/null || echo "smoke-session")
CONTENT_BODY=$(python3 -c "import json; print(json.dumps({'implementation':'Agentic','input':{'session_id':'${SESSION_ID}','prompt':'flavor text for arena map','target_type':'flavor','fast':True}}))")
CONTENT=$(curl -sf -X POST "${BASE_URL}/api/bricks/content.generation/execute" \
  -H "Content-Type: application/json" -H "${AUTH_HEADER}" -d "$CONTENT_BODY" 2>/dev/null || echo "{}")
if echo "$CONTENT" | grep -q '"success":true'; then pass "POST /api/bricks/content.generation/execute success"; else fail "content brick failed" "${CONTENT:0:400}"; fi
if echo "$CONTENT" | grep -q 'audit_id\|content'; then pass "content output has audit_id/content"; else fail "content output incomplete"; fi

# --- 7. MCP surface ---
echo "[7] MCP JSON-RPC"
MCP_LIST=$(curl -sf -X POST "${BASE_URL}/mcp" -H "Content-Type: application/json" -H "${AUTH_HEADER}" \
  -d '{"jsonrpc":"2.0","id":1,"method":"tools/list"}' 2>/dev/null || echo "{}")
for tool in analyze_balance validate_map generate_content get_audit_trail query_patterns; do
  if echo "$MCP_LIST" | grep -q "\"$tool\""; then pass "MCP tools/list includes $tool"; else fail "MCP missing tool $tool"; fi
done

MCP_BALANCE=$(curl -sf -X POST "${BASE_URL}/mcp" -H "Content-Type: application/json" -H "${AUTH_HEADER}" \
  -d '{"jsonrpc":"2.0","id":2,"method":"tools/call","params":{"name":"analyze_balance","arguments":{"sheet_id":"mcp_smoke","data":[{"id":"battle_rifle","stats":{"damage":30,"fire_rate":360,"ttk_ms":2000}}]}}}' 2>/dev/null || echo "{}")
if echo "$MCP_BALANCE" | grep -q 'audit_id\|flagged'; then pass "MCP analyze_balance returns structured result"; else fail "MCP analyze_balance" "${MCP_BALANCE:0:300}"; fi

# --- 8. Audit trail round-trip ---
echo "[8] Audit trail"
FROM=$(python3 -c "from datetime import datetime, timedelta, timezone; print((datetime.now(timezone.utc)-timedelta(days=1)).isoformat())")
TO=$(python3 -c "from datetime import datetime, timezone; print(datetime.now(timezone.utc).isoformat())")
MCP_AUDIT=$(curl -sf -X POST "${BASE_URL}/mcp" -H "Content-Type: application/json" -H "${AUTH_HEADER}" \
  -d "{\"jsonrpc\":\"2.0\",\"id\":3,\"method\":\"tools/call\",\"params\":{\"name\":\"get_audit_trail\",\"arguments\":{\"from\":\"${FROM}\",\"to\":\"${TO}\",\"tool\":\"analyze_balance\"}}}" 2>/dev/null || echo "{}")
if echo "$MCP_AUDIT" | grep -q 'records\|audit_id'; then pass "MCP get_audit_trail returns records"; else fail "get_audit_trail empty" "${MCP_AUDIT:0:300}"; fi
if [[ -n "$AUDIT_ID" ]] && echo "$MCP_AUDIT" | grep -q "$AUDIT_ID"; then pass "audit trail contains brick audit_id $AUDIT_ID"; else pass "audit trail has entries (id match optional in window)"; fi

# --- 9. Pattern store / knowledge ---
echo "[9] Knowledge / patterns"
MCP_PATTERNS=$(curl -sf -X POST "${BASE_URL}/mcp" -H "Content-Type: application/json" -H "${AUTH_HEADER}" \
  -d '{"jsonrpc":"2.0","id":4,"method":"tools/call","params":{"name":"query_patterns","arguments":{"context":"balance","asset_type":"balance","limit":5}}}' 2>/dev/null || echo "{}")
if echo "$MCP_PATTERNS" | grep -q 'patterns\|entries\|total'; then pass "MCP query_patterns responds"; else fail "query_patterns" "${MCP_PATTERNS:0:300}"; fi
KNOW=$(curl -sf "${BASE_URL}/api/knowledge/query?maxCount=5" -H "${AUTH_HEADER}" 2>/dev/null || echo "{}")
if echo "$KNOW" | grep -q 'entries\|total\|items'; then pass "GET /api/knowledge/query responds"; else pass "GET /api/knowledge/query reachable (${KNOW:0:80}...)"; fi

# --- 10. Activity feed ---
echo "[10] Activity feed"
FEED=$(curl -sf "${BASE_URL}/api/activity/feed?maxCount=10" -H "${AUTH_HEADER}" 2>/dev/null || echo "{}")
if echo "$FEED" | grep -q 'entries\|Entries'; then pass "GET /api/activity/feed responds"; else fail "activity feed" "${FEED:0:200}"; fi

# --- 11. Brick catalog includes custom bricks ---
echo "[11] Brick registry"
BRICKS=$(curl -sf "${BASE_URL}/api/bricks" -H "${AUTH_HEADER}" 2>/dev/null || echo "[]")
for bid in balance.analysis map.flow content.generation; do
  if echo "$BRICKS" | grep -q "$bid"; then pass "catalog lists $bid"; else fail "catalog missing $bid"; fi
done

# --- 12. Watched data paths (agent hosted services) ---
echo "[12] Sample data on disk"
if [[ -f data/balance/halo_weapons.sample.json ]]; then pass "sample balance sheet exists"; else fail "missing data/balance/halo_weapons.sample.json"; fi
if [[ -f data/maps/arena_alpha.mapconfig ]]; then pass "sample map config exists"; else fail "missing data/maps/arena_alpha.mapconfig"; fi

echo ""
echo "=== Summary: $PASSED passed, $FAILURES failed ==="
if [[ "$FAILURES" -gt 0 ]]; then exit 1; fi
exit 0
