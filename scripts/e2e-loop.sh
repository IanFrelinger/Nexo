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

# ───────────────────────────── run ─────────────────────────────
D=$(fresh); run_cli init demo --path "$D"
sed -e "s/mode: sealed/mode: proposing/" -e "s/gatesRequired: \[\]/gatesRequired: [tests]/" "$D/ashlar.policy.yaml" > "$D/.p" && mv "$D/.p" "$D/ashlar.policy.yaml"
run_cli run "test request" --path "$D"
claim "run-refuses-unverified" 65 "you cannot run what does not verify"

sed -e "s/mode: proposing/mode: sealed/" -e "s/gatesRequired: \[tests\]/gatesRequired: []/" "$D/ashlar.policy.yaml" > "$D/.p" && mv "$D/.p" "$D/ashlar.policy.yaml"
run_cli run "classify one sample invoice" --path "$D"
claim "run-mock-completes" 0 "provider mock"

run_cli run "x" --path "$WORK"
claim "run-not-a-project" 1 "not an ashlar project"

# ───────────────────────────── keys / signing ─────────────────────────────
# Isolate the operator identity into the scratch tree — never touch the real ~/.ashlar/keys.
# Exported so both `keys` and the `gates` signing path (which loads the machine-global key)
# resolve the SAME directory, which is the whole point of presence-activation.
export ASHLAR_KEY_DIR="$WORK/keys"

run_cli keys show
claim "keys-show-none" 0 "no operator key"
echo "$OUT" | grep -q "ed25519" \
  && result "keys-show-none-hides-fingerprint" FAIL "printed a fingerprint with no key (S-3)" \
  || result "keys-show-none-hides-fingerprint" PASS "no fingerprint for an absent key"

run_cli keys init
claim "keys-init-generates" 0 "operator key ready" "ed25519:"
[ -f "$ASHLAR_KEY_DIR/operator.key" ] && [ -f "$ASHLAR_KEY_DIR/operator.pub" ] \
  && result "keys-init-writes-both-halves" PASS "seed + pub on disk" \
  || result "keys-init-writes-both-halves" FAIL "missing key files"

run_cli keys init
claim "keys-init-refuses-overwrite" 1 "already exists" "rotate"

run_cli keys show
claim "keys-show-fingerprint" 0 "ed25519:" "operator key"

run_cli keys init --rotate
claim "keys-init-rotate" 0 "operator key rotated"
ls "$ASHLAR_KEY_DIR"/trusted/*.pub >/dev/null 2>&1 \
  && result "keys-rotate-retains-old-pub" PASS "old pub kept in trusted/" \
  || result "keys-rotate-retains-old-pub" FAIL "old pub not retained"

# The integration: with a key present, the gate SIGNS the verdicts it records.
D=$(fresh); run_cli init demo --path "$D"; set_proposing "$D"
proposal_json ext-signed brick > "$D/p.json"
run_cli gates propose --file "$D/p.json" --path "$D"
claim "propose-with-key-holds" 0 "HELD"
grep -q '"Sig"' "$D/.ashlar/gates/ext-signed.json" \
  && result "held-record-is-signed" PASS "Sig present" \
  || result "held-record-is-signed" FAIL "no Sig in the held record"
grep -q '"Signer"' "$D/.ashlar/gates/ext-signed.json" \
  && result "held-record-names-signer" PASS "Signer embedded" \
  || result "held-record-names-signer" FAIL "no Signer in the held record"

# Admit re-signs over the NEW content; the show reads it back fail-closed, so a green read is
# the proof the verdict was NOT bricked by a stale signature (the bug the review caught).
run_cli gates --admit ext-signed --as tester --path "$D"
claim "admit-signed-record" 0 "seated"
run_cli gates --show ext-signed --path "$D"
claim "show-signed-admitted-reads-back" 0 "ADMITTED by tester"

# A record signed at rest still reads back after a fresh process — durability plus signature.
run_cli gates --show ext-signed --path "$D"
claim "signed-record-survives-process-death" 0 "ADMITTED"

# ── verify now CERTIFIES when a key is present, signing into the instance ledger ──
DV=$(fresh); run_cli init cert-demo --path "$DV"
run_cli verify --path "$DV"
claim "verify-certifies-with-key" 0 "CERTIFIED" "ed25519:" "ledger #1"
[ -f "$DV/.ashlar/ledger/000001.json" ] \
  && result "verify-writes-a-ledger-entry" PASS "entry on disk" \
  || result "verify-writes-a-ledger-entry" FAIL "no ledger entry written"
grep -q '"Sig"' "$DV/.ashlar/ledger/000001.json" \
  && result "ledger-entry-is-signed" PASS "Sig present" \
  || result "ledger-entry-is-signed" FAIL "ledger entry not signed"

# second verify: the provenance course appears and the chain extends to #2
run_cli verify --path "$DV"
claim "verify-second-run-adds-provenance" 0 "CERTIFIED" "provenance" "ledger #2"

# a tampered ledger fails verification via the provenance course, fail-closed
printf '{ not a valid entry' > "$DV/.ashlar/ledger/000001.json"
run_cli verify --path "$DV"
claim "verify-refuses-a-corrupt-ledger" 65 "provenance" "Corrupt ledger"

# A CORRUPT operator key must fail loud and CLEAN on write paths, and never block reads.
DSIGNED="$D"
printf 'not-valid-base64!!!' > "$ASHLAR_KEY_DIR/operator.key"

run_cli keys show
claim "keys-show-corrupt-fails-clean" 1 "Corrupt operator key"
echo "$OUT" | grep -qE "Unhandled exception|System.FormatException|^   at " \
  && result "keys-corrupt-no-raw-stack-trace" FAIL "leaked a stack trace instead of a clean message" \
  || result "keys-corrupt-no-raw-stack-trace" PASS "clean message, no stack trace"

DC=$(fresh); run_cli init demo --path "$DC"; set_proposing "$DC"
proposal_json ext-corrupt brick > "$DC/p.json"
run_cli gates propose --file "$DC/p.json" --path "$DC"
claim "propose-corrupt-key-fails-clean" 1 "Corrupt operator key"

# Reads verify via the record's OWN embedded key, so a corrupt operator key must NOT block them.
run_cli gates --show ext-signed --path "$DSIGNED"
claim "read-survives-a-corrupt-operator-key" 0 "ADMITTED"

unset ASHLAR_KEY_DIR

# ───────────────────────────── pkg: certified extensions travel ─────────────────────────────
# Two nodes sharing one extension: the origin admits it, seals it into a package; the receiver
# verifies it intrinsically and runs it through ITS OWN gate. Fresh key dir — the corrupt-key
# block above deliberately mangled the previous one.
export ASHLAR_KEY_DIR="$WORK/pkgkeys"
run_cli keys init >/dev/null

A=$(fresh); run_cli init origin-node --path "$A"; set_proposing "$A"
mkdir -p "$A/.ashlar/forge/proposed"
cat > "$A/.ashlar/forge/proposed/fpkg1.json" <<'EOF'
{"Id":"fpkg1","TargetPath":"src/Shared.cs","NewContent":"// shared brick v1","Summary":"add brick shared.classify","CreatedAt":"2026-08-24T06:00:00Z","UpdatedAt":"2026-08-24T06:00:00Z"}
EOF
cat > "$A/p.json" <<'EOF'
{"id":"ext-pkg","kind":"brick","summary":"add brick shared.classify","proposedBy":"night-agent",
 "proposedAt":"2026-08-24T06:00:00Z","diff":"+ 1 file",
 "forgeProposalIds":["fpkg1"],
 "courses":[{"name":"sandbox","passed":true,"detail":"confined"},{"name":"tests","passed":true,"detail":"14 passed"},{"name":"security","passed":true,"detail":"0 findings"}]}
EOF
run_cli gates propose --file "$A/p.json" --path "$A" >/dev/null
run_cli gates --admit ext-pkg --as origin-op --path "$A"
claim "pkg-origin-admit-applies" 0 "seated" "applied"
[ -f "$A/src/Shared.cs" ] \
  && result "pkg-origin-file-on-disk" PASS "the admitted write landed" \
  || result "pkg-origin-file-on-disk" FAIL "admitted write missing"

run_cli pkg export --id ext-pkg --out "$WORK/shared.ashpkg" --path "$A"
claim "pkg-export-seals" 0 "packaged" "ed25519:"

run_cli pkg show "$WORK/shared.ashpkg"
claim "pkg-show-verifies-keyless" 0 "package verifies" "shared.classify" "ed25519:"

# receiver in proposing mode: held, nothing on disk until THIS operator seats it
B=$(fresh); run_cli init receiver-node --path "$B"; set_proposing "$B"
run_cli pkg import "$WORK/shared.ashpkg" --path "$B"
claim "pkg-import-held-under-proposing" 0 "HELD" "origin verdict"
[ ! -f "$B/src/Shared.cs" ] \
  && result "pkg-import-held-not-on-disk" PASS "hold before write, for remote code too" \
  || result "pkg-import-held-not-on-disk" FAIL "imported file landed before admission"

run_cli gates --admit ext-pkg --as receiver-op --path "$B"
claim "pkg-receiver-admit-applies" 0 "seated" "applied"
grep -q "shared brick v1" "$B/src/Shared.cs" 2>/dev/null \
  && result "pkg-received-content-matches" PASS "the sealed bytes landed" \
  || result "pkg-received-content-matches" FAIL "content mismatch or missing"

# a sealed receiver rejects it and nothing ever lands
C=$(fresh); run_cli init sealed-node --path "$C"
run_cli pkg import "$WORK/shared.ashpkg" --path "$C"
claim "pkg-import-sealed-rejects" 65 "REJECTED" "sealed"
[ ! -f "$C/src/Shared.cs" ] \
  && result "pkg-sealed-nothing-lands" PASS "sender certification is not authority" \
  || result "pkg-sealed-nothing-lands" FAIL "sealed project accepted remote code"

# a tampered package is refused as forged, before any gate is consulted
sed 's/shared brick v1/backdoored/' "$WORK/shared.ashpkg" > "$WORK/tampered.ashpkg"
run_cli pkg import "$WORK/tampered.ashpkg" --path "$C"
claim "pkg-import-tampered-refused" 65 "seal does not verify"

# importing the same package twice is idempotent — already-decided is skipped, not re-parked
run_cli pkg import "$WORK/shared.ashpkg" --path "$B"
claim "pkg-import-duplicate-skipped" 0 "already imported"

# exporting without an operator key is refused — a seal is a signature
ASHLAR_KEY_DIR="$WORK/nokeys" run_cli pkg export --id ext-pkg --out "$WORK/x.ashpkg" --path "$A"
claim "pkg-export-keyless-refused" 1 "requires an operator key"

unset ASHLAR_KEY_DIR

# ───────────────────────────── pkg mesh: publish + pull between peers ─────────────────────────────
# The origin publishes its certified package to a mesh store; a peer pulls the whole store and
# each package faces the peer's OWN gate. Reuses the shared package from the pkg block above.
export ASHLAR_KEY_DIR="$WORK/pkgkeys"
MESH="$WORK/mesh-store"

run_cli pkg publish "$WORK/shared.ashpkg" --store "$MESH"
claim "pkg-publish-to-mesh" 0 "published to the mesh" "ed25519:"
ls "$MESH"/*.ashpkg >/dev/null 2>&1 \
  && result "pkg-publish-lands-in-store" PASS "package in the mesh store" \
  || result "pkg-publish-lands-in-store" FAIL "nothing in the store"

# publishing a tampered package to the mesh is refused at the source
run_cli pkg publish "$WORK/tampered.ashpkg" --store "$MESH"
claim "pkg-publish-refuses-forged" 65 "seal does not verify"

# a proposing peer pulls the store: the package is HELD (nothing on disk until it seats it)
PP=$(fresh); run_cli init pull-node --path "$PP"; set_proposing "$PP"
run_cli pkg pull --from "$MESH" --path "$PP"
claim "pkg-pull-holds-under-proposing" 0 "HELD" "faces YOUR gate" "pulled 1"
[ ! -f "$PP/src/Shared.cs" ] \
  && result "pkg-pull-held-not-on-disk" PASS "pulled code holds before the gate" \
  || result "pkg-pull-held-not-on-disk" FAIL "pulled file landed before admission"

# re-pulling the same store is idempotent: already-decided packages are skipped, not re-parked
run_cli pkg pull --from "$MESH" --path "$PP"
claim "pkg-pull-idempotent" 0 "already have" "already-have 1"
FORGE_COUNT=$(ls "$PP/.ashlar/forge/proposed"/*.json 2>/dev/null | wc -l)
[ "$FORGE_COUNT" -le 1 ] \
  && result "pkg-repull-does-not-leak-forge" PASS "no orphaned parked proposals" \
  || result "pkg-repull-does-not-leak-forge" FAIL "forge queue grew on re-pull ($FORGE_COUNT)"

# a sealed peer pulls the same store: every package rejected, nothing lands, non-zero exit
PS=$(fresh); run_cli init sealed-pull --path "$PS"
run_cli pkg pull --from "$MESH" --path "$PS"
claim "pkg-pull-sealed-rejects-all" 65 "REJECTED" "refused/rejected 1"
[ ! -f "$PS/src/Shared.cs" ] \
  && result "pkg-pull-sealed-nothing-lands" PASS "receiver sovereignty holds over the mesh" \
  || result "pkg-pull-sealed-nothing-lands" FAIL "sealed peer accepted mesh code"

# pulling from an empty store is a clean no-op
run_cli pkg pull --from "$WORK/nostore-$RANDOM" --path "$PP"
claim "pkg-pull-missing-store" 1 "no such peer store"

unset ASHLAR_KEY_DIR

# ───────────────────────────── pkg mesh 2: two-node co-production ─────────────────────────────
# The loop the mesh exists for: node A admits and SHARES v1 in one verb; node B pulls it through
# ITS OWN gate, builds v2 on top, admits, shares back; node A pulls v2 through ITS gate. Both
# gates exercised in both directions, and the store dedupes what each side already decided.
export ASHLAR_KEY_DIR="$WORK/pkgkeys"
MESH2="$WORK/mesh-coprod"

CA=$(fresh); run_cli init coprod-a --path "$CA"; set_proposing "$CA"
CB=$(fresh); run_cli init coprod-b --path "$CB"; set_proposing "$CB"

# node A: admit v1 (a parked forge write + its gate proposal), then share = seal + publish
mkdir -p "$CA/.ashlar/forge/proposed"
cat > "$CA/.ashlar/forge/proposed/fco1.json" <<'EOF'
{"Id":"fco1","TargetPath":"src/Coprod.cs","NewContent":"// coprod v1","Summary":"add brick coprod.classify","CreatedAt":"2026-08-25T06:00:00Z","UpdatedAt":"2026-08-25T06:00:00Z"}
EOF
cat > "$CA/pco1.json" <<'EOF'
{"id":"ext-co1","kind":"brick","summary":"add brick coprod.classify","proposedBy":"night-agent",
 "proposedAt":"2026-08-25T06:00:00Z","diff":"+ 1 file",
 "forgeProposalIds":["fco1"],
 "courses":[{"name":"sandbox","passed":true,"detail":"confined"},{"name":"tests","passed":true,"detail":"14 passed"},{"name":"security","passed":true,"detail":"0 findings"}]}
EOF
run_cli gates propose --file "$CA/pco1.json" --path "$CA" >/dev/null
run_cli gates --admit ext-co1 --as coprod-a-op --path "$CA"
claim "coprod-a-admit-applies-v1" 0 "seated" "applied"
run_cli pkg share --id ext-co1 --store "$MESH2" --path "$CA"
claim "coprod-share-one-verb" 0 "shared to the mesh" "ed25519:"
[ "$(ls "$MESH2"/*.ashpkg 2>/dev/null | wc -l)" -eq 1 ] \
  && result "coprod-share-lands-in-store" PASS "seal + publish in one step" \
  || result "coprod-share-lands-in-store" FAIL "share did not land exactly one package"

# node B: pull v1 — held by B's OWN gate, nothing on disk until B seats it
run_cli pkg pull --from "$MESH2" --path "$CB"
claim "coprod-b-pull-holds" 0 "HELD" "pulled 1"
[ ! -f "$CB/src/Coprod.cs" ] \
  && result "coprod-b-held-not-on-disk" PASS "B's gate holds A's work before it lands" \
  || result "coprod-b-held-not-on-disk" FAIL "pulled file landed before admission"
run_cli gates --admit ext-co1 --as coprod-b-op --path "$CB"
claim "coprod-b-admit-applies-v1" 0 "seated" "applied"
grep -q "coprod v1" "$CB/src/Coprod.cs" 2>/dev/null \
  && result "coprod-b-holds-v1" PASS "v1 landed on B through B's gate" \
  || result "coprod-b-holds-v1" FAIL "v1 content missing on B"

# node B: build v2 ON TOP of v1, admit through B's gate, share back to the same store
mkdir -p "$CB/.ashlar/forge/proposed"
cat > "$CB/.ashlar/forge/proposed/fco2.json" <<'EOF'
{"Id":"fco2","TargetPath":"src/Coprod.cs","NewContent":"// coprod v2 (builds on v1)","Summary":"improve brick coprod.classify","CreatedAt":"2026-08-25T07:00:00Z","UpdatedAt":"2026-08-25T07:00:00Z"}
EOF
cat > "$CB/pco2.json" <<'EOF'
{"id":"ext-co2","kind":"brick","summary":"improve brick coprod.classify","proposedBy":"night-agent",
 "proposedAt":"2026-08-25T07:00:00Z","diff":"~ 1 file",
 "forgeProposalIds":["fco2"],
 "courses":[{"name":"sandbox","passed":true,"detail":"confined"},{"name":"tests","passed":true,"detail":"15 passed"},{"name":"security","passed":true,"detail":"0 findings"}]}
EOF
run_cli gates propose --file "$CB/pco2.json" --path "$CB" >/dev/null
run_cli gates --admit ext-co2 --as coprod-b-op --path "$CB" >/dev/null
run_cli pkg share --id ext-co2 --store "$MESH2" --path "$CB"
claim "coprod-b-shares-v2-back" 0 "shared to the mesh"

# node A: pull the store — v1 is already-have (A's gate decided it), v2 holds, A seats it
run_cli pkg pull --from "$MESH2" --path "$CA"
claim "coprod-a-pull-dedupes-and-holds" 0 "already have" "HELD" "pulled 2"
run_cli gates --admit ext-co2 --as coprod-a-op --path "$CA"
claim "coprod-a-admit-applies-v2" 0 "seated" "applied"
grep -q "coprod v2" "$CA/src/Coprod.cs" 2>/dev/null \
  && result "coprod-a-holds-v2" PASS "A's file holds v2 — both gates, both directions" \
  || result "coprod-a-holds-v2" FAIL "v2 did not land on A"

# content claims: the admission signed (path, sha256) for its rows at propose time, so a forge
# row edited AFTER the gate decided fails verification — the doctored bytes never travel under
# the origin's signature. Refusal is a 65, the same family as a package that fails its seal.
sed 's|// coprod v1|// tampered after admission|' "$CA/.ashlar/forge/applied/fco1.json" > "$CA/.t" \
  && mv "$CA/.t" "$CA/.ashlar/forge/applied/fco1.json"
run_cli pkg export --id ext-co1 --out "$WORK/tampered-row.ashpkg" --path "$CA"
claim "pkg-claims-refuse-edited-row" 65 "does not match the signed claim"
[ ! -f "$WORK/tampered-row.ashpkg" ] \
  && result "pkg-claims-nothing-written" PASS "a refused export writes no package" \
  || result "pkg-claims-nothing-written" FAIL "a package landed despite the failed claim"

unset ASHLAR_KEY_DIR

# ───────────────────────────── export native: the agentic-app bundle ─────────────────────────────
# A certified project becomes a portable, self-proving bundle. The runtime binary is a separate
# (slow) publish, so here we prove the deterministic staging and that the staged app self-verifies
# offline with NO origin key — the download proving its own certification.
export ASHLAR_KEY_DIR="$WORK/exportkeys"
run_cli keys init >/dev/null

DE=$(fresh); run_cli init exportdemo --path "$DE"
run_cli verify --path "$DE" >/dev/null      # certify: ledger #1
run_cli export native --path "$DE" --out "$WORK/bundles" --rid linux-x64 --no-runtime
claim "export-native-stages-bundle" 0 "CERTIFIED bundle" "exportdemo" "linux-x64"

BUN="$WORK/bundles/exportdemo-linux-x64"
{ [ -f "$BUN/app/ashlar.yaml" ] && [ -f "$BUN/app/ashlar.policy.yaml" ] && [ -d "$BUN/app/.ashlar" ] \
  && [ -f "$BUN/run.sh" ] && [ -f "$BUN/run.cmd" ] && [ -f "$BUN/bundle.json" ] && [ -f "$BUN/README.md" ]; } \
  && result "export-native-bundle-complete" PASS "app + ledger + launchers + descriptor" \
  || result "export-native-bundle-complete" FAIL "bundle incomplete"

grep -q '"certified": true' "$BUN/bundle.json" \
  && result "export-native-descriptor-certified" PASS "descriptor records the certification" \
  || result "export-native-descriptor-certified" FAIL "descriptor missing certification"

# the launcher verifies before it runs — the self-proving contract, checked in the staged script
grep -q "verify --path" "$BUN/run.sh" \
  && result "export-native-launcher-self-proves" PASS "run.sh verifies before running" \
  || result "export-native-launcher-self-proves" FAIL "launcher does not self-verify"

# a DOWNLOADER with NO origin key still confirms the app's certification chain, offline
DVOUT=$(ASHLAR_KEY_DIR="$WORK/emptykeys" NO_COLOR=1 dotnet run --project "$CLI_PROJ" --no-build -- verify --path "$BUN/app" 2>&1); DVRC=$?
OUT="$DVOUT"; RC="$DVRC"
claim "export-native-app-self-verifies-keyless" 0 "provenance" "chain intact"

# tamper the staged contract after export: a keyless downloader's verify REFUSES it (the whole
# point of self-proving — an altered app does not run)
echo "# tampered after signing" >> "$BUN/app/ashlar.yaml"
DTOUT=$(ASHLAR_KEY_DIR="$WORK/emptykeys" NO_COLOR=1 dotnet run --project "$CLI_PROJ" --no-build -- verify --path "$BUN/app" 2>&1); DTRC=$?
OUT="$DTOUT"; RC="$DTRC"
claim "export-native-tampered-app-refused" 65 "do not match the certification" "provenance"

# refuses to export a project that does not verify
DBAD=$(fresh); run_cli init badexport --path "$DBAD"
echo "kind: Nonsense" > "$DBAD/ashlar.yaml"
run_cli export native --path "$DBAD" --out "$WORK/bundles" --rid linux-x64 --no-runtime
claim "export-native-refuses-unverified" 65 "does not verify"

# ───────────────────────────── export aws / azure: one-command cloud bundles ─────────────────────────────
# Same certified project, staged for the cloud: the runtime image + this app + a verify-then-run
# entrypoint + a one-command deploy script. Nothing here touches a cloud — the scripts do, when run.
run_cli export aws --path "$DE" --out "$WORK/bundles"
claim "export-aws-stages-bundle" 0 "CERTIFIED cloud bundle" "aws"
AWSB="$WORK/bundles/exportdemo-aws"
{ [ -f "$AWSB/Dockerfile" ] && [ -f "$AWSB/entrypoint.sh" ] && [ -f "$AWSB/deploy-aws.sh" ] \
  && [ -f "$AWSB/app/ashlar.yaml" ] && [ -d "$AWSB/app/.ashlar" ] && [ -f "$AWSB/bundle.json" ]; } \
  && result "export-aws-bundle-complete" PASS "Dockerfile + entrypoint + deploy + app + ledger" \
  || result "export-aws-bundle-complete" FAIL "aws bundle incomplete"
grep -q "verify --path /work/app" "$AWSB/entrypoint.sh" \
  && result "export-aws-container-self-proves" PASS "the container verifies before it runs" \
  || result "export-aws-container-self-proves" FAIL "entrypoint does not verify first"

run_cli export azure --path "$DE" --out "$WORK/bundles"
claim "export-azure-stages-bundle" 0 "CERTIFIED cloud bundle" "azure"
AZB="$WORK/bundles/exportdemo-azure"
{ [ -f "$AZB/deploy-azure.sh" ] && grep -q "az acr build" "$AZB/deploy-azure.sh" && grep -q "az container create" "$AZB/deploy-azure.sh"; } \
  && result "export-azure-deploy-script" PASS "ACR build + ACI one-shot" \
  || result "export-azure-deploy-script" FAIL "azure deploy script wrong"

run_cli export aws --path "$DBAD" --out "$WORK/bundles"
claim "export-aws-refuses-unverified" 65 "does not verify"

unset ASHLAR_KEY_DIR

# ───────────────────────────── verdict ─────────────────────────────
echo
echo "==================== e2e-loop verdict ===================="
echo "scenarios: $N   pass: $PASS   fail: $FAIL"
if [ "$FAIL" -gt 0 ]; then
  echo "FAILED scenarios:"; awk -F'\t' '$4=="FAIL"' "$RESULTS" | sed 's/^/  /'
  exit 1
fi
echo "every behavioural claim under test holds"
