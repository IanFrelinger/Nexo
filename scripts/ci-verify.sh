#!/usr/bin/env bash
set -euo pipefail

# Paths (adjust if yours differ)
UNITY="/Applications/Unity/Hub/Editor/6000.2.6f1/Unity.app/Contents/MacOS/Unity"
PROJ="$(cd "$(dirname "${BASH_SOURCE[0]}")/../DirectorStudioUnity" && pwd)"
ART="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
ASM="NexoDirectorStudio.Tests.PlayMode"

XML="$ART/playmode-results.xml"
LOG_UTF="$ART/unity-playmode.log"
JSON="$ART/playmode-smoke.json"
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
  echo "✅ UTF PlayMode produced XML: $XML (exit=$UTF_EXIT)"
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

# Exit code from Unity indicates pass/fail for smoke runner
SMOKE_EXIT=$?
if [[ -f "$JSON" ]]; then
  echo "🧪 Smoke results:"
  cat "$JSON" || true
fi
echo "Logs: $LOG_UTF (UTF), $LOG_SMOKE (smoke)"
exit $SMOKE_EXIT
