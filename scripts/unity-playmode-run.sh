#!/usr/bin/env bash
set -euo pipefail

UNITY="/Applications/Unity/Hub/Editor/2022.3.62f1/Unity.app/Contents/MacOS/Unity"
PROJ="/Users/ianfrelinger/CursorProjects/Nexo/DirectorStudioUnity"
ART="/Users/ianfrelinger/CursorProjects/Nexo"

# IMPORTANT: set this to your actual PlayMode test asmdef name (from step 2)
ASM="NexoDirectorStudio.Tests.PlayMode"

mkdir -p "$ART"

"$UNITY" \
  -batchmode -nographics \
  -projectPath "$PROJ" \
  -runTests -testPlatform PlayMode \
  -assemblyNames "$ASM" \
  -testResults "$ART/playmode-results.xml" \
  -logFile "$ART/unity-playmode.log" \
  -quit
