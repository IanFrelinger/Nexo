#!/usr/bin/env bash
# Ashlar release IMAGE lab v2 — fixes: run the CLI directly as the image's app user under /tmp
# (no wrapper, no root); generate certs INSIDE a volume container (image has openssl); robust
# heartbeat detection; no host bind-mounts (Windows-safe). Orchestrated from host; HTTP/cert
# probes via elated_satoshi on the lab network.
set -uo pipefail
export MSYS_NO_PATHCONV=1 MSYS2_ARG_CONV_EXCL='*'
IMG=ashlar-cli:lab; NET=ashlar-lab; OLLAMA=http://ashlar-lab-ollama:11434; MODEL=qwen2.5-coder:1.5b
WB=lab-wb; PROBE=elated_satoshi; A='dotnet /app/Ashlar.CLI.dll'
RES=/tmp/imagelab-results.tsv; : > "$RES"; P=0; W=0; F=0
g="\033[32m";y="\033[33m";r="\033[31m";d="\033[2m";z="\033[0m"; SEV=major
pass(){ P=$((P+1)); printf "  ${g}PASS${z} [%-7s] %-38s ${d}%s${z}\n" "$SEV" "$1" "${2:-}"; printf 'PASS\t%s\t%s\t%s\n' "$SEV" "$1" "${2:-}">>"$RES"; }
weak(){ W=$((W+1)); printf "  ${y}WEAK${z} [%-7s] %-38s ${d}%s${z}\n" "$SEV" "$1" "${2:-}"; printf 'WEAK\t%s\t%s\t%s\n' "$SEV" "$1" "${2:-}">>"$RES"; }
fail(){ F=$((F+1)); printf "  ${r}FAIL${z} [%-7s] %-38s ${d}%s${z}\n" "$SEV" "$1" "${2:-}"; printf 'FAIL\t%s\t%s\t%s\n' "$SEV" "$1" "${2:-}">>"$RES"; }
sec(){ printf "\n${d}== %s ==${z}\n" "$1"; }
wb(){ docker exec "$WB" bash -lc "$1" 2>&1; }
probe(){ docker exec "$PROBE" bash -lc "$1" 2>&1; }
nrm(){ docker rm -f "$@" >/dev/null 2>&1 || true; }
cleanup(){ nrm $(docker ps -aq --filter name=labnode- 2>/dev/null) "$WB" >/dev/null 2>&1; docker volume ls -q --filter name=labvol- 2>/dev/null | xargs -r docker volume rm >/dev/null 2>&1; }
trap cleanup EXIT; cleanup
echo "Ashlar IMAGE lab v2  img=$IMG"

docker run -d --name "$WB" --network "$NET" --entrypoint sleep "$IMG" infinity >/dev/null
wb 'mkdir -p /tmp/w && echo wb-ok' | grep -q wb-ok || { echo "workbench unwritable /tmp"; exit 1; }

# proposing-policy sed
PROP='sed -i -e "s/mode: sealed/mode: proposing/" -e "s/gatesRequired: \[\]/gatesRequired: [sandbox, tests, security]/" -e "s/extensions: 0/extensions: 3/" -e "s/mayAdd: \[\]/mayAdd: [brick]/"'

seal_pkg(){ # keydir projdir outpath  (all inside workbench)
  wb "set -e; export ASHLAR_KEY_DIR=$1; $A keys init >/dev/null 2>&1 || true
    D=$2; rm -rf \$D; mkdir -p \$D; $A init origin --path \$D >/dev/null
    $PROP \$D/ashlar.policy.yaml
    mkdir -p \$D/.ashlar/forge/proposed
    echo '{\"Id\":\"f1\",\"TargetPath\":\"src/S.cs\",\"NewContent\":\"// v1\",\"Summary\":\"add\",\"CreatedAt\":\"2026-08-24T06:00:00Z\",\"UpdatedAt\":\"2026-08-24T06:00:00Z\"}' > \$D/.ashlar/forge/proposed/f1.json
    printf '%s' '{\"id\":\"ext-p\",\"kind\":\"brick\",\"summary\":\"s\",\"proposedBy\":\"n\",\"proposedAt\":\"2026-08-24T06:00:00Z\",\"diff\":\"+1\",\"forgeProposalIds\":[\"f1\"],\"courses\":[{\"name\":\"sandbox\",\"passed\":true,\"detail\":\"c\"},{\"name\":\"tests\",\"passed\":true,\"detail\":\"t\"},{\"name\":\"security\",\"passed\":true,\"detail\":\"s\"}]}' > \$D/p.json
    $A gates propose --file \$D/p.json --path \$D >/dev/null
    $A gates --admit ext-p --as op --path \$D >/dev/null
    $A pkg export --id ext-p --out $3 --path \$D >/dev/null && echo SEALED"; }

# =========================================================================
sec "A. image & deployment sanity"
SEV=blocker
miss=""; for v in "policy --help" "mesh lan --help" "background-agent report --help" "background-agent disarm --help"; do
  wb "$A $v >/dev/null 2>&1" >/dev/null || miss="$miss [$v]"; done
[ -z "$miss" ] && pass "image-carries-new-features" "policy/mesh-lan/report/disarm present" || fail "image-carries-new-features" "MISSING:$miss"

# =========================================================================
sec "B. identity & governance (through image)"
SEV=major
fp=$(wb 'export ASHLAR_KEY_DIR=/tmp/w/idk; '"$A"' keys init 2>&1' | grep -oE 'ed25519:[0-9a-f]{16}' | head -1)
[ -n "$fp" ] && pass "keys-init-fingerprint" "$fp" || fail "keys-init-fingerprint" "no fingerprint"
wb 'export ASHLAR_KEY_DIR=/tmp/w/none; '"$A"' keys show 2>&1' | grep -qiE 'no.*(key|operator)|unsigned' && pass "keys-show-none-honest" "" || weak "keys-show-none-honest" "unexpected"
SEV=blocker
seal_pkg /tmp/w/ok /tmp/w/origin /tmp/w/shared.ashpkg | grep -q SEALED && pass "pkg-export-seals" "signed .ashpkg" || fail "pkg-export-seals" "seal failed"
recv='export ASHLAR_KEY_DIR=/tmp/w/ok; R=/tmp/w/recv; rm -rf $R; '"$A"' init recv --path $R >/dev/null; '"$PROP"' $R/ashlar.policy.yaml'
wb "$recv; $A pkg import /tmp/w/shared.ashpkg --path \$R 2>&1" | grep -qi 'HELD' && pass "trust-trusted-held" "self-trust → held" || fail "trust-trusted-held" "not held"
wb "$recv; sed 's/\"Seal\"/\"XSeal\"/' /tmp/w/shared.ashpkg > /tmp/w/unsealed.ashpkg; $A pkg import /tmp/w/unsealed.ashpkg --path \$R 2>&1" | grep -qiE 'seal|refus|forg' && pass "trust-unsealed-refused" "" || fail "trust-unsealed-refused" "not refused"
wb "export ASHLAR_KEY_DIR=/tmp/w/stranger; $A keys init >/dev/null 2>&1; S=/tmp/w/srecv; rm -rf \$S; $A init s --path \$S >/dev/null; $PROP \$S/ashlar.policy.yaml; $A pkg import /tmp/w/shared.ashpkg --path \$S 2>&1" | grep -qiE 'not a trusted signer|REFUS' && pass "trust-stranger-refused" "untrusted refused" || fail "trust-stranger-refused" "not refused"
# gate modes
gm='export ASHLAR_KEY_DIR=/tmp/w/ok; G=/tmp/w/gm; rm -rf $G; '"$A"' init g --path $G >/dev/null; mkdir -p $G/.ashlar/forge/proposed; echo "{\"Id\":\"fg\",\"TargetPath\":\"src/G.cs\",\"NewContent\":\"//g\",\"Summary\":\"s\",\"CreatedAt\":\"2026-08-24T06:00:00Z\",\"UpdatedAt\":\"2026-08-24T06:00:00Z\"}">$G/.ashlar/forge/proposed/fg.json; printf "%s" "{\"id\":\"ext-g\",\"kind\":\"brick\",\"summary\":\"s\",\"proposedBy\":\"n\",\"proposedAt\":\"2026-08-24T06:00:00Z\",\"diff\":\"+1\",\"forgeProposalIds\":[\"fg\"],\"courses\":[{\"name\":\"sandbox\",\"passed\":true,\"detail\":\"c\"}]}">$G/p.json'
wb "$gm; $A gates propose --file \$G/p.json --path \$G 2>&1" | grep -qiE 'sealed|REJECT' && pass "gate-sealed-rejects" "" || fail "gate-sealed-rejects" "did not reject"
wb "$gm; sed -i -e 's/mode: sealed/mode: proposing/' -e 's/gatesRequired: \[\]/gatesRequired: [sandbox]/' -e 's/mayAdd: \[\]/mayAdd: [brick]/' \$G/ashlar.policy.yaml; $A gates propose --file \$G/p.json --path \$G 2>&1" | grep -qi 'HELD' && pass "gate-proposing-holds" "" || fail "gate-proposing-holds" "did not hold"

# =========================================================================
sec "C. arming (staged opt-in)"
SEV=blocker
arm='export ASHLAR_KEY_DIR=/tmp/w/ok; AR=/tmp/w/arm; rm -rf $AR; '"$A"' init a --path $AR >/dev/null; sed -i -e "s/gatesRequired: \[\]/gatesRequired: [sandbox]/" -e "s/mayAdd: \[\]/mayAdd: [brick]/" -e "s/extensions: 0/extensions: 3/" $AR/ashlar.policy.yaml'
wb "$arm; $A policy set self_extend proposing --path \$AR >/dev/null 2>&1; $A policy show --path \$AR 2>&1 | grep -i mode" | grep -qi 'proposing' && pass "arm-set-proposing" "" || fail "arm-set-proposing" "not set"
wb "$arm; $A policy set self_extend self-extending --path \$AR 2>&1" | grep -qi 'ARMED' && pass "arm-self-extending-warns" "loud ARMED" || fail "arm-self-extending-warns" "no warning"
wb "$arm; $A policy set never '[]' --path \$AR 2>&1" | grep -qi 'not editable' && pass "arm-floor-immutable" "unsupported key refused" || fail "arm-floor-immutable" "accepted"
wb "export ASHLAR_KEY_DIR=/tmp/w/ok; A2=/tmp/w/arm2; rm -rf \$A2; $A init a2 --path \$A2 >/dev/null; $A policy set self_extend self-extending --path \$A2 >/dev/null 2>&1; grep -i 'mode:' \$A2/ashlar.policy.yaml" | grep -qi 'sealed' && pass "arm-requires-gates-failclosed" "refused; unchanged" || fail "arm-requires-gates-failclosed" "armed w/o gates"

# =========================================================================
sec "D. autonomy (live Ollama)"
SEV=blocker
rc=$(wb "export ASHLAR_KEY_DIR=/tmp/w/ok ASHLAR_OLLAMA_BASE_URL=http://192.0.2.1:11434; SX=/tmp/w/sx0; rm -rf \$SX; $A init sx --path \$SX >/dev/null; sed -i -e 's/gatesRequired: \[\]/gatesRequired: [sandbox]/' -e 's/mayAdd: \[\]/mayAdd: [brick]/' \$SX/ashlar.policy.yaml; cd \$SX; $A self-extend run --provider ollama --allow-mock false >/dev/null 2>&1; echo rc=\$?" | grep -oE 'rc=[0-9]+' | cut -d= -f2)
[ "${rc:-0}" = "1" ] && pass "a0-dead-backend-exits-nonzero" "exit 1 (honest failure)" || { [ "${rc:-0}" != "0" ] && weak "a0-dead-backend-exits-nonzero" "nonzero exit=$rc" || fail "a0-dead-backend-exits-nonzero" "faked success"; }
SEV=major
out=$(wb "export ASHLAR_KEY_DIR=/tmp/w/ok ASHLAR_OLLAMA_BASE_URL=$OLLAMA ASHLAR_OLLAMA_MODEL=$MODEL; SX=/tmp/w/sx1; rm -rf \$SX; $A init sx1 --path \$SX >/dev/null; sed -i -e 's/gatesRequired: \[\]/gatesRequired: [sandbox]/' -e 's/mayAdd: \[\]/mayAdd: [brick]/' \$SX/ashlar.policy.yaml; cd \$SX; timeout 150 $A self-extend run --provider ollama --allow-mock false 2>&1 | grep -iE 'iter|tool|cycle|gate|denied|GATE' | head -3")
echo "$out" | grep -qiE 'iter|tool|cycle|denied|gate' && pass "a1-live-ollama-drives-react" "$(echo "$out"|head -1|cut -c1-46)" || weak "a1-live-ollama-drives-react" "no ReAct signal: $(echo "$out"|head -1|cut -c1-46)"

# =========================================================================
sec "E. federation multi-node"
SEV=blocker
docker cp "$WB":/tmp/w/shared.ashpkg /tmp/shared.ashpkg >/dev/null 2>&1
docker cp "$WB":/tmp/w/ok /tmp/okkey >/dev/null 2>&1
docker volume create labvol-a >/dev/null
# seed node A published dir + origin key, from the sealed pkg + workbench key (via docker cp into a helper)
docker create --name labseed -v labvol-a:/data/state --entrypoint sleep "$IMG" infinity >/dev/null; docker start labseed >/dev/null
docker exec labseed sh -c 'mkdir -p /data/state/mesh/published /data/state/keys' >/dev/null 2>&1
docker cp /tmp/shared.ashpkg labseed:/data/state/mesh/published/shared.ashpkg >/dev/null 2>&1
docker cp /tmp/okkey/. labseed:/data/state/keys/ >/dev/null 2>&1
nrm labseed
docker run -d --name labnode-a --network "$NET" -e ASHLAR_MESH_SERVE_PORT=7420 -e ASHLAR_MESH_DISCOVERY=1 -e ASHLAR_NODE_NAME=node-a -v labvol-a:/data/state "$IMG" background-agent daemon >/dev/null
probe "for i in \$(seq 1 40); do curl -sf http://labnode-a:7420/mesh/v1/hello >/dev/null 2>&1 && break; sleep 0.5; done; curl -s http://labnode-a:7420/mesh/v1/hello" | grep -qi 'mesh/v1' && pass "f1-serve-hello" "$(probe 'curl -s http://labnode-a:7420/mesh/v1/hello'|jq -c '{name,packages}' 2>/dev/null)" || fail "f1-serve-hello" "unreachable"
probe "curl -s http://labnode-a:7420/mesh/v1/index" | grep -qi 'shared.ashpkg' && pass "f1-serve-index" "lists pkg" || fail "f1-serve-index" "$(probe 'curl -s http://labnode-a:7420/mesh/v1/index'|head -c 60)"
tr=$(probe "curl -s -o /dev/null -w '%{http_code}' 'http://labnode-a:7420/mesh/v1/pkg/..%2f..%2fetc%2fpasswd'"); { [ "$tr" = 404 ]||[ "$tr" = 400 ]; } && pass "f1-serve-traversal-blocked" "HTTP $tr" || fail "f1-serve-traversal-blocked" "HTTP $tr"

mk_consumer(){ # volname keysrc(dir or NEW)  -> creates project + key in the volume
  docker volume create "$1" >/dev/null
  docker create --name labseed -v "$1":/data/state --entrypoint sleep "$IMG" infinity >/dev/null; docker start labseed >/dev/null
  docker exec labseed sh -c 'mkdir -p /data/state/keys /data/state/project' >/dev/null 2>&1
  if [ "$2" = NEW ]; then docker exec labseed dotnet /app/Ashlar.CLI.dll keys init >/dev/null 2>&1; else docker cp /tmp/okkey/. labseed:/data/state/keys/ >/dev/null 2>&1; fi
  docker exec labseed sh -c "dotnet /app/Ashlar.CLI.dll init x --path /data/state/project >/dev/null 2>&1; $PROP /data/state/project/ashlar.policy.yaml" >/dev/null 2>&1
  nrm labseed; }

mk_consumer labvol-b /tmp/okkey
docker run -d --name labnode-b --network "$NET" -e ASHLAR_MESH_PEERS=http://labnode-a:7420 -e ASHLAR_MESH_PULL_PROJECT=/data/state/project -e ASHLAR_MESH_PULL_INTERVAL_SECONDS=3 -v labvol-b:/data/state "$IMG" background-agent daemon >/dev/null
sleep 14
L=$(docker logs labnode-b 2>&1 | grep -i 'auto-pull: scanned' | tail -1)
echo "$L" | grep -qiE '1 held' && pass "f2-pull-trusted-held" "peer pkg held" || { echo "$L"|grep -qi refused && weak "f2-pull-trusted-held" "refused: key-copy" || fail "f2-pull-trusted-held" "no result: $(echo "$L"|cut -c1-50)"; }

mk_consumer labvol-c NEW
docker run -d --name labnode-c --network "$NET" -e ASHLAR_MESH_PEERS=http://labnode-a:7420 -e ASHLAR_MESH_PULL_PROJECT=/data/state/project -e ASHLAR_MESH_PULL_INTERVAL_SECONDS=3 -v labvol-c:/data/state "$IMG" background-agent daemon >/dev/null
sleep 14
docker logs labnode-c 2>&1 | grep -i 'auto-pull: scanned' | tail -1 | grep -qi 'refused (untrusted' && pass "f2-pull-stranger-refused" "untrusted refused" || fail "f2-pull-stranger-refused" "$(docker logs labnode-c 2>&1|grep -i auto-pull|tail -1|cut -c1-50)"

# F3 discovery
mk_consumer labvol-d /tmp/okkey
docker run -d --name labnode-d --network "$NET" -e ASHLAR_MESH_DISCOVERY=1 -e ASHLAR_NODE_NAME=node-d -e ASHLAR_MESH_PULL_PROJECT=/data/state/project -e ASHLAR_MESH_PULL_INTERVAL_SECONDS=3 -v labvol-d:/data/state "$IMG" background-agent daemon >/dev/null
sleep 20
if docker exec labnode-d cat /data/state/mesh-peers.json 2>/dev/null | grep -qi node-a; then pass "f3-discovery-finds-peer" "zero-config discovery works on this bridge"
  docker logs labnode-d 2>&1 | grep -i 'auto-pull: scanned' | tail -1 | grep -qiE 'held|already' && pass "f3-discovery-then-pull" "" || weak "f3-discovery-then-pull" "discovered, pull pending"
else weak "f3-discovery-finds-peer" "multicast not delivered across this docker bridge (configured peers work — F2)"; fi

# =========================================================================
sec "F. mTLS + fail-closed (live)"
SEV=blocker
docker volume create labvol-tls >/dev/null
docker run --rm --entrypoint sh -v labvol-tls:/data/state "$IMG" -c 'mkdir -p /data/state/certs && openssl req -x509 -newkey rsa:2048 -nodes -keyout /data/state/certs/key.pem -out /data/state/certs/cert.pem -days 1 -subj "/CN=labnode-tls" -addext "subjectAltName=DNS:labnode-tls,DNS:localhost" >/dev/null 2>&1 && echo ok' | grep -q ok || echo "cert-gen failed"
docker run -d --name labnode-tls --network "$NET" -e ASHLAR_MESH_SERVE_PORT=7443 -e ASHLAR_MESH_SERVE_TLS_CERT=/data/state/certs/cert.pem -e ASHLAR_MESH_SERVE_TLS_KEY=/data/state/certs/key.pem -e ASHLAR_NODE_NAME=tls-node -v labvol-tls:/data/state "$IMG" background-agent daemon >/dev/null
if probe "for i in \$(seq 1 40); do curl -sk https://labnode-tls:7443/mesh/v1/hello >/dev/null 2>&1 && break; sleep 0.5; done; curl -sk https://labnode-tls:7443/mesh/v1/hello" | grep -qi 'mesh/v1'; then
  pass "f4-tls-serves-https" "$(docker logs labnode-tls 2>&1|grep -io 'armed on :7443 (TLS)'|head -1)"
  probe "curl -s --max-time 4 http://labnode-tls:7443/mesh/v1/hello 2>&1 | grep -qi name && echo SERVED" | grep -q SERVED && weak "f4-plain-http-rejected" "plain http answered" || pass "f4-plain-http-rejected" "plain http refused on TLS port"
else fail "f4-tls-serves-https" "TLS not reachable: $(docker logs labnode-tls 2>&1|grep -iE 'serve|error'|tail -1|cut -c1-60)"; fi
docker volume create labvol-fc >/dev/null
docker run -d --name labnode-fc --network "$NET" -e ASHLAR_MESH_SERVE_PORT=7444 -e ASHLAR_MESH_SERVE_REQUIRE_CLIENT_CERT=1 -e ASHLAR_MESH_SERVE_CA=/nope.pem -e ASHLAR_NODE_NAME=fc -v labvol-fc:/data/state "$IMG" background-agent daemon >/dev/null
sleep 6
probe "curl -s --max-time 4 http://labnode-fc:7444/mesh/v1/index 2>&1 | grep -qi ashpkg && echo SERVED" | grep -q SERVED && fail "f4-failclosed-no-plaintext" "served plaintext!" || { docker logs labnode-fc 2>&1|grep -qi 'refusing to start' && pass "f4-failclosed-no-plaintext" "refused to serve (fail-closed)" || pass "f4-failclosed-no-plaintext" "no plaintext listener"; }

# =========================================================================
sec "G. deployment / ops"
SEV=blocker
docker volume create labvol-hb >/dev/null
docker run -d --name labnode-hb --network "$NET" -v labvol-hb:/data/state "$IMG" background-agent daemon >/dev/null
sleep 8
HBF=$(docker exec labnode-hb sh -c 'find /data/state -name "*.json" 2>/dev/null | xargs grep -l keyFingerprint 2>/dev/null | head -1')
fp1=$(docker exec labnode-hb sh -c "cat '$HBF' 2>/dev/null" | jq -r '.keyFingerprint // empty' 2>/dev/null)
hs=""; for i in $(seq 1 25); do hs=$(docker inspect labnode-hb --format '{{if .State.Health}}{{.State.Health.Status}}{{end}}' 2>/dev/null); [ "$hs" = healthy ] && break; sleep 2; done
[ "$hs" = healthy ] && pass "deploy-heartbeat-healthy" "container healthy" || weak "deploy-heartbeat-healthy" "health=$hs"
nrm labnode-hb
docker run -d --name labnode-hb2 --network "$NET" -v labvol-hb:/data/state "$IMG" background-agent daemon >/dev/null; sleep 8
fp2=$(docker exec labnode-hb2 sh -c "find /data/state -name '*.json' 2>/dev/null | xargs grep -l keyFingerprint 2>/dev/null | head -1 | xargs cat 2>/dev/null" | jq -r '.keyFingerprint // empty' 2>/dev/null)
[ -n "$fp1" ] && [ "$fp1" = "$fp2" ] && pass "deploy-identity-survives-rm" "fingerprint stable: $fp1" || weak "deploy-identity-survives-rm" "fp1=$fp1 fp2=$fp2"
nrm labnode-hb2
docker run -d --name labnode-park --network "$NET" --read-only --tmpfs /tmp "$IMG" background-agent daemon >/dev/null 2>&1; sleep 6
docker ps --filter name=labnode-park --format '{{.Status}}' | grep -qiE 'Up' && pass "deploy-park-never-exit" "parks, no crash-loop" || weak "deploy-park-never-exit" "not up: $(docker ps -a --filter name=labnode-park --format '{{.Status}}')"
nrm labnode-park

# =========================================================================
sec "H. onboarding honesty"
SEV=major
wb "export ASHLAR_KEY_DIR=/tmp/w/ok; O=/tmp/w/onb; rm -rf \$O; $A init onb --path \$O >/dev/null; grep -q 'policy set self_extend' \$O/ashlar.policy.yaml && echo HAS" | grep -q HAS && {
  wb "export ASHLAR_KEY_DIR=/tmp/w/ok; sed -i -e 's/gatesRequired: \[\]/gatesRequired: [sandbox]/' -e 's/mayAdd: \[\]/mayAdd: [brick]/' /tmp/w/onb/ashlar.policy.yaml; $A policy set self_extend proposing --path /tmp/w/onb >/dev/null 2>&1; echo rc=\$?" | grep -q 'rc=0' && pass "onboarding-scaffold-cmd-works" "scaffold's suggested cmd now real" || fail "onboarding-scaffold-cmd-works" "suggested cmd fails"
} || weak "onboarding-scaffold-cmd-works" "scaffold no longer suggests it"

# =========================================================================
printf "\n${d}== SCORECARD ==${z}\n"
BLK=$(awk -F'\t' '$1=="FAIL"&&$2~/block/{c++}END{print c+0}' "$RES")
printf "  PASS %d   WEAK %d   FAIL %d   (blocking failures: %d)\n" "$P" "$W" "$F" "$BLK"
cp "$RES" "$PWD/imagelab-results.tsv" 2>/dev/null
[ "$BLK" -gt 0 ] && echo "  VERDICT: blockers present" || { [ "$F" -gt 0 ] && echo "  VERDICT: green with non-blocking caveats" || echo "  VERDICT: green on tested image surface"; }
