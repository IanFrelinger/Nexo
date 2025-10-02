#!/usr/bin/env bash
set -euo pipefail
UNITY_BIN="${UNITY_BIN:-/Applications/Unity/Hub/Editor/6000.2.6f1/Unity.app/Contents/MacOS/Unity}"
PROJ="${PROJ:-$(cd "$(dirname "${BASH_SOURCE[0]}")/../DirectorStudioUnity" && pwd)}"
ART="${ART:-$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)}"
CFG="${CFG:-$ART/nexo.pipeline.json}"

"$UNITY_BIN" -batchmode -nographics \
  -projectPath "$PROJ" \
  -executeMethod NexoDirectorStudio.EditorCLI.DirectorPipelineCli.Run \
  --config "$CFG" --artifacts "$ART/Artifacts" \
  -logFile "$ART/unity-pipeline.log" \
  -quit
