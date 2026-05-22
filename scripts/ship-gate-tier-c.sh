#!/usr/bin/env bash
# Ship Tier C: release preflight (pack graph + NuGet consumer sample).
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

VERSION="${SHIP_GATE_VERSION:-0.0.0-ship-gate-local}"
echo "== Ship Tier C: release preflight (${VERSION}) =="
bash scripts/release-preflight-local.sh "$VERSION"

echo ""
echo "ship-gate-tier-c: PASS"
