#!/usr/bin/env bash
# Application Tier B: CLI unit tests (focused) + doctor report.
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

CLI_TESTS="application/src/Nexo.Tests.CLI/Nexo.Tests.CLI.csproj"

echo "== Application Tier B: CLI command tests =="
dotnet build "$CLI_TESTS" -v minimal
NEXO_ALLOW_MOCK=1 dotnet test "$CLI_TESTS" -f net8.0 --no-build \
  --filter "FullyQualifiedName~ValidateCommandTests|FullyQualifiedName~TrustCommandTests|FullyQualifiedName~WorkflowCommandTests" \
  --blame-hang-timeout 120s --blame-hang-dump-type none

echo "== Application Tier B: doctor --json =="
set +e
NEXO_ALLOW_MOCK=1 dotnet run --project application/src/Nexo.CLI/Nexo.CLI.csproj -- doctor --json
doctor_exit=$?
set -e
if [ "$doctor_exit" -ne 0 ]; then
  echo "doctor --json exited $doctor_exit (warnings may fail strict profile; review output)" >&2
  if [ "${APPLICATION_GATE_STRICT_DOCTOR:-0}" = "1" ]; then
    exit "$doctor_exit"
  fi
fi

echo ""
echo "application-gate-tier-b: PASS"
