#!/usr/bin/env bash
# One command before you cut a release: graph alignment + NuGet consumer sample (isolated cache).
# This is not the packaging-lane external-product-shape check. Preflight can be green
# while `scripts/verify-external-product-shape.sh` still fails (that script is step 2
# of the autonomous release-manager packaging lane).
# Usage: bash scripts/release-preflight-local.sh 1.2.3
# Optional: ASHLAR_RELEASE_PREFLIGHT_TRIGGER_GATE=1  →  gh workflow run "Runtime Release Gate" --ref <branch>
#           ASHLAR_RELEASE_PREFLIGHT_REF=master     (default: current branch from git)
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
VER="${1:?usage: release-preflight-local.sh <semver>   example: bash scripts/release-preflight-local.sh 1.2.3}"
VER="${VER#v}"

# shellcheck source=scripts/lib/release-staging-guards.sh
source "${ROOT}/scripts/lib/release-staging-guards.sh"
assert_valid_semver "${VER}"
assert_version_matches_canonical "${VER}"

if [[ "${ASHLAR_RELEASE_AUDIT:-0}" == "1" && "${ASHLAR_RELEASE_PREFLIGHT_TRIGGER_GATE:-0}" == "1" ]]; then
  echo "ABORT: release audit mode never dispatches external workflows." >&2
  exit 64
fi

echo "== 1/2 Pack graph vs MSBuild (Ashlar.Hosting) =="
python3 "${ROOT}/scripts/verify-pack-ashlar-hosting-graph-alignment.py"

echo "== 2/2 NuGet consumer sample (local feed + isolated cache) =="
export ASHLAR_SDK_PACKAGE_VERSION="${VER}"
bash "${ROOT}/scripts/verify-stable-sdk-host-sample-packages.sh"

if [[ "${ASHLAR_RELEASE_PREFLIGHT_TRIGGER_GATE:-}" == "1" ]]; then
  REF="${ASHLAR_RELEASE_PREFLIGHT_REF:-$(git -C "${ROOT}" rev-parse --abbrev-ref HEAD)}"
  if command -v gh >/dev/null 2>&1; then
    echo "== Optional: triggering Runtime Release Gate on origin/${REF} =="
    gh workflow run "Runtime Release Gate" --ref "${REF}" || {
      echo "::warning::gh workflow run failed (auth or repo?). Continue without CI gate."
    }
  else
    echo "::notice::ASHLAR_RELEASE_PREFLIGHT_TRIGGER_GATE=1 but gh not installed; skip."
  fi
fi

echo ""
echo "Preflight OK for version ${VER}."
echo "Next (pick one):"
echo "  • Ship GHCR + NuGet:  git tag v${VER} && git push origin v${VER}"
echo "  • Open Actions:       gh workflow run Release --ref \$(git rev-parse --abbrev-ref HEAD) -f version=${VER}"
echo "  • Same preflight:     dotnet run --project application/src/Ashlar.CLI -- release preflight ${VER}"
echo "  • Dispatch CI:        dotnet run --project application/src/Ashlar.CLI -- release dispatch ${VER}   (or: make release-dispatch VERSION=${VER})"
echo "  • Docs:               docs/RELEASE_RUNBOOK.md  docs/PUBLISHING.md  docs/GitHubRepoVariables.md"
