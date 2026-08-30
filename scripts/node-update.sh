#!/usr/bin/env bash
# Re-pin deploy/node.yml to a published multi-arch image digest, and show the diff.
#
#   scripts/node-update.sh                  # pin to origin/master's sha-<12> tag
#   scripts/node-update.sh sha-0bf711f9abcd # pin to a specific master build
#   scripts/node-update.sh 0.1.0            # pin to a release tag
#
# The tag is resolved on GHCR and verified to be an index carrying BOTH linux/amd64 and
# linux/arm64 before anything is rewritten — a single-arch pin bricks the other half of the
# fleet with "no matching manifest", and it does so on the machine you are not standing at.
# Nothing is committed; the diff is printed for review.
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
NODE_YML="${REPO_ROOT}/deploy/node.yml"
IMAGE="ghcr.io/ianfrelinger/nexo-cli"

TAG="${1:-}"
if [ -z "${TAG}" ]; then
  SHORT="$(git -C "${REPO_ROOT}" rev-parse --short=12 origin/master)"
  TAG="sha-${SHORT}"
  echo "no tag given; using origin/master -> ${TAG}"
fi

if ! INSPECT="$(docker buildx imagetools inspect "${IMAGE}:${TAG}" 2>&1)"; then
  echo "ERROR: ${IMAGE}:${TAG} not found on GHCR — has the publish workflow for that commit finished?" >&2
  exit 1
fi
for p in "linux/amd64" "linux/arm64"; do
  if ! grep -q "${p}" <<<"${INSPECT}"; then
    echo "ERROR: ${IMAGE}:${TAG} is missing ${p} — refusing a pin that bricks half the fleet" >&2
    exit 1
  fi
done
DIGEST="$(awk '/^Digest:/ {print $2; exit}' <<<"${INSPECT}")"
if [ -z "${DIGEST}" ]; then
  echo "ERROR: could not read the index digest from imagetools output" >&2
  exit 1
fi

sed -i.bak -E \
  -e "s|^([[:space:]]*)#[[:space:]]*(sha-[0-9a-f]{12}\|v?[0-9]+\.[0-9]+\.[0-9]+), verified to carry.*$|\\1# ${TAG}, verified to carry both linux/amd64 and linux/arm64.|" \
  -e "s|^([[:space:]]*image:[[:space:]]*)${IMAGE}@sha256:[0-9a-f]{64}[[:space:]]*$|\\1${IMAGE}@${DIGEST}|" \
  "${NODE_YML}"
rm -f "${NODE_YML}.bak"

echo
git -C "${REPO_ROOT}" --no-pager diff -- deploy/node.yml
echo
echo "review the diff, then roll it out:"
echo "  docker compose -f deploy/node.yml pull && docker compose -f deploy/node.yml up -d"
echo "  (whole fleet: scripts/fleet-update.sh)"
