#!/usr/bin/env bash
# Ship Tier D: release bundle (runtime SLO gate + doctor).
# Skipping the bundle used to hide a missing evidence artifact behind a
# doctor-only PASS.
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

CLI="application/src/Ashlar.CLI/Ashlar.CLI.csproj"
PROFILE="${SHIP_GATE_BUNDLE_PROFILE:-quick}"

echo "== Ship Tier D: ci release-bundle --profile ${PROFILE} =="
ASHLAR_ALLOW_MOCK=1 dotnet run --project "$CLI" -- ci release-bundle --profile "$PROFILE"

echo ""
echo "ship-gate-tier-d: PASS"
