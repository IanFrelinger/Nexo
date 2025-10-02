#!/usr/bin/env bash
set -euo pipefail
UNITY="/Applications/Unity/Hub/Editor/6000.2.6f1/Unity.app/Contents/MacOS/Unity"
PROJ="$(cd "$(dirname "${BASH_SOURCE[0]}")/../DirectorStudioUnity" && pwd)"
ART="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
mkdir -p "$ART"
"$UNITY" -batchmode -nographics -projectPath "$PROJ" \
  -executeMethod NexoDirectorStudio.Editor.CI.CiEditorSmoke.Run \
  -prompt "short FPS room with a switch and a door" -seconds 10 \
  -results "$ART/playmode-smoke.json" -logFile "$ART/unity-smoke.log" -quit