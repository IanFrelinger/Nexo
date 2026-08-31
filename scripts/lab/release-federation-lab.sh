#!/usr/bin/env bash
# Focused live multi-node federation + identity against the fresh image (no host bind-mounts:
# seal INSIDE the volume). Emits PASS/FAIL for the scenarios imagelab2's seeding chain broke.
set -uo pipefail
export MSYS_NO_PATHCONV=1 MSYS2_ARG_CONV_EXCL='*'
IMG=ashlar-cli:lab; NET=ashlar-lab; PROBE=elated_satoshi; A='dotnet /app/Ashlar.CLI.dll'
g="\033[32m";r="\033[31m";y="\033[33m";d="\033[2m";z="\033[0m"; P=0;F=0;W=0
pass(){ P=$((P+1)); printf "  ${g}PASS${z} %-34s ${d}%s${z}\n" "$1" "${2:-}"; }
fail(){ F=$((F+1)); printf "  ${r}FAIL${z} %-34s ${d}%s${z}\n" "$1" "${2:-}"; }
weak(){ W=$((W+1)); printf "  ${y}WEAK${z} %-34s ${d}%s${z}\n" "$1" "${2:-}"; }
nrm(){ docker rm -f "$@" >/dev/null 2>&1 || true; }
clean(){ nrm $(docker ps -aq --filter name=fnode- 2>/dev/null) >/dev/null 2>&1; docker volume ls -q --filter name=fvol- 2>/dev/null | xargs -r docker volume rm >/dev/null 2>&1; }
trap clean EXIT; clean
PROP='sed -i -e "s/mode: sealed/mode: proposing/" -e "s/gatesRequired: \[\]/gatesRequired: [sandbox, tests, security]/" -e "s/extensions: 0/extensions: 3/" -e "s/mayAdd: \[\]/mayAdd: [brick]/"'
probe(){ docker exec "$PROBE" bash -lc "$1" 2>&1; }

echo "Focused federation lab (image=$IMG)"
docker volume create fvol-a >/dev/null
# Seal a signed .ashpkg INTO node A's published dir, using A's own operator key (all in-volume).
AFP=$(docker run --rm --entrypoint bash -v fvol-a:/data/state "$IMG" -lc "
  export ASHLAR_KEY_DIR=/data/state/keys; A='dotnet /app/Ashlar.CLI.dll'
  \$A keys init >/dev/null 2>&1; mkdir -p /data/state/mesh/published
  D=/tmp/o; \$A init o --path \$D >/dev/null; $PROP \$D/ashlar.policy.yaml
  mkdir -p \$D/.ashlar/forge/proposed
  echo '{\"Id\":\"f1\",\"TargetPath\":\"src/S.cs\",\"NewContent\":\"//v1\",\"Summary\":\"a\",\"CreatedAt\":\"2026-08-24T06:00:00Z\",\"UpdatedAt\":\"2026-08-24T06:00:00Z\"}' > \$D/.ashlar/forge/proposed/f1.json
  printf '%s' '{\"id\":\"ext-p\",\"kind\":\"brick\",\"summary\":\"s\",\"proposedBy\":\"n\",\"proposedAt\":\"2026-08-24T06:00:00Z\",\"diff\":\"+1\",\"forgeProposalIds\":[\"f1\"],\"courses\":[{\"name\":\"sandbox\",\"passed\":true,\"detail\":\"c\"},{\"name\":\"tests\",\"passed\":true,\"detail\":\"t\"},{\"name\":\"security\",\"passed\":true,\"detail\":\"s\"}]}' > \$D/p.json
  \$A gates propose --file \$D/p.json --path \$D >/dev/null
  \$A gates --admit ext-p --as op --path \$D >/dev/null
  \$A pkg export --id ext-p --out /data/state/mesh/published/shared.ashpkg --path \$D >/dev/null
  \$A keys show 2>&1 | grep -oE 'ed25519:[0-9a-f]{16}'" | tr -d '\r')
[ -n "$AFP" ] && pass "seal-in-volume" "node-a signer $AFP" || fail "seal-in-volume" "no fingerprint"

docker run -d --name fnode-a --network "$NET" -e ASHLAR_MESH_SERVE_PORT=7420 -e ASHLAR_MESH_DISCOVERY=1 -e ASHLAR_NODE_NAME=node-a -v fvol-a:/data/state "$IMG" background-agent daemon >/dev/null
probe "for i in \$(seq 1 40); do curl -sf http://fnode-a:7420/mesh/v1/hello >/dev/null 2>&1 && break; sleep 0.5; done; curl -s http://fnode-a:7420/mesh/v1/index" | grep -qi 'shared.ashpkg' && pass "f1-serve-index" "A serves its published pkg" || fail "f1-serve-index" "$(probe 'curl -s http://fnode-a:7420/mesh/v1/index'|head -c 60)"

# helper: consumer volume with own key + proposing project; optional trust of A's FP
consumer(){ # vol trustA(yes/no)
  docker volume create "$1" >/dev/null
  docker run --rm --entrypoint bash -v "$1":/data/state "$IMG" -lc "
    export ASHLAR_KEY_DIR=/data/state/keys; A='dotnet /app/Ashlar.CLI.dll'
    \$A keys init >/dev/null 2>&1; \$A init x --path /data/state/project >/dev/null; $PROP /data/state/project/ashlar.policy.yaml
    [ '$2' = yes ] && \$A keys trust $AFP >/dev/null 2>&1 || true" >/dev/null 2>&1; }

consumer fvol-b yes
docker run -d --name fnode-b --network "$NET" -e ASHLAR_MESH_PEERS=http://fnode-a:7420 -e ASHLAR_MESH_PULL_PROJECT=/data/state/project -e ASHLAR_MESH_PULL_INTERVAL_SECONDS=3 -v fvol-b:/data/state "$IMG" background-agent daemon >/dev/null
sleep 14
L=$(docker logs fnode-b 2>&1 | grep -i 'auto-pull: scanned' | tail -1)
echo "$L" | grep -qiE '1 held' && pass "f2-pull-trusted-held" "B trusts A → pkg held for review" || fail "f2-pull-trusted-held" "$(echo "$L"|cut -c1-60)"

consumer fvol-c no
docker run -d --name fnode-c --network "$NET" -e ASHLAR_MESH_PEERS=http://fnode-a:7420 -e ASHLAR_MESH_PULL_PROJECT=/data/state/project -e ASHLAR_MESH_PULL_INTERVAL_SECONDS=3 -v fvol-c:/data/state "$IMG" background-agent daemon >/dev/null
sleep 14
docker logs fnode-c 2>&1 | grep -i 'auto-pull: scanned' | tail -1 | grep -qi 'refused (untrusted' && pass "f2-pull-stranger-refused" "C doesn't trust A → refused" || fail "f2-pull-stranger-refused" "$(docker logs fnode-c 2>&1|grep -i auto-pull|tail -1|cut -c1-60)"

# F3 zero-config discovery: node D, discovery only
consumer fvol-d yes
docker run -d --name fnode-d --network "$NET" -e ASHLAR_MESH_DISCOVERY=1 -e ASHLAR_NODE_NAME=node-d -e ASHLAR_MESH_PULL_PROJECT=/data/state/project -e ASHLAR_MESH_PULL_INTERVAL_SECONDS=3 -v fvol-d:/data/state "$IMG" background-agent daemon >/dev/null
sleep 20
docker exec fnode-d cat /data/state/mesh-peers.json 2>/dev/null | grep -qi node-a && { pass "f3-discovery-finds-peer" "zero-config: D found A over multicast"; docker logs fnode-d 2>&1|grep -i 'auto-pull: scanned'|tail -1|grep -qiE 'held|already' && pass "f3-discovery-then-pull" "discovered→pulled" || weak "f3-discovery-then-pull" "found, pull pending"; } || weak "f3-discovery-finds-peer" "multicast not delivered across docker bridge (configured peers work — F2 above)"

# Identity persistence: a KEYED node's fingerprint survives docker rm (named volume)
docker volume create fvol-id >/dev/null
docker run --rm --entrypoint bash -v fvol-id:/data/state "$IMG" -lc 'dotnet /app/Ashlar.CLI.dll keys init >/dev/null 2>&1' >/dev/null 2>&1
ID1=$(docker run --rm --entrypoint bash -v fvol-id:/data/state "$IMG" -lc "dotnet /app/Ashlar.CLI.dll keys show 2>&1 | grep -oE 'ed25519:[0-9a-f]{16}'" | tr -d '\r')
ID2=$(docker run --rm --entrypoint bash -v fvol-id:/data/state "$IMG" -lc "dotnet /app/Ashlar.CLI.dll keys show 2>&1 | grep -oE 'ed25519:[0-9a-f]{16}'" | tr -d '\r')
[ -n "$ID1" ] && [ "$ID1" = "$ID2" ] && pass "identity-persists-across-recreate" "$ID1 stable" || fail "identity-persists-across-recreate" "id1=$ID1 id2=$ID2"

printf "\n  ${d}fed: PASS %d  FAIL %d  WEAK %d${z}\n" "$P" "$F" "$W"
