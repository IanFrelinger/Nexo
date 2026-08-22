#!/usr/bin/env bash
# Security Tier C: trust CLI surfaces (boundary + dashboard + audit JSON).
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

CLI="application/src/Ashlar.CLI/Ashlar.CLI.csproj"
CLI_TESTS="application/src/Ashlar.Tests.CLI/Ashlar.Tests.CLI.csproj"

echo "== Security Tier C: TrustCommand unit suite =="
dotnet build "$CLI_TESTS" -v minimal
ASHLAR_ALLOW_MOCK=1 dotnet test "$CLI_TESTS" -f net10.0 --no-build \
  --filter "FullyQualifiedName~TrustCommandTests" \
  --blame-hang-timeout 120s --blame-hang-dump-type none

echo "== Security Tier C: trust boundary + dashboard JSON smoke =="
dotnet build "$CLI" -v minimal
ASHLAR_ALLOW_MOCK=1 dotnet run --project "$CLI" --no-build -- trust boundary --format-json >/dev/null
ASHLAR_ALLOW_MOCK=1 dotnet run --project "$CLI" --no-build -- trust dashboard --format-json >/dev/null

echo ""
echo "security-gate-tier-c: PASS"
