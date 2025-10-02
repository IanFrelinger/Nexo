#!/usr/bin/env bash
set -euo pipefail
UNITY_BIN="${UNITY_BIN:-/Applications/Unity/Hub/Editor/6000.2.6f1/Unity.app/Contents/MacOS/Unity}"
PROJ="${PROJ:-$(cd "$(dirname "${BASH_SOURCE[0]}")/../DirectorStudioUnity" && pwd)}"
ART="${ART:-$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)}"

echo "🧪 Testing individual phases..."

"$UNITY_BIN" -batchmode -nographics \
  -projectPath "$PROJ" \
  -executeMethod NexoDirectorStudio.EditorCLI.TestIndividualPhases.Run \
  -logFile "$ART/unity-individual-phases.log" \
  -quit

echo "🔍 Checking individual phases log..."
grep -A 2 -B 2 "TestIndividualPhases\|PhaseRunner\|error\|Error\|ERROR" "$ART/unity-individual-phases.log" | tail -20
