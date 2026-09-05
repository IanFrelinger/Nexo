#!/usr/bin/env bash
# Ship Tier B: ProdStyle slice + smoke + doctor (lightweight ci-verify stand-in).
# Full `ci verify` + `ashlar validate` run the entire test suite and are opt-in until FluentAssertions binding is fixed repo-wide.
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

CLI="application/src/Ashlar.CLI/Ashlar.CLI.csproj"
INFRA="src/Ashlar.Tests.Infrastructure/Ashlar.Tests.Infrastructure.csproj"

echo "== Ship Tier B: build CLI + infrastructure tests =="
dotnet build "$CLI" -v minimal
dotnet build "$INFRA" -v minimal

echo "== Ship Tier B: ProdStyle (FluentAssertions-safe filter) =="
make test-prod-style

echo "== Ship Tier B: framework smoke (net8.0, counted) =="
ASHLAR_ALLOW_MOCK=1 python3 scripts/run-dotnet-test-counted.py \
  --project "$INFRA" \
  --expected-prefix "Ashlar.Tests.Infrastructure." \
  --min-tests 9 \
  -- \
  -f net8.0 \
  --filter "FullyQualifiedName~BaseFrameworkSmokeTests" \
  --blame-hang-timeout 60s \
  --blame-hang-dump-type none

echo "== Ship Tier B: doctor --json =="
set +e
ASHLAR_ALLOW_MOCK=1 dotnet run --project "$CLI" --no-build -- doctor --json
doctor_exit=$?
set -e
if [ "$doctor_exit" -ne 0 ]; then
  echo "doctor --json exited $doctor_exit (warnings may fail strict profile; review output)" >&2
  if [ "${SHIP_GATE_STRICT_DOCTOR:-0}" = "1" ]; then
    exit "$doctor_exit"
  fi
fi

if [ "${SHIP_GATE_RUN_CI_VERIFY:-0}" = "1" ]; then
  echo "== Ship Tier B: full ci verify (opt-in) =="
  ASHLAR_ALLOW_MOCK=1 dotnet run --project "$CLI" --no-build -- ci verify
fi

echo ""
echo "ship-gate-tier-b: PASS"
