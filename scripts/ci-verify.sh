#!/usr/bin/env bash
set -euo pipefail

# ---------- Config (env-overridable) ----------
UNITY_BIN="${UNITY_BIN:-/Applications/Unity/Hub/Editor/6000.2.6f1/Unity.app/Contents/MacOS/Unity}"
PROJ="${PROJ:-$(cd "$(dirname "${BASH_SOURCE[0]}")/../DirectorStudioUnity" && pwd)}"
ART="${ART:-$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)}"
ASM="${ASM:-NexoDirectorStudio.Tests.PlayMode}"
PROMPT="${PROMPT:-short FPS room with a switch and a door}"
SMOKE_SECONDS="${SMOKE_SECONDS:-10}"

UTF_XML="$ART/playmode-results.xml"
UTF_LOG="$ART/unity-playmode.log"
SMOKE_JSON="$ART/playmode-smoke.json"
SMOKE_JUNIT="$ART/playmode-smoke.junit.xml"
SMOKE_LOG="$ART/unity-smoke.log"

mkdir -p "$ART"

# ---------- Try UTF PlayMode ----------
echo "==> UTF PlayMode: $UNITY_BIN"
set +e
"$UNITY_BIN" -batchmode -nographics \
  -projectPath "$PROJ" \
  -runTests -testPlatform PlayMode \
  -assemblyNames "$ASM" \
  -testResults "$UTF_XML" \
  -logFile "$UTF_LOG" \
  -quit
UTF_EXIT=$?
set -e

if [[ -s "$UTF_XML" ]]; then
  echo "✅ UTF PlayMode: XML produced → $UTF_XML (exit=$UTF_EXIT)"
  exit "$UTF_EXIT"
fi
echo "⚠️  UTF PlayMode produced no XML; falling back to smoke..."

# ---------- Smoke (Editor, no PlayMode) ----------
"$UNITY_BIN" -batchmode -nographics \
  -projectPath "$PROJ" \
  -executeMethod NexoDirectorStudio.Editor.CI.CiEditorSmoke.Run \
  -prompt "$PROMPT" \
  -seconds "$SMOKE_SECONDS" \
  -results "$SMOKE_JSON" \
  -acceptance "Assets/NexoDirectorStudio/Acceptance/Default.asset" \
  -logFile "$SMOKE_LOG" \
  -quit
SMOKE_EXIT=$?

# Prefer JUnit from smoke; print short verdict
if [[ -f "$SMOKE_JUNIT" ]]; then
  echo "🧪 Smoke JUnit: $SMOKE_JUNIT"
  if [[ -f "$SMOKE_JSON" ]]; then
    OK=$(jq -r '.ok' "$SMOKE_JSON" 2>/dev/null || echo "")
    TRIG=$(jq -r '.interactionsTriggered' "$SMOKE_JSON" 2>/dev/null || echo "")
    echo "Smoke verdict: ok=${OK:-?}, interactions=${TRIG:-?}, exit=$SMOKE_EXIT"
  fi
  exit "$SMOKE_EXIT"
fi

# Last resort: show JSON and exit with Unity code
[[ -f "$SMOKE_JSON" ]] && (echo "🧪 Smoke JSON:"; cat "$SMOKE_JSON") || true
exit "$SMOKE_EXIT"
