#!/usr/bin/env bash
set -euo pipefail
UNITY="/Applications/Unity/Hub/Editor/6000.2.6f1/Unity.app/Contents/MacOS/Unity"
PROJ="$(cd "$(dirname "${BASH_SOURCE[0]}")/../DirectorStudioUnity" && pwd)"
ART="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
ASM="NexoDirectorStudio.Tests.PlayMode"
mkdir -p "$ART"
"$UNITY" -batchmode -nographics -projectPath "$PROJ" \
  -runTests -testPlatform PlayMode -assemblyNames "$ASM" \
  -testResults "$ART/playmode-results.xml" -logFile "$ART/unity-playmode.log" -quit