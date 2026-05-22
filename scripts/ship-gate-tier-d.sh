#!/usr/bin/env bash
# Ship Tier D: release bundle or doctor-only (runtime gate is opt-in — can fail without full runtime bench data).
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

CLI="application/src/Nexo.CLI/Nexo.CLI.csproj"

if [ "${SHIP_GATE_RUN_RUNTIME_GATE:-0}" = "1" ]; then
  PROFILE="${SHIP_GATE_BUNDLE_PROFILE:-quick}"
  echo "== Ship Tier D: ci release-bundle --profile ${PROFILE} =="
  NEXO_ALLOW_MOCK=1 dotnet run --project "$CLI" -- ci release-bundle --profile "$PROFILE"
else
  echo "== Ship Tier D: release bundle skipped (SHIP_GATE_RUN_RUNTIME_GATE=1 to enable) =="
  echo "== Ship Tier D: doctor sign-off =="
  NEXO_ALLOW_MOCK=1 dotnet run --project "$CLI" -- doctor --json >/dev/null
fi

echo ""
echo "ship-gate-tier-d: PASS"
