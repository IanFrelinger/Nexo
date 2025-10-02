#!/usr/bin/env bash
set -euo pipefail

UNITY="/Applications/Unity/Hub/Editor/6000.2.6f1/Unity.app/Contents/MacOS/Unity"
PROJ="/Users/ianfrelinger/CursorProjects/Nexo/DirectorStudioUnity"
ART="/Users/ianfrelinger/CursorProjects/Nexo"
mkdir -p "$ART"

# Default parameters
PROMPT="${1:-Simple FPS test}"

echo "🎬 Starting Director CLI Runner"
echo "📝 Prompt: $PROMPT"
echo ""

"$UNITY" \
  -batchmode -nographics \
  -projectPath "$PROJ" \
  -executeMethod NexoDirectorStudio.Editor.DirectorCliRunner.Run \
  -prompt "$PROMPT" \
  -logFile "$ART/unity-director.log" \
  -quit

echo ""
echo "📋 Log file: $ART/unity-director.log"
