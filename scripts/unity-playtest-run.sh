#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ART="$(cd "$SCRIPT_DIR/.." && pwd)"
PROJ="${PROJ:-$ART/DirectorStudioUnity}"
UNITY="${UNITY_BIN:-/Applications/Unity/Hub/Editor/6000.2.6f1/Unity.app/Contents/MacOS/Unity}"
mkdir -p "$ART"

# Default parameters
PROMPT="${1:-FPS shooter with enemies, power-ups, and keycard doors}"
DURATION="${2:-30}"
RESULTS="${3:-$ART/playtest-results.json}"

echo "🤖 Starting AI Playtest Pipeline"
echo "📝 Prompt: $PROMPT"
echo "⏱️ Duration: ${DURATION}s"
echo "📊 Results: $RESULTS"
echo ""

"$UNITY" \
  -batchmode -nographics \
  -projectPath "$PROJ" \
  -executeMethod NexoDirectorStudio.Editor.PlaytestCliRunner.Run \
  -prompt "$PROMPT" \
  -testDuration "$DURATION" \
  -enableMetrics true \
  -results "$RESULTS" \
  -logFile "$ART/unity-playtest.log" \
  -quit

echo ""
echo "📊 Playtest Results:"
if [[ -f "$RESULTS" ]]; then
  echo "✅ Results saved to: $RESULTS"
  echo "📄 Content preview:"
  head -20 "$RESULTS"
else
  echo "❌ No results file generated"
fi

echo ""
echo "📋 Log file: $ART/unity-playtest.log"
