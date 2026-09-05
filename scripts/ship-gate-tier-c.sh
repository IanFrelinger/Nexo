#!/usr/bin/env bash
# Ship Tier C: release preflight (pack graph + NuGet consumer sample).
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

# Preflight is fail-closed on canonical semver (VERSION file). A dummy
# prerelease such as 0.0.0-ship-gate-local is no longer accepted.
if [[ -n "${SHIP_GATE_VERSION:-}" ]]; then
  VERSION="$SHIP_GATE_VERSION"
else
  VERSION="$(tr -d '[:space:]' < VERSION)"
fi
echo "== Ship Tier C: release preflight (${VERSION}) =="
bash scripts/release-preflight-local.sh "$VERSION"

echo ""
echo "ship-gate-tier-c: PASS"
