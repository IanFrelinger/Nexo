#!/usr/bin/env bash
# RC Tier B: production-readiness mirror + release bundle (runtime gate + doctor).
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

CLI="application/src/Nexo.CLI/Nexo.CLI.csproj"

echo "== RC Tier B: ship gate (prod readiness + ci verify + release preflight) =="
SHIP_GATE_SKIP_PRIOR=1 make ship-gate-full

echo "== RC Tier B: release bundle (quick + runtime gate) =="
NEXO_ALLOW_MOCK=1 dotnet run --project "$CLI" -- ci release-bundle --profile quick

if [ "${RC_GATE_RELEASE_BUNDLE_FULL:-0}" = "1" ]; then
  echo "== RC Tier B: release bundle (full profile) =="
  NEXO_ALLOW_MOCK=1 dotnet run --project "$CLI" -- ci release-bundle --profile full
fi

echo ""
echo "rc-gate-tier-b: PASS"
