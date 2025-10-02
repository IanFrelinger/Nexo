#!/usr/bin/env bash
set -euo pipefail

UNITY="/Applications/Unity/Hub/Editor/6000.2.6f1/Unity.app/Contents/MacOS/Unity"
PROJ="$(cd "$(dirname "${BASH_SOURCE[0]}")/../DirectorStudioUnity" && pwd)"
ART="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

echo "==> List compiled test DLLs"
"$UNITY" -batchmode -nographics -projectPath "$PROJ" \
 -executeMethod NexoDirectorStudio.Editor.CI.CiListTestAssemblies.Run \
 -quit -logFile "$ART/diag-test-assemblies.log" || true
cat "$ART/diag-test-assemblies.log" || true

echo "==> Ask UTF what PlayMode tests exist"
"$UNITY" -batchmode -nographics -projectPath "$PROJ" \
 -executeMethod NexoDirectorStudio.Editor.CI.CiListUtfPlaymodeTests.Run \
 -quit -logFile "$ART/diag-utf-list.log" || true
cat "$ART/diag-utf-list.log" || true

echo "==> Run your existing smoke (as control)"
scripts/unity-smoke-fallback.sh || true
