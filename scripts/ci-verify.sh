#!/usr/bin/env bash
set -euo pipefail

UNITY="/Applications/Unity/Hub/Editor/6000.2.6f1/Unity.app/Contents/MacOS/Unity"
PROJ="$(cd "$(dirname "${BASH_SOURCE[0]}")/../DirectorStudioUnity" && pwd)"
ART="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
ASM="NexoDirectorStudio.Tests.PlayMode"

XML="$ART/playmode-results.xml"
LOG_UTF="$ART/unity-playmode.log"
JSON="$ART/playmode-smoke.json"
SMOKE_JUNIT="$ART/playmode-smoke.junit.xml"
LOG_SMOKE="$ART/unity-smoke.log"

echo "==> [1/2] Running UTF PlayMode tests (Unity 6)..."
set +e
"$UNITY" -batchmode -nographics \
  -projectPath "$PROJ" \
  -runTests -testPlatform PlayMode \
  -assemblyNames "$ASM" \
  -testResults "$XML" \
  -logFile "$LOG_UTF" \
  -quit
UTF_EXIT=$?
set -e

if [[ -s "$XML" ]]; then
  echo "✅ UTF PlayMode produced JUnit XML: $XML"
  exit $UTF_EXIT
fi

echo "⚠️  UTF PlayMode did not produce XML. Falling back to editor smoke..."

"$UNITY" -batchmode -nographics \
  -projectPath "$PROJ" \
  -executeMethod NexoDirectorStudio.Editor.CI.CiEditorSmoke.Run \
  -prompt "short FPS room with a switch and a door" \
  -seconds 10 \
  -results "$JSON" \
  -logFile "$LOG_SMOKE" \
  -quit
SMOKE_EXIT=$?

# Prefer JUnit XML from smoke for CI parsers; show JSON too
if [[ -f "$SMOKE_JUNIT" ]]; then
  echo "🧪 Smoke JUnit XML: $SMOKE_JUNIT"
  [[ -f "$JSON" ]] && echo "🧪 Smoke JSON:" && cat "$JSON" || true
  exit $SMOKE_EXIT
fi

# Fallback: if JUnit missing but JSON exists, still exit with proper code
[[ -f "$JSON" ]] && echo "🧪 Smoke JSON:" && cat "$JSON" || true
exit $SMOKE_EXIT
