#!/usr/bin/env bash
# Headless pass for Nexo Guide: (1) DI test, (2) app run with --headless.
# Use for CI or local validation without a display.
set -e
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

echo "=== 1. Headless DI test (execution pipeline resolves) ==="
dotnet test src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj -f net8.0 --filter "Guide_HeadlessDI" --no-build 2>/dev/null || \
  dotnet test src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj -f net8.0 --filter "Guide_HeadlessDI"
DI_EXIT=$?
if [ "$DI_EXIT" -ne 0 ]; then
  echo "FAIL: Headless DI test failed (exit $DI_EXIT)"
  exit 1
fi
echo "PASS: Headless DI test"
echo ""

echo "=== 2. Build and run Guide in headless mode ==="
dotnet build src/Nexo.Guide/Nexo.Guide.csproj -f net8.0 -p:TreatWarningsAsErrors=false -v q
dotnet run --project src/Nexo.Guide/Nexo.Guide.csproj -f net8.0 -p:TreatWarningsAsErrors=false --no-build -- --headless 2>/tmp/nexo-guide-headless.log
APP_EXIT=$?
if [ "$APP_EXIT" -ne 0 ]; then
  echo "FAIL: App headless run exited with $APP_EXIT"
  tail -40 /tmp/nexo-guide-headless.log
  exit 1
fi
if grep -q "Unable to resolve service" /tmp/nexo-guide-headless.log 2>/dev/null; then
  echo "FAIL: DI resolution error in app (see /tmp/nexo-guide-headless.log)"
  tail -30 /tmp/nexo-guide-headless.log
  exit 1
fi
echo "PASS: App headless run (exit 0)"
echo ""

echo "=== 3. Full-app test via Universal Tester agent (Guide as CLI target, headless) ==="
dotnet test src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj -f net8.0 --filter "FullyQualifiedName~GuideFullAppUniversalTesterTests" -p:TreatWarningsAsErrors=false --no-build
AGENT_EXIT=$?
if [ "$AGENT_EXIT" -ne 0 ]; then
  echo "FAIL: Universal Tester agent full-app test failed (exit $AGENT_EXIT)"
  exit 1
fi
echo "PASS: Full-app agent test"
exit 0
