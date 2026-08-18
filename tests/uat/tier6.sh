#!/usr/bin/env bash
# Nexo UAT — Tier 6 (MCP / A2A protocol ingress).
#
# Tier 7 pins that these surfaces are OFF by default. This tier asks the harder question: when an
# operator turns them on, do they behave the way SECURITY.md says? The claim that matters is not
# "the flag works" but "turning the flag on does not quietly open a door" -- protocol ingress stays
# credentialed on EVERY verb, including GETs that the default MutatingApi scope would otherwise leave
# open, and agent-card discovery is anonymous only when an operator opts into it.
set -uo pipefail

SRC="${UAT_REPO_DIR:-$PWD}"
OUT="${UAT_OUT:-${WORK:-/work}/out}"
mkdir -p "$OUT"
: >"$OUT/results-tier6.tsv"

PASS=0; FAIL=0
result() {
  local tier="$1" id="$2" verdict="$3"; shift 3
  printf 'RESULT\t%s\t%s\t%s\t%s\n' "$tier" "$id" "$verdict" "$*" | tee -a "$OUT/results-tier6.tsv"
  if [ "$verdict" = PASS ]; then PASS=$((PASS+1)); else FAIL=$((FAIL+1)); fi
}
say() { printf '\n=== %s ===\n' "$*"; }
cd "$SRC" || exit 1

unset ASPNETCORE_HTTP_PORTS
export NEXO_ALLOW_MOCK=1
API=""; API_PID=""

pkill -f 'Nexo\.API' 2>/dev/null
PORT_FREE=no
for _ in $(seq 1 30); do
  curl -s -o /dev/null --max-time 2 "http://localhost:5000/health" 2>/dev/null || { PORT_FREE=yes; break; }
  sleep 1
done
if [ "$PORT_FREE" != yes ]; then
  echo "ABORT: something is already serving http://localhost:5000; these checks start the API themselves." >&2
  exit 2
fi

start_api() {
  local tag="$1"; shift
  local log="$OUT/api-$tag.log"
  ( env "$@" dotnet run --project application/src/Nexo.API -f net10.0 >"$log" 2>&1 ) &
  API_PID=$!
  for _ in $(seq 1 90); do
    grep -q 'Now listening on:' "$log" 2>/dev/null && break
    kill -0 $API_PID 2>/dev/null || break
    sleep 2
  done
  grep -q 'Now listening on:' "$log" 2>/dev/null || { API=""; return 1; }
  API=$(grep -m1 -oE 'Now listening on: *http://[^ ]+' "$log" \
        | sed 's/Now listening on: *//; s#0\.0\.0\.0#localhost#; s#\[::\]#localhost#')
  return 0
}
stop_api() {
  [ -n "$API_PID" ] && { kill $API_PID 2>/dev/null; wait $API_PID 2>/dev/null; }
  pkill -f 'Nexo\.API' 2>/dev/null
  API_PID=""
  for _ in $(seq 1 30); do
    curl -s -o /dev/null --max-time 2 "http://localhost:5000/health" 2>/dev/null || return 0
    sleep 1
  done
}
code() { curl -s -o /dev/null -w '%{http_code}' --max-time 60 "$@"; }

say "6.1 the MCP flag actually maps the surface (tier 7 pins that it is 404 while off)"
if start_api mcp-on Nexo__Mcp__Server__Enabled=true; then
  M=$(code -X POST "$API/api/mcp" -H 'Content-Type: application/json' -d '{}')
  if [ "$M" != "404" ]; then
    result 6 mcp-flag-maps PASS "/api/mcp=$M with Nexo:Mcp:Server:Enabled=true (was 404 while off)"
  else
    result 6 mcp-flag-maps FAIL "/api/mcp still 404 with the server enabled; the flag does not map the surface"
  fi
else
  result 6 mcp-flag-maps FAIL "API did not start with MCP enabled: $(tail -3 "$OUT/api-mcp-on.log" | tr '\n' ' ')"
fi
stop_api

say "6.2 enabling a protocol surface does not open a door: every verb stays credentialed"
# SECURITY.md: MutatingApi "credentials POST/PUT/PATCH/DELETE under /api AND EVERY VERB on /api/mcp and
# /api/a2a". A GET to an ordinary /api route is open under this scope, so if the protocol paths were
# treated as ordinary the GET below would be 200 -- an anonymous read of a surface an operator just
# turned on, under the DEFAULT scope. That is the regression this check exists for.
if start_api mcp-auth \
     Nexo__Mcp__Server__Enabled=true \
     Nexo__Security__AuthorizationMode=ApiKey \
     Nexo__Security__ApiKey=uat-tier6-key \
     Nexo__Security__AuthorizationScope=MutatingApi; then
  OPEN=$(code "$API/api/trust/status")
  MG=$(code "$API/api/mcp")
  MP=$(code -X POST "$API/api/mcp" -H 'Content-Type: application/json' -d '{}')
  if [ "$OPEN" = "200" ] && [ "$MG" = "401" ] && [ "$MP" = "401" ]; then
    result 6 protocol-credentialed-every-verb PASS "ordinary GET=$OPEN stays open, but /api/mcp GET=$MG and POST=$MP under the default MutatingApi scope"
  else
    result 6 protocol-credentialed-every-verb FAIL "SECURITY: expected ordinary GET=200 with protocol GET/POST=401; got open=$OPEN mcpGET=$MG mcpPOST=$MP"
  fi
else
  result 6 protocol-credentialed-every-verb FAIL "API did not start with MCP + ApiKey: $(tail -3 "$OUT/api-mcp-auth.log" | tr '\n' ' ')"
fi
stop_api

# NOT covered here, deliberately: agent-card anonymity and A2A ingress with a live agent. A2A refuses
# to start until an EXPOSED AGENT IS ACTUALLY REGISTERED, which an out-of-process run of the shipped
# host cannot do -- naming an unregistered id is still refused (verified). That surface is covered
# in-repo by McpA2AProtocolIngressProdStyleTests, which registers a test agent. Recorded rather than
# quietly dropped: a black-box tier that pretends to cover it would be worse than one that says it
# does not.

say "6.3 enabling A2A without a public base URL refuses to start, and names the setting"
# Found by getting this wrong: an agent card carries a base URL that REMOTE agents will call, so a
# card built from a guessed or empty URL would publish a wrong address to the ecosystem. The host
# refuses instead, which is the behaviour to keep -- and the message names the exact setting.
if start_api a2a-nourl Nexo__A2A__Server__Enabled=true; then
  result 6 a2a-requires-public-url FAIL "SECURITY/CORRECTNESS: A2A started with no PublicBaseUrl; agent cards would advertise a guess"
else
  if grep -qi 'PublicBaseUrl' "$OUT/api-a2a-nourl.log"; then
    result 6 a2a-requires-public-url PASS "refused, naming the setting: $(grep -m1 -o 'PublicBaseUrl[^\"]\{0,80\}' "$OUT/api-a2a-nourl.log")"
  else
    result 6 a2a-requires-public-url FAIL "refused, but without naming PublicBaseUrl: $(tail -2 "$OUT/api-a2a-nourl.log" | tr '\n' ' ')"
  fi
fi
stop_api

say "6.4 enabling A2A with a base URL but no exposed agents also refuses"
# The second half of the same discipline: a reachable A2A server that exposes nothing by default.
# An allow-list that defaulted to "everything" would publish every registered agent to the ecosystem
# the moment an operator flipped one flag.
if start_api a2a-noagents \
     Nexo__A2A__Server__Enabled=true \
     Nexo__A2A__Server__PublicBaseUrl=http://localhost:5000; then
  result 6 a2a-requires-exposed-agents FAIL "SECURITY: A2A started with no ExposedAgentIds; exposure defaults must not be implicit"
else
  if grep -qi 'ExposedAgentIds\|no agents are exposed' "$OUT/api-a2a-noagents.log"; then
    result 6 a2a-requires-exposed-agents PASS "refused: $(grep -m1 -o 'A2A server is enabled but[^\"]\{0,90\}' "$OUT/api-a2a-noagents.log")"
  else
    result 6 a2a-requires-exposed-agents FAIL "refused without naming the allow-list: $(tail -2 "$OUT/api-a2a-noagents.log" | tr '\n' ' ')"
  fi
fi
stop_api

say "6.5 an enabled MCP server exposes no tools unless an operator names them"
# ExposedToolIds is an explicit allow-list. Enabled with an empty list must mean "no tools", not "all".
if grep -qE 'ExposedToolIds' src/Nexo.Mcp.Server/NexoMcpServerOptions.cs 2>/dev/null; then
  if grep -qiE 'fail-closed|defaults? to (false|empty)|no tools' src/Nexo.Mcp.Server/NexoMcpServerOptions.cs 2>/dev/null; then
    result 6 exposed-tools-allowlist PASS "ExposedToolIds is an explicit allow-list on a fail-closed options type"
  else
    result 6 exposed-tools-allowlist FAIL "ExposedToolIds exists but the options type does not document a fail-closed default"
  fi
else
  result 6 exposed-tools-allowlist FAIL "no ExposedToolIds allow-list on the MCP server options"
fi

say "summary"
printf 'PASS=%d FAIL=%d\n' "$PASS" "$FAIL" | tee "$OUT/summary-tier6.txt"
[ "$FAIL" -eq 0 ]
