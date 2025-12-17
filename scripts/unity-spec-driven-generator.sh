#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ART="$(cd "$SCRIPT_DIR/.." && pwd)"
PROJ="${PROJ:-$ART/DirectorStudioUnity}"
UNITY="${UNITY_BIN:-/Applications/Unity/Hub/Editor/6000.2.6f1/Unity.app/Contents/MacOS/Unity}"
mkdir -p "$ART"

# Default parameters
SPEC_PATH="${1:-$ART/game-specification.json}"
INCLUDE_PLAYTEST="${2:-true}"
RESULTS="${3:-$ART/spec-driven-results.json}"

echo "🎮 Starting Specification-Driven Game Generator"
echo "=============================================="
echo ""
echo "📋 Using specification: $SPEC_PATH"
echo "🤖 Include playtesting: $INCLUDE_PLAYTEST"
echo "📊 Results will be saved to: $RESULTS"
echo ""

if [[ ! -f "$SPEC_PATH" ]]; then
  echo "❌ Specification file not found: $SPEC_PATH"
  echo "💡 Please run the Game Specification Wizard first:"
  echo "   ./scripts/unity-game-spec-wizard.sh"
  exit 1
fi

"$UNITY" \
  -batchmode -nographics \
  -projectPath "$PROJ" \
  -executeMethod NexoDirectorStudio.Editor.SimpleSpecDrivenGenerator.GenerateFromSpec \
  -specPath "$SPEC_PATH" \
  -includePlaytest "$INCLUDE_PLAYTEST" \
  -results "$RESULTS" \
  -logFile "$ART/unity-spec-driven-generator.log" \
  -quit

echo ""
echo "📊 Specification-Driven Generation Results:"
if [[ -f "$RESULTS" ]]; then
  echo "✅ Results saved to: $RESULTS"
  echo "📄 Content preview:"
  head -20 "$RESULTS"
else
  echo "❌ No results file generated"
fi

echo ""
echo "📋 Log file: $ART/unity-spec-driven-generator.log"
