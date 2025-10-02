#!/usr/bin/env bash
set -euo pipefail

UNITY="/Applications/Unity/Hub/Editor/6000.2.6f1/Unity.app/Contents/MacOS/Unity"
PROJ="$(cd "$(dirname "${BASH_SOURCE[0]}")/../DirectorStudioUnity" && pwd)"
ART="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

echo "==> Running functional validation on real generated scene..."
"$UNITY" -batchmode -nographics \
  -projectPath "$PROJ" \
  -executeMethod NexoDirectorStudio.Editor.CI.CiEditorSmokeRealScene.Run \
  -seconds 15 \
  -results "$ART/playmode-smoke-real.json" \
  -acceptance "Assets/NexoDirectorStudio/Acceptance/Default.asset" \
  -logFile "$ART/unity-real-scene-smoke.log" \
  -quit

echo "==> Results:"
echo "JSON: $ART/playmode-smoke-real.json"
echo "JUnit: $ART/playmode-smoke-real.junit.xml"
echo "Functional: $ART/functional_smoke_real.json"

if [[ -f "$ART/playmode-smoke-real.json" ]]; then
  echo "Smoke JSON:"
  cat "$ART/playmode-smoke-real.json"
fi

if [[ -f "$ART/functional_smoke_real.json" ]]; then
  echo "Functional JSON:"
  cat "$ART/functional_smoke_real.json"
fi
