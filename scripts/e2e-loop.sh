#!/usr/bin/env bash
# End-to-end scenario suite for the product loop: init → verify → gates.
#
# Every scenario is a behavioural CLAIM about the shipped CLI, exercised against the real
# binaries — no mocks, each invocation a fresh process (which is itself the persistence
# test: state that survives between lines here survived process death). RESULT lines follow
# the UAT convention; the script exits non-zero if any scenario fails.
#
# Usage: bash scripts/e2e-loop.sh   (repo root; builds the CLI once, then --no-build)
set -uo pipefail

CLI_PROJ="${ASHLAR_CLI_PROJ:-application/src/Ashlar.CLI}"
WORK="${RUNNER_TEMP:-${TMPDIR:-/tmp}}/ashlar-e2e-$$"
RESULTS="$WORK/results.tsv"
mkdir -p "$WORK"
: > "$RESULTS"

PASS=0; FAIL=0; N=0

result() { # name PASS|FAIL detail
  N=$((N+1))
  printf 'RESULT\t%d\t%s\t%s\t%s\n' "$N" "$1" "$2" "$3" | tee -a "$RESULTS"
  if [ "$2" = PASS ]; then PASS=$((PASS+1)); else FAIL=$((FAIL+1)); fi
}

# run <name> <expected-rc> <grep-must ...> -- <cli args...>
# Captures stdout+stderr; asserts exit code and that every pattern appears.
OUT=""; RC=0
run_cli() {
  OUT=$(NO_COLOR=1 dotnet run --project "$CLI_PROJ" --no-build -- "$@" 2>&1) && RC=0 || RC=$?
}
claim() { # name expected_rc pattern...
  local name="$1" want="$2"; shift 2
  if [ "$RC" -ne "$want" ]; then
    result "$name" FAIL "exit $RC, expected $want :: $(echo "$OUT" | head -2 | tr '\n' ' ')"
    return
  fi
  local p
  for p in "$@"; do
    if ! echo "$OUT" | grep -qF -- "$p"; then
      result "$name" FAIL "missing '$p' :: $(echo "$OUT" | head -2 | tr '\n' ' ')"
      return
    fi
  done
  result "$name" PASS "exit $want"
}

fresh() { # fresh project dir; prints path
  local d="$WORK/p$RANDOM$RANDOM"
  mkdir -p "$d"; echo "$d"
}

# policy editors (operate on $1/ashlar.policy.yaml)
set_proposing() {
  sed -e "s/mode: sealed/mode: proposing/" \
      -e "s/gatesRequired: \[\]/gatesRequired: [sandbox, tests, security]/" \
      -e "s/extensions: 0/extensions: ${2:-3}/" \
      -e "s/mayAdd: \[\]/mayAdd: [brick]/" "$1/ashlar.policy.yaml" > "$1/.p" && mv "$1/.p" "$1/ashlar.policy.yaml"
}
set_selfextending() {
  set_proposing "$1" "${2:-3}"
  sed -e "s/mode: proposing/mode: self-extending/" "$1/ashlar.policy.yaml" > "$1/.p" && mv "$1/.p" "$1/ashlar.policy.yaml"
}

proposal_json() { # id kind [failedCourse|missing]
  local id="$1" kind="$2" variant="${3:-ok}"
  local tests='{"name":"tests","passed":true,"detail":"14 passed"}'
  [ "$variant" = failtests ] && tests='{"name":"tests","passed":false,"detail":"2 failed"}'
  local courses="{\"name\":\"sandbox\",\"passed\":true,\"detail\":\"confined\"},$tests,{\"name\":\"security\",\"passed\":true,\"detail\":\"0 findings\"}"
  [ "$variant" = missing ] && courses="{\"name\":\"sandbox\",\"passed\":true,\"detail\":\"confined\"},$tests"
  cat <<EOF
{"id":"$id","kind":"$kind","summary":"add brick demo.v2","proposedBy":"classifier",
 "proposedAt":"2026-08-23T02:14:00Z","diff":"+ 34 lines",
 "courses":[$courses]}
EOF
}

echo "== building the CLI once =="
dotnet build "$CLI_PROJ" -v quiet >/dev/null || { echo "build failed"; exit 1; }

# ───────────────────────────── init ─────────────────────────────
D=$(fresh)
run_cli init invoice-triage --path "$D"
claim "init-creates-project" 0 "ashlar.yaml" "the only file"
[ -f "$D/ashlar.yaml" ] && [ -f "$D/ashlar.policy.yaml" ] \
  && result "init-both-files-on-disk" PASS "both present" \
  || result "init-both-files-on-disk" FAIL "missing files"

H1=$(cat "$D/ashlar.yaml" "$D/ashlar.policy.yaml" | cksum)
run_cli init invoice-triage --path "$D"
claim "init-rerun-refuses" 1 "refusing to overwrite"
H2=$(cat "$D/ashlar.yaml" "$D/ashlar.policy.yaml" | cksum)
[ "$H1" = "$H2" ] \
  && result "init-refusal-left-files-untouched" PASS "checksums equal" \
  || result "init-refusal-left-files-untouched" FAIL "files changed on refusal"

for bad in "9lives" "-lead" "has space" "a.b"; do
  D=$(fresh); run_cli init "$bad" --path "$D"
  claim "init-rejects-name[$bad]" 1 "REJECTED"
  [ ! -f "$D/ashlar.yaml" ] \
    && result "init-rejection-writes-nothing[$bad]" PASS "no files" \
    || result "init-rejection-writes-nothing[$bad]" FAIL "wrote files for a rejected name"
done

D=$(fresh); run_cli init a --path "$D"
claim "init-accepts-single-letter" 0 "project contract for 'a'"

D="$WORK/dir with spaces/nested"
run_cli init spaced --path "$D"
claim "init-path-with-spaces" 0 "ashlar.yaml"

# ───────────────────────────── verify ─────────────────────────────
D=$(fresh); run_cli init demo --path "$D"
run_cli verify --path "$D"
claim "verify-fresh-project" 0 "VERIFIED" "unsigned" "course 1" "course 2" "course 3"

echo "$OUT" | grep -q "ed25519" \
  && result "verify-never-fakes-a-signature" FAIL "printed ed25519 with no keys" \
  || result "verify-never-fakes-a-signature" PASS "no signature claimed"

printf '%s' "$OUT" | grep -q $'\x1b' \
  && result "verify-redirected-output-is-plain" FAIL "raw ANSI in redirected output" \
  || result "verify-redirected-output-is-plain" PASS "no ESC bytes"

run_cli verify --path "$WORK"
claim "verify-not-a-project" 1 "not an ashlar project" "ashlar init"

D=$(fresh); run_cli init demo --path "$D"
echo "kind: Nonsense" > "$D/ashlar.yaml"
run_cli verify --path "$D"
claim "verify-broken-manifest-fails-contract" 65 "course 'contract'" "REJECTED"

D=$(fresh); run_cli init demo --path "$D"
printf '\nselfExtend:\n  mode: self-extending\n' >> "$D/ashlar.yaml"
run_cli verify --path "$D"
claim "verify-manifest-grabbing-the-envelope" 65 "policy-owned"

D=$(fresh); run_cli init demo --path "$D"
sed "s/gates: \[tests\]/gates: []/" "$D/ashlar.yaml" > "$D/.m" && mv "$D/.m" "$D/ashlar.yaml"
run_cli verify --path "$D"
claim "verify-ungated-agent" 65 "no gates"

D=$(fresh); run_cli init demo --path "$D"
sed "s|root: .|root: does-not-exist|" "$D/ashlar.policy.yaml" > "$D/.p" && mv "$D/.p" "$D/ashlar.policy.yaml"
run_cli verify --path "$D"
claim "verify-missing-sandbox-root" 65 "does not exist"

D=$(fresh); run_cli init demo --path "$D"
sed "s|writable: \[\]|writable: [../outside]|" "$D/ashlar.policy.yaml" > "$D/.p" && mv "$D/.p" "$D/ashlar.policy.yaml"
run_cli verify --path "$D"
claim "verify-writable-escape" 65 "escapes"

D=$(fresh); run_cli init demo --path "$D"
sed -e "s/mode: sealed/mode: proposing/" -e "s/gatesRequired: \[\]/gatesRequired: [tests]/" \
  "$D/ashlar.policy.yaml" > "$D/.p" && mv "$D/.p" "$D/ashlar.policy.yaml"
run_cli verify --path "$D"
claim "verify-unfunded-proposing" 65 "seal it or fund it"

D=$(fresh); run_cli init demo --path "$D"; set_proposing "$D"
run_cli verify --path "$D"
claim "verify-funded-proposing" 0 "VERIFIED" "mode: proposing"

# ───────────────────────────── gates ─────────────────────────────
D=$(fresh); run_cli init demo --path "$D"; set_proposing "$D"
run_cli gates --path "$D"
claim "gates-empty-queue" 0 "nothing held"

proposal_json ext-1 brick > "$D/p.json"
run_cli gates propose --file "$D/p.json" --path "$D"
claim "propose-proposing-holds" 0 "HELD" "a person seats the stone"

run_cli gates --path "$D"      # fresh process: durability is inherent to the assertion
claim "gates-lists-across-processes" 0 "ext-1" "classifier"

run_cli gates --show ext-1 --path "$D"
claim "gates-show-record" 0 "sandbox" "tests" "security" "HELD" "+ 34 lines"

run_cli gates --admit ext-1 --as tester --path "$D"
claim "gates-admit-seats" 0 "seated" "admitted by tester"

run_cli gates --show ext-1 --path "$D"
claim "gates-show-admitted" 0 "ADMITTED by tester"

run_cli gates --admit ext-1 --as anyone --path "$D"
claim "gates-no-second-decision" 1 "no administrative path"

proposal_json ext-1 brick > "$D/p.json"
run_cli gates propose --file "$D/p.json" --path "$D"
claim "propose-duplicate-id-refused" 1 "append-once"

proposal_json ext-r brick > "$D/p.json"
run_cli gates propose --file "$D/p.json" --path "$D" >/dev/null 2>&1 || true
run_cli gates --refuse ext-r --as tester --path "$D"
claim "refuse-requires-reason" 1 "requires a reason"

run_cli gates --refuse ext-r --as tester --reason "no regression case" --path "$D"
claim "refuse-with-reason-records" 0 "refused" "fed back"

run_cli gates --admit ghost-id --as tester --path "$D"
claim "gates-unknown-id" 1 "No proposal"

proposal_json "../escape" brick > "$D/p.json"
run_cli gates propose --file "$D/p.json" --path "$D"
claim "propose-illegal-id-fails-closed" 1 "Illegal proposal id"

echo "not json at all" > "$D/p.json"
run_cli gates propose --file "$D/p.json" --path "$D"
claim "propose-malformed-json" 1 "could not be parsed"

# sealed rejects everything
D=$(fresh); run_cli init demo --path "$D"
proposal_json ext-s brick > "$D/p.json"
run_cli gates propose --file "$D/p.json" --path "$D"
claim "propose-sealed-rejects" 65 "sealed"

# envelope beats mode and budget
D=$(fresh); run_cli init demo --path "$D"; set_selfextending "$D" 100
proposal_json ext-t tool > "$D/p.json"
run_cli gates propose --file "$D/p.json" --path "$D"
claim "propose-tool-outside-envelope" 65 "outside the envelope" "never the application's"

# failed and missing courses reject in proposing mode
D=$(fresh); run_cli init demo --path "$D"; set_proposing "$D"
proposal_json ext-f brick failtests > "$D/p.json"
run_cli gates propose --file "$D/p.json" --path "$D"
claim "propose-failed-course-rejects" 65 "'tests' failed"
proposal_json ext-m brick missing > "$D/p.json"
run_cli gates propose --file "$D/p.json" --path "$D"
claim "propose-missing-course-rejects" 65 "did not run"

# self-extending: admits within budget, then degrades to held
D=$(fresh); run_cli init demo --path "$D"; set_selfextending "$D" 1
proposal_json ext-a1 brick > "$D/p.json"
run_cli gates propose --file "$D/p.json" --path "$D"
claim "selfextend-admits-within-budget" 0 "ADMITTED" "1 of 1"
proposal_json ext-a2 brick > "$D/p.json"
run_cli gates propose --file "$D/p.json" --path "$D"
claim "selfextend-exhausted-degrades-to-held" 0 "HELD" "budget"

# ───────────────────────────── verdict ─────────────────────────────
echo
echo "==================== e2e-loop verdict ===================="
echo "scenarios: $N   pass: $PASS   fail: $FAIL"
if [ "$FAIL" -gt 0 ]; then
  echo "FAILED scenarios:"; awk -F'\t' '$4=="FAIL"' "$RESULTS" | sed 's/^/  /'
  exit 1
fi
echo "every behavioural claim under test holds"
