#!/usr/bin/env bash
# Nexo UAT — Tier 4 (authoring / integration) and the security negatives the quickstart asks testers to run.
#
# Section 5 of TesterQuickstart lists what is "worth your time", which makes each item a claim the page is
# making about the product. Three of them are machine-checkable and are checked here.
set -uo pipefail

REPO_URL="${REPO_URL:-https://github.com/IanFrelinger/Nexo.git}"
WORK="${WORK:-/work}"
# See tier0-2.sh: UAT_REPO_DIR runs against the workspace checkout so a gate tests the commit under review.
SRC="${UAT_REPO_DIR:-$WORK/Nexo}"
OUT="${UAT_OUT:-$WORK/out}"
mkdir -p "$OUT"
: >"$OUT/results-tier4.tsv"

PASS=0; FAIL=0
result() {
  local tier="$1" id="$2" verdict="$3"; shift 3
  printf 'RESULT\t%s\t%s\t%s\t%s\n' "$tier" "$id" "$verdict" "$*" | tee -a "$OUT/results-tier4.tsv"
  if [ "$verdict" = PASS ]; then PASS=$((PASS+1)); else FAIL=$((FAIL+1)); fi
}
say() { printf '\n=== %s ===\n' "$*"; }
unset ASPNETCORE_HTTP_PORTS   # image opinion, not the product's (see tier0-2.sh)

[ -d "$SRC" ] || git clone --quiet "$REPO_URL" "$SRC" || exit 1
cd "$SRC" || exit 1

# ---------------------------------------------------------------- Tier 4: authoring
say "4.1 the reference brick, via the command TesterQuickstart section 4 prints"
t0=$(date +%s)
if dotnet test samples/hello-brick/HelloBrick.Tests/HelloBrick.Tests.csproj >"$OUT/hello-brick.log" 2>&1; then
  TOTAL=$(grep -oE 'Passed: +[0-9]+' "$OUT/hello-brick.log" | tail -1 | grep -oE '[0-9]+')
  result 4 hello-brick PASS "$(( $(date +%s) - t0 ))s, ${TOTAL:-?} passed"
else
  result 4 hello-brick FAIL "$(grep -m3 -E 'error|Failed ' "$OUT/hello-brick.log" | tr '\n' ' ')"
fi

say "4.2 the sample builds by ProjectReference into src/, as the page says (nothing on nuget.org yet)"
if grep -rq "ProjectReference" samples/hello-brick/ 2>/dev/null; then
  result 4 hello-brick-projectref PASS "sample consumes Nexo by ProjectReference"
else
  result 4 hello-brick-projectref FAIL "no ProjectReference in samples/hello-brick"
fi

# ---------------------------------------------------------------- security negatives (quickstart section 5)
say "4.3 'Does the API fail closed?' — Lan exposure with no auth mode must REFUSE TO START"
export NEXO_ALLOW_MOCK=1
Nexo__Security__ExposureProfile=Lan timeout 120 dotnet run --project application/src/Nexo.API -f net10.0 \
  >"$OUT/api-lan.log" 2>&1
LAN_RC=$?
if grep -q 'Now listening on:' "$OUT/api-lan.log"; then
  result 4 exposure-fail-closed FAIL "SECURITY: API started with ExposureProfile=Lan and no auth mode"
else
  result 4 exposure-fail-closed PASS "refused to start (rc=$LAN_RC): $(grep -iE 'exposure|security' "$OUT/api-lan.log" | head -1 | cut -c1-160)"
fi

say "4.4 'Does the API fail closed?' — ApiKey mode must 401 a task POST with no key header"
export Nexo__Security__AuthorizationMode=ApiKey
export Nexo__Security__ApiKey=uat-probe-key
export Nexo__Security__AuthorizationScope=AllApi
dotnet run --project application/src/Nexo.API -f net10.0 >"$OUT/api-auth.log" 2>&1 &
AUTH_PID=$!
for _ in $(seq 1 90); do grep -q 'Now listening on:' "$OUT/api-auth.log" 2>/dev/null && break; kill -0 $AUTH_PID 2>/dev/null || break; sleep 2; done

if grep -q 'Now listening on:' "$OUT/api-auth.log"; then
  # Anchor on the listen line. The startup log carries other URLs first -- the Ollama probe
  # (http://127.0.0.1:11434/api/tags) precedes it -- and a bare "first http:// in the file" grep
  # silently points curl at a dead port, which then reads as a failed security check.
  API=$(grep -m1 -oE 'Now listening on: *http://[^ ]+' "$OUT/api-auth.log" \
        | sed 's/Now listening on: *//; s#0\.0\.0\.0#localhost#; s#\[::\]#localhost#')
  CODE=$(curl -s -o "$OUT/nokey.json" -w '%{http_code}' --max-time 60 "$API/api/copilot/task" \
          -H "Content-Type: application/json" -d '{"task":"probe","auditCount":1}')
  if [ "$CODE" = "401" ]; then
    result 4 apikey-401-without-header PASS "POST /api/copilot/task without X-Nexo-Api-Key -> 401"
  else
    result 4 apikey-401-without-header FAIL "SECURITY: expected 401, got $CODE ($(head -c 120 "$OUT/nokey.json"))"
  fi

  # And the same call WITH the key must be allowed, or the check above proves nothing.
  CODE2=$(curl -s -o "$OUT/withkey.json" -w '%{http_code}' --max-time 300 "$API/api/copilot/task" \
           -H "Content-Type: application/json" -H "X-Nexo-Api-Key: uat-probe-key" \
           -d '{"task":"probe","auditCount":1}')
  if [ "$CODE2" = "200" ]; then
    result 4 apikey-200-with-header PASS "same POST with the key -> 200 (the 401 is auth, not breakage)"
  else
    result 4 apikey-200-with-header FAIL "expected 200 with a valid key, got $CODE2"
  fi

  # Probes must stay outside auth (container HEALTHCHECK / k8s probes send no headers).
  HCODE=$(curl -s -o /dev/null -w '%{http_code}' --max-time 30 "$API/health")
  RCODE=$(curl -s -o /dev/null -w '%{http_code}' --max-time 30 "$API/ready")
  if [ "$HCODE" = "200" ] && [ "$RCODE" = "200" ]; then
    result 4 probes-outside-auth PASS "/health=$HCODE /ready=$RCODE with auth on and no credentials"
  else
    result 4 probes-outside-auth FAIL "probes need credentials: /health=$HCODE /ready=$RCODE"
  fi
else
  result 4 apikey-401-without-header FAIL "API did not start in ApiKey mode: $(tail -3 "$OUT/api-auth.log" | tr '\n' ' ')"
fi
kill $AUTH_PID 2>/dev/null; wait $AUTH_PID 2>/dev/null; pkill -f 'Nexo\.API' 2>/dev/null

say "4.5 a BLANK configured key must not authenticate anyone, least of all a blank one"
# docker-compose.friend-mesh.yml interpolates Nexo__Security__ApiKey with no default, so an operator
# who forgets the variable boots with AuthorizationMode=ApiKey and an empty key. Program.cs logs that
# protected routes "will reject requests until configuration is complete"; this checks that they do.
# The empty-header case is the one worth pinning: an empty presented credential must not compare equal
# to an empty configured one, which is the trap that turns a misconfiguration into an open API.
export Nexo__Security__AuthorizationMode=ApiKey
export Nexo__Security__ApiKey=""
export Nexo__Security__AuthorizationScope=AllApi
dotnet run --project application/src/Nexo.API -f net10.0 >"$OUT/api-blank.log" 2>&1 &
BLANK_PID=$!
for _ in $(seq 1 90); do grep -q 'Now listening on:' "$OUT/api-blank.log" 2>/dev/null && break; kill -0 $BLANK_PID 2>/dev/null || break; sleep 2; done

if grep -q 'Now listening on:' "$OUT/api-blank.log"; then
  BAPI=$(grep -m1 -oE 'Now listening on: *http://[^ ]+' "$OUT/api-blank.log" \
         | sed 's/Now listening on: *//; s#0\.0\.0\.0#localhost#; s#\[::\]#localhost#')
  post() { curl -s -o /dev/null -w '%{http_code}' --max-time 60 "$BAPI/api/copilot/task" \
             -H 'Content-Type: application/json' "$@" -d '{"task":"probe","auditCount":1}'; }
  NONE=$(post)
  EMPTY=$(post -H 'X-Nexo-Api-Key: ')
  GUESS=$(post -H 'X-Nexo-Api-Key: anything')
  if [ "$NONE" = "401" ] && [ "$EMPTY" = "401" ] && [ "$GUESS" = "401" ]; then
    result 4 blank-key-authenticates-nobody PASS "blank configured key: no header=$NONE, empty header=$EMPTY, arbitrary=$GUESS"
  else
    result 4 blank-key-authenticates-nobody FAIL "SECURITY: blank configured key admitted a request -- no header=$NONE, empty header=$EMPTY, arbitrary=$GUESS"
  fi
else
  result 4 blank-key-authenticates-nobody FAIL "API did not start with a blank key: $(tail -3 "$OUT/api-blank.log" | tr '\n' ' ')"
fi
kill $BLANK_PID 2>/dev/null; wait $BLANK_PID 2>/dev/null; pkill -f 'Nexo\.API' 2>/dev/null

say "summary"
printf 'PASS=%d FAIL=%d\n' "$PASS" "$FAIL" | tee "$OUT/summary-tier4.txt"
[ "$FAIL" -eq 0 ]
