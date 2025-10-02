#!/usr/bin/env bash
set -euo pipefail

UNITY="/Applications/Unity/Hub/Editor/6000.2.6f1/Unity.app/Contents/MacOS/Unity"
PROJ="/Users/ianfrelinger/CursorProjects/Nexo/DirectorStudioUnity"
ART="/Users/ianfrelinger/CursorProjects/Nexo"
mkdir -p "$ART"

echo "🎮 Starting Interactive Game Specification Wizard"
echo "================================================"
echo ""
echo "This wizard will guide you through creating a detailed game specification"
echo "using Socratic dialogue - I'll ask thoughtful questions to help you discover"
echo "and define your game vision step by step."
echo ""
echo "Ready to begin? The process will take about 5-10 minutes."
echo ""

"$UNITY" \
  -batchmode -nographics \
  -projectPath "$PROJ" \
  -executeMethod NexoDirectorStudio.Editor.InteractiveGameSpecWizard.RunWizard \
  -logFile "$ART/unity-interactive-spec-wizard.log" \
  -quit

echo ""
echo "📊 Interactive Game Specification Wizard Results:"
if [[ -f "$ART/game-specification.json" ]]; then
  echo "✅ Specification saved to: $ART/game-specification.json"
  echo "📄 Content preview:"
  head -30 "$ART/game-specification.json"
else
  echo "❌ No specification file generated"
fi

echo ""
echo "📋 Log file: $ART/unity-interactive-spec-wizard.log"
