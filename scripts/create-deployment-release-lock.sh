#!/usr/bin/env bash
# Resolve multi-arch index digests once, then deploy only immutable refs.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
REGISTRY="${DEPLOY_REGISTRY:-ghcr.io/ianfrelinger}"
TAG="${DEPLOY_TAG:-latest}"
OUT="${1:-$ROOT/deployment.release.lock}"

resolve() {
  local ref="$1" digest
  digest="$(docker buildx imagetools inspect "$ref" --format '{{.Manifest.Digest}}')"
  test -n "$digest" && printf '%s@%s' "${ref%%:*}" "$digest"
}

agent="$(resolve "$REGISTRY/nexo-dep-extract-agent:$TAG")"
tool="$(resolve "$REGISTRY/nexo-dep-extract-tool:$TAG")"
cat >"$OUT" <<EOF
# Generated $(date -u +%Y-%m-%dT%H:%M:%SZ); index-manifest digest convention.
DEPLOY_AGENT_IMAGE=$agent
DEPLOY_TOOL_IMAGE=$tool
DEPLOY_DIGEST_KIND=index
EOF
chmod 600 "$OUT"
echo "Wrote immutable release lock: $OUT"
cat "$OUT"
