#!/usr/bin/env bash
# Ashlar UAT — Tier 5 (deploy and operate).
#
# Checks the claims docs/DEPLOYMENT.md makes about the golden paths, against the compose files an
# operator would actually run. These are cheap: `docker compose config` resolves and normalises the
# stack without building or starting anything, so this can gate every PR. Building and booting the
# stack is a heavier, deliberate run -- see the bottom of this file.
#
# Requires the docker CLI (not a running daemon for `config`). It therefore runs on the CI runner
# directly rather than inside the SDK container the other tiers use.
set -uo pipefail

SRC="${UAT_REPO_DIR:-$PWD}"
OUT="${UAT_OUT:-${WORK:-/work}/out}"
mkdir -p "$OUT"
: >"$OUT/results-tier5.tsv"

PASS=0; FAIL=0
result() {
  local tier="$1" id="$2" verdict="$3"; shift 3
  printf 'RESULT\t%s\t%s\t%s\t%s\n' "$tier" "$id" "$verdict" "$*" | tee -a "$OUT/results-tier5.tsv"
  if [ "$verdict" = PASS ]; then PASS=$((PASS+1)); else FAIL=$((FAIL+1)); fi
}
say() { printf '\n=== %s ===\n' "$*"; }
cd "$SRC" || exit 1

PORTAL=deploy/compose/docker-compose.portal.yml

say "5.1 every shipped compose file either resolves, or refuses for a stated reason"
# Three outcomes, and only one of them is a defect:
#   resolves                        -- fine
#   "required variable X is missing" -- fine, and deliberate: the secret-bearing stacks ship no default
#                                       for ASHLAR_API_KEY / NEO4J_AUTH and say so. Refusing to start
#                                       without a secret is the behaviour we want to keep.
#   anything else                   -- schema or syntax breakage, which is the drift worth catching
# Overrides are fragments that only resolve when merged, so they are identified by the role the
# compose README gives them rather than by a filename convention -- docker-compose.agent-server.local.yml
# is an override that does not carry ".override." in its name.
BAD=""; DELIBERATE=""; SKIPPED=""
for f in deploy/compose/docker-compose.*.yml; do
  base=$(basename "$f")
  # Classify by the role the compose README gives the file, plus the .override. filename convention.
  # A "does the header mention override" heuristic was tried and rejected: it swept in
  # docker-compose.mesh-lab.yml, a full stack whose header merely mentions its overrides, and skipping
  # it would have quietly dropped coverage while the check still reported PASS.
  if grep -qE "\`$base\`[^|]*\|[^|]*Override:" deploy/compose/README.md 2>/dev/null \
     || case "$base" in *.override.yml) true;; *) false;; esac; then
    SKIPPED="$SKIPPED $base"; continue
  fi
  err=$(docker compose -f "$f" config -q 2>&1); rc=$?
  if [ "$rc" -eq 0 ]; then
    continue
  elif printf '%s' "$err" | grep -q 'required variable'; then
    DELIBERATE="$DELIBERATE $base"
  else
    BAD="$BAD $base($(printf '%s' "$err" | head -1 | cut -c1-70))"
  fi
done
if [ -z "$BAD" ]; then
  result 5 compose-files-resolve PASS "resolve or refuse cleanly; secret-gated:${DELIBERATE:- none}; overrides skipped:${SKIPPED:- none}"
else
  result 5 compose-files-resolve FAIL "broken (not a missing-secret refusal):$BAD"
fi

say "5.2 golden path A resolves, and is the file DEPLOYMENT.md names"
if [ -f "$PORTAL" ] && docker compose -f "$PORTAL" config >"$OUT/portal-config.yml" 2>"$OUT/portal-config.err"; then
  result 5 portal-resolves PASS "$PORTAL resolves"
else
  result 5 portal-resolves FAIL "$PORTAL missing or unresolvable: $(head -2 "$OUT/portal-config.err" 2>/dev/null | tr '\n' ' ')"
  echo "cannot check the golden path without a resolved config"; printf 'PASS=%d FAIL=%d\n' "$PASS" "$FAIL"; exit 1
fi

# The published ports come back normalised as host_ip/target/published triples. Read the host_ip that
# belongs to each target port rather than grepping for "127.0.0.1" anywhere in the file -- a loopback
# binding on one service must not vouch for a different service that is wide open.
host_ip_for() {
  awk -v want="$1" '
    /^[[:space:]]*-[[:space:]]*mode:/ { hip=""; tgt="" }
    /host_ip:/    { hip=$2 }
    /target:/     { tgt=$2; if (tgt == want) { print (hip == "" ? "<unset>" : hip); exit } }
  ' "$OUT/portal-config.yml"
}

say "5.3 'API and Ollama default to loopback' — the claim an operator's exposure depends on"
API_IP=$(host_ip_for 8080)
OLL_IP=$(host_ip_for 11434)
if [ "$API_IP" = "127.0.0.1" ]; then
  result 5 api-binds-loopback PASS "API 8080 published on $API_IP, as DEPLOYMENT.md golden path A says"
else
  result 5 api-binds-loopback FAIL "SECURITY: DEPLOYMENT.md says loopback; API 8080 publishes on ${API_IP:-<none>}"
fi
if [ "$OLL_IP" = "127.0.0.1" ]; then
  result 5 ollama-binds-loopback PASS "Ollama 11434 published on $OLL_IP"
else
  result 5 ollama-binds-loopback FAIL "SECURITY: DEPLOYMENT.md says loopback; Ollama 11434 publishes on ${OLL_IP:-<none>}"
fi

say "5.4 'HTTP API on port 8080'"
if grep -qE 'published: "8080"' "$OUT/portal-config.yml"; then
  result 5 api-port-8080 PASS "8080 published, as documented"
else
  result 5 api-port-8080 FAIL "no published 8080 in the resolved portal stack"
fi

say "5.5 'API image builds from .docker/Dockerfile.api', build context repo root"
if grep -q 'dockerfile: .*\.docker/Dockerfile\.api' "$OUT/portal-config.yml"; then
  result 5 api-dockerfile PASS "builds from .docker/Dockerfile.api"
else
  result 5 api-dockerfile FAIL "resolved stack does not build the API from .docker/Dockerfile.api"
fi
if [ -f .docker/Dockerfile.api ]; then
  result 5 api-dockerfile-exists PASS ".docker/Dockerfile.api is present"
else
  result 5 api-dockerfile-exists FAIL "DEPLOYMENT.md names .docker/Dockerfile.api; it does not exist"
fi

say "5.6 the stack points Ollama at the ollama SERVICE, not the API container's own loopback"
# The compose file carries an explicit comment about this; a regression would silently make the API
# dial itself and fail at first inference rather than at boot.
if grep -qE 'OLLAMA_BASE_URL: http://ollama:11434' "$OUT/portal-config.yml"; then
  result 5 ollama-service-url PASS "OLLAMA_BASE_URL points at http://ollama:11434"
else
  result 5 ollama-service-url FAIL "OLLAMA_BASE_URL is $(grep -m1 'OLLAMA_BASE_URL' "$OUT/portal-config.yml" | tr -d ' ')"
fi

say "5.7 every file DEPLOYMENT.md names actually exists"
MISSING=""
for p in $(grep -oE '`(deploy|\.docker|\.github)/[A-Za-z0-9._/-]+`' docs/DEPLOYMENT.md | tr -d '`' | sort -u); do
  [ -e "$p" ] || MISSING="$MISSING $p"
done
if [ -z "$MISSING" ]; then
  result 5 deployment-doc-paths PASS "every deploy/, .docker/ and .github/ path named in DEPLOYMENT.md resolves"
else
  result 5 deployment-doc-paths FAIL "named but absent:$MISSING"
fi

say "5.8 a path the docs call 'gitignored' is actually ignored"
# docs/SelfHostedAgentServer.md tells an operator to put a personal override -- whose own example
# mounts a secrets file -- at a path it calls gitignored. If that path is tracked instead, the
# operator's machine paths land staged for commit, and they clobber a shipped file on the way.
GI_BAD=""
for p in $(grep -oE '\*\*gitignored\*\* file such as `[^`]+`' docs/SelfHostedAgentServer.md \
           | grep -oE '`[^`]+`' | tr -d '`'); do
  sample="${p%/}/probe.yml"
  case "$p" in */) : ;; *) sample="$p" ;; esac
  if git ls-files --error-unmatch "$p" >/dev/null 2>&1; then
    GI_BAD="$GI_BAD $p(tracked)"
  elif ! git check-ignore -q "$sample" 2>/dev/null; then
    GI_BAD="$GI_BAD $sample(not-ignored)"
  fi
done
if [ -z "$GI_BAD" ]; then
  result 5 gitignored-claims-true PASS "every path the docs call gitignored is untracked and ignored"
else
  result 5 gitignored-claims-true FAIL "documented as gitignored but not:$GI_BAD"
fi

say "5.9 the k8s sample is well-formed"
K8S_BAD=""
for m in deploy/k8s/*.yaml; do
  [ -e "$m" ] || continue
  docker run --rm -i mikefarah/yq:4 e '.' - <"$m" >/dev/null 2>&1 || K8S_BAD="$K8S_BAD $(basename "$m")"
done
if [ -z "$K8S_BAD" ]; then
  result 5 k8s-manifests-parse PASS "deploy/k8s manifests parse as YAML"
else
  result 5 k8s-manifests-parse FAIL "do not parse:$K8S_BAD"
fi

say "summary"
printf 'PASS=%d FAIL=%d\n' "$PASS" "$FAIL" | tee "$OUT/summary-tier5.txt"

# Deliberately NOT here: building the API image and running `docker compose up --wait` to curl /health.
# It costs minutes per run and duplicates container-image-gate / the readiness gate's /health smoke.
# Run it by hand when the Dockerfile or the stack changes:
#   docker compose -f deploy/compose/docker-compose.portal.yml up -d --build
#   curl -s http://127.0.0.1:8080/health
[ "$FAIL" -eq 0 ]
