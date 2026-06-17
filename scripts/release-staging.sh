#!/usr/bin/env bash
# Guarded staging-only dispatch of release.yml (no nuget.org push when PUBLISH_MODE unset/none).
#
# Usage:
#   bash scripts/release-staging.sh 0.1.0
#   make release-staging VERSION=0.1.0
#   make release-staging DRY_RUN=1 VERSION=0.1.0   # plan only, no dispatch
#
# Environment:
#   DRY_RUN=1              Print plan + assert results; do not dispatch.
#   NEXO_RELEASE_STAGING_REF Branch/ref for workflow_dispatch (default: current branch).
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
# shellcheck source=scripts/lib/release-staging-guards.sh
source "${ROOT}/scripts/lib/release-staging-guards.sh"

VERSION="${VERSION:-${1:-}}"
DRY_RUN="${DRY_RUN:-0}"
REF="${NEXO_RELEASE_STAGING_REF:-$(git -C "${ROOT}" rev-parse --abbrev-ref HEAD 2>/dev/null || echo master)}"

if [[ -z "${VERSION}" ]]; then
  echo "usage: release-staging.sh <semver>   or   make release-staging VERSION=x.y.z [DRY_RUN=1]" >&2
  exit 1
fi

VERSION="$(normalize_semver "${VERSION}")"

if [[ "${DRY_RUN}" == "1" ]]; then
  export RELEASE_STAGING_DRY_RUN=1
fi

echo "== release-staging pre-flight =="
assert_gh_authenticated
assert_valid_semver "${VERSION}"
assert_version_matches_canonical "${VERSION}"
assert_nuget_org_push_skipped
assert_staging_configured

STAGING_URL="$(gh_repo_variable NUGET_STAGING_FEED_URL 2>/dev/null | tr -d '[:space:]' || true)"
if [[ -z "${STAGING_URL}" ]]; then
  STAGING_URL="(see NUGET_STAGING_FEED_URL)"
fi

echo ""
echo "Plan:"
echo "  workflow:      release.yml (workflow_dispatch)"
echo "  version:       ${VERSION}"
echo "  ref:           ${REF}"
echo "  staging feed:  ${STAGING_URL}"
echo "  nuget.org:     skipped (staging-only guard passed)"
echo "  dry run:       ${DRY_RUN}"

if [[ "${DRY_RUN}" == "1" ]]; then
  echo ""
  echo "release-staging: DRY_RUN complete (no workflow dispatched)."
  exit 0
fi

echo ""
echo "==> Dispatching release.yml ..."
gh workflow run release.yml --ref "${REF}" -f "version=${VERSION}" -f skip_multi_arch=false

echo "Waiting for workflow run to appear ..."
sleep 5
RUN_ID=""
for _ in $(seq 1 12); do
  RUN_ID="$(gh run list --workflow=release.yml --branch="${REF}" --limit 1 --json databaseId,status -q '.[0].databaseId' 2>/dev/null || true)"
  if [[ -n "${RUN_ID}" && "${RUN_ID}" != "null" ]]; then
    break
  fi
  sleep 5
done

if [[ -z "${RUN_ID}" || "${RUN_ID}" == "null" ]]; then
  echo "ABORT: could not resolve release.yml run id after dispatch." >&2
  exit 1
fi

RUN_URL="$(gh run view "${RUN_ID}" --json url -q .url)"
echo "Watching run ${RUN_ID} (${RUN_URL}) ..."
if gh run watch "${RUN_ID}" --exit-status; then
  echo "release-staging: OK (${RUN_URL})"
else
  echo "release-staging: FAILED (${RUN_URL})" >&2
  exit 1
fi
