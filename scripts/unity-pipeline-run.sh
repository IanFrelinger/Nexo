#!/usr/bin/env bash
set -euo pipefail
UNITY="${UNITY_BIN:-/Applications/Unity/Hub/Editor/6000.2.6f1/Unity.app/Contents/MacOS/Unity}"
PROJ="${PROJ:-$(cd "$(dirname "${BASH_SOURCE[0]}")/../DirectorStudioUnity" && pwd)}"
ART="${ART:-$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)}"
PROMPT="${PROMPT:-micro room with one switch and one door}"
SEED="${SEED:-1337}"
RESUME_FLAG="${RESUME_FLAG:---resume}"

"$UNITY" -batchmode -nographics \
  -projectPath "$PROJ" \
  -executeMethod NexoDirectorStudio.EditorCLI.DirectorPipelineCli.Run \
  --prompt "$PROMPT" --seed "$SEED" --artifacts "$ART/Artifacts" $RESUME_FLAG \
  -logFile "$ART/unity-pipeline.log" \
  -quit
