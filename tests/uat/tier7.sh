#!/usr/bin/env bash
# Nexo UAT — Tier 7 (security negatives).
#
# SECURITY.md is unusually specific about the default posture, so this tier tests what that page
# CLAIMS rather than what a reviewer might wish were true. Each check cites the claim it pins.
#
# One claim is deliberately NOT tested as a boundary: SECURITY.md says tenant/org/user headers are
# "client-asserted" and must be trusted only behind auth or an authenticating proxy. Asserting tenant
# isolation would be inventing a guarantee the project explicitly declines to make, and a green on an
# invented requirement is worse than no check.
set -uo pipefail

SRC="${UAT_REPO_DIR:-$PWD}"
OUT="${UAT_OUT:-${WORK:-/work}/out}"
mkdir -p "$OUT"
: >"$OUT/results-tier7.tsv"

PASS=0; FAIL=0
result() {
  local tier="$1" id="$2" verdict="$3"; shift 3
  printf 'RESULT\t%s\t%s\t%s\t%s\n' "$tier" "$id" "$verdict" "$*" | tee -a "$OUT/results-tier7.tsv"
  if [ "$verdict" = PASS ]; then PASS=$((PASS+1)); else FAIL=$((FAIL+1)); fi
}
say() { printf '\n=== %s ===\n' "$*"; }
cd "$SRC" || exit 1

unset ASPNETCORE_HTTP_PORTS
export NEXO_ALLOW_MOCK=1
API=""
API_PID=""

# Leftovers from an earlier run hold :5000, and every phase below then reports "API did not start" --
# which reads exactly like a product failure. Refuse to run on a dirty port rather than emit results
# that blame the product for the environment.
pkill -f 'Nexo\.API' 2>/dev/null
PORT_FREE=no
for _ in $(seq 1 30); do
  curl -s -o /dev/null --max-time 2 "http://localhost:5000/health" 2>/dev/null || { PORT_FREE=yes; break; }
  sleep 1
done
if [ "$PORT_FREE" != yes ]; then
  echo "ABORT: something is already serving http://localhost:5000. Stop it and re-run;" >&2
  echo "       these checks each start the API themselves and would otherwise blame the product." >&2
  exit 2
fi

# Start the API under a named log, waiting for the listen line. Returns 1 if it never listened, which
# for several of these checks is the PASSING outcome -- refusing to start is the claim.
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
  if grep -q 'Now listening on:' "$log" 2>/dev/null; then
    API=$(grep -m1 -oE 'Now listening on: *http://[^ ]+' "$log" \
          | sed 's/Now listening on: *//; s#0\.0\.0\.0#localhost#; s#\[::\]#localhost#')
    return 0
  fi
  API=""
  return 1
}
stop_api() {
  [ -n "$API_PID" ] && { kill $API_PID 2>/dev/null; wait $API_PID 2>/dev/null; }
  pkill -f 'Nexo\.API' 2>/dev/null
  API_PID=""
  # This tier starts the API once per configuration, so the port must actually be free before the next
  # one binds. A fixed sleep is not enough -- release took several seconds in practice, and every later
  # phase then failed with "address already in use", which reads exactly like a product failure.
  for _ in $(seq 1 30); do
    curl -s -o /dev/null --max-time 2 "http://localhost:5000/health" 2>/dev/null || return 0
    sleep 1
  done
}
code() { curl -s -o /dev/null -w '%{http_code}' --max-time 60 "$@"; }

# ---------------------------------------------------------------- default posture
say "7.1/7.2 default posture: remote execution unmapped, protocol surfaces off (SECURITY.md 'in-scope surfaces')"
if start_api default; then
  B=$(code -X POST "$API/api/execution/build" -H 'Content-Type: application/json' -d '{}')
  R=$(code -X POST "$API/api/execution/run"   -H 'Content-Type: application/json' -d '{}')
  if [ "$B" = "404" ] && [ "$R" = "404" ]; then
    result 7 remote-execution-unmapped PASS "build=$B run=$R -- 'mapped only when Nexo:Execution:ServeRemoteExecution=true (404 otherwise)'"
  else
    result 7 remote-execution-unmapped FAIL "SECURITY: SECURITY.md says 404 by default; build=$B run=$R"
  fi

  M=$(code -X POST "$API/api/mcp" -H 'Content-Type: application/json' -d '{}')
  if [ "$M" = "404" ]; then
    result 7 protocol-surfaces-off PASS "/api/mcp=$M -- 'the MCP / A2A protocol surfaces are feature-flagged off'"
  else
    result 7 protocol-surfaces-off FAIL "SECURITY: /api/mcp answered $M with no flag set; SECURITY.md says feature-flagged off"
  fi
else
  result 7 remote-execution-unmapped FAIL "API did not start in the default posture"
fi
stop_api

# ---------------------------------------------------------------- opted in, but unauthenticated
say "7.3 remote execution opted in with AuthorizationMode=None must 403, not serve (SECURITY.md 'opt-in')"
if start_api exec-none Nexo__Execution__ServeRemoteExecution=true; then
  B=$(code -X POST "$API/api/execution/build" -H 'Content-Type: application/json' -d '{}')
  if [ "$B" = "403" ]; then
    result 7 remote-execution-refuses-anonymous PASS "build=$B -- 'refuse AuthorizationMode=None with 403 even when opted in'"
  else
    result 7 remote-execution-refuses-anonymous FAIL "SECURITY: opted-in remote execution answered $B under AuthorizationMode=None; SECURITY.md says 403"
  fi
else
  result 7 remote-execution-refuses-anonymous FAIL "API did not start with ServeRemoteExecution=true"
fi
stop_api

# ---------------------------------------------------------------- scope semantics
say "7.4/7.5 AuthorizationScope=MutatingApi credentials writes but not reads (SECURITY.md 'what needs auth')"
if start_api scope-mutating \
     Nexo__Security__AuthorizationMode=ApiKey \
     Nexo__Security__ApiKey=uat-tier7-key \
     Nexo__Security__AuthorizationScope=MutatingApi; then
  # An ordinary read stays open under MutatingApi; a POST does not.
  G=$(code "$API/api/trust/status")
  P=$(code -X POST "$API/api/copilot/task" -H 'Content-Type: application/json' -d '{"task":"probe","auditCount":1}')
  if [ "$G" = "200" ] && [ "$P" = "401" ]; then
    result 7 scope-mutating-only PASS "GET /api/trust/status=$G, POST=$P -- MutatingApi credentials writes, not ordinary reads"
  else
    result 7 scope-mutating-only FAIL "expected GET=200 POST=401 under MutatingApi; got GET=$G POST=$P"
  fi

  # ...but task history is credentialed under EITHER scope, because it carries the prompts and outputs
  # of past runs. RequireAuthForCopilotReadApis defaults true and was undocumented until this tier ran;
  # a reader of the scope sentence alone would have expected 200 here.
  H=$(code "$API/api/copilot/tasks")
  if [ "$H" = "401" ]; then
    result 7 copilot-history-credentialed PASS "GET /api/copilot/tasks=$H under MutatingApi (RequireAuthForCopilotReadApis defaults true)"
  else
    result 7 copilot-history-credentialed FAIL "SECURITY: task history readable without a key (=$H); RequireAuthForCopilotReadApis should default true"
  fi

  W=$(code -X POST "$API/api/copilot/task" -H 'Content-Type: application/json' \
        -H 'X-Nexo-Api-Key: not-the-key' -d '{"task":"probe","auditCount":1}')
  OK=$(code -X POST "$API/api/copilot/task" -H 'Content-Type: application/json' \
        -H 'X-Nexo-Api-Key: uat-tier7-key' -d '{"task":"probe","auditCount":1}')
  if [ "$W" = "401" ] && [ "$OK" = "200" ]; then
    result 7 wrong-key-rejected PASS "wrong key=$W, correct key=$OK -- rejection is authentication, not breakage"
  else
    result 7 wrong-key-rejected FAIL "SECURITY: wrong key=$W, correct key=$OK"
  fi

  # A credential must never come back out of the process it was configured into.
  if grep -q 'uat-tier7-key' "$OUT/api-scope-mutating.log"; then
    result 7 key-not-logged FAIL "SECURITY: the configured API key appears in the API's own log"
  else
    result 7 key-not-logged PASS "the configured API key never appears in the API's log"
  fi
else
  result 7 scope-mutating-only FAIL "API did not start in ApiKey/MutatingApi mode"
fi
stop_api

# ---------------------------------------------------------------- exposure, and its escape hatch
say "7.6 Public exposure with no auth must refuse to start (SECURITY.md 'exposure fails closed')"
if start_api public-none Nexo__Security__ExposureProfile=Public; then
  result 7 public-exposure-refused FAIL "SECURITY: API started with ExposureProfile=Public and AuthorizationMode=None"
else
  result 7 public-exposure-refused PASS "refused to start: $(grep -iE 'exposure|Security' "$OUT/api-public-none.log" | head -1 | cut -c1-150)"
fi
stop_api

say "7.7 the documented escape hatch turns the refusal into a warning (SECURITY.md 'escape hatch')"
if start_api public-hatch \
     Nexo__Security__ExposureProfile=Public \
     Nexo__Security__AllowUnauthenticatedNetworkExposure=true; then
  if grep -qiE 'warn' "$OUT/api-public-hatch.log"; then
    result 7 exposure-escape-hatch PASS "starts with AllowUnauthenticatedNetworkExposure=true, and warns"
  else
    result 7 exposure-escape-hatch FAIL "starts, but silently -- the page says the refusal becomes a WARNING"
  fi
else
  result 7 exposure-escape-hatch FAIL "escape hatch did not let the API start: $(tail -3 "$OUT/api-public-hatch.log" | tr '\n' ' ')"
fi
stop_api

# ---------------------------------------------------------------- static: the daemon socket
say "7.8 no shipped compose file mounts docker.sock into Nexo.API (SECURITY.md 'remote execution')"
SOCK=""
for f in deploy/compose/docker-compose.*.yml; do
  awk '
    /^  [a-zA-Z0-9_-]+:/ { svc=$1; sub(":","",svc) }
    /docker\.sock/       { print svc }
  ' "$f" | sort -u | while read -r svc; do
      case "$svc" in *api*|*API*) echo "$(basename "$f"):$svc";; esac
    done >>"$OUT/sock-hits.txt" 2>/dev/null
done
SOCK=$(cat "$OUT/sock-hits.txt" 2>/dev/null | tr '\n' ' ')
if [ -z "$(printf '%s' "$SOCK" | tr -d ' ')" ]; then
  result 7 no-docker-sock-in-api PASS "no shipped compose file mounts docker.sock into an api service"
else
  result 7 no-docker-sock-in-api FAIL "SECURITY: docker.sock mounted into:$SOCK"
fi

say "summary"
printf 'PASS=%d FAIL=%d\n' "$PASS" "$FAIL" | tee "$OUT/summary-tier7.txt"
[ "$FAIL" -eq 0 ]
