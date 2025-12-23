#!/usr/bin/env bash
set -euo pipefail

# Open Unity project in the Unity editor
# Usage: ./scripts/open-unity-editor.sh [project-path]

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ART="$(cd "$SCRIPT_DIR/.." && pwd)"
UNITY="${UNITY_BIN:-/Applications/Unity/Hub/Editor/6000.2.6f1/Unity.app/Contents/MacOS/Unity}"

PROJECT_PATH="${1:-$ART/DirectorStudioUnity}"

if [[ ! -d "$PROJECT_PATH" ]]; then
  echo "❌ Project directory not found: $PROJECT_PATH"
  exit 1
fi

echo "🚀 Opening Unity Editor"
echo "======================"
echo "  Project: $PROJECT_PATH"
echo ""

# Open Unity editor (non-headless mode)
# On macOS, we can use 'open' to launch it in the background
if [[ "$(uname)" == "Darwin" ]]; then
  # Extract the Unity.app path from the executable path
  UNITY_APP="${UNITY%/Contents/MacOS/Unity}"
  if [[ -d "$UNITY_APP" ]]; then
    echo "Opening Unity Editor for project..."
    open -a "$UNITY_APP" --args -projectPath "$PROJECT_PATH"
  else
    # Fallback: run Unity directly (will open GUI)
    "$UNITY" -projectPath "$PROJECT_PATH" &
  fi
elif [[ "$(uname)" == "Linux" ]]; then
  # On Linux, run in background
  "$UNITY" -projectPath "$PROJECT_PATH" &
elif [[ "$(uname)" == "MINGW"* ]] || [[ "$(uname)" == "MSYS"* ]] || [[ -n "${WINDIR:-}" ]]; then
  # On Windows
  "$UNITY" -projectPath "$PROJECT_PATH"
else
  # Fallback: run in background
  "$UNITY" -projectPath "$PROJECT_PATH" &
fi

echo "✅ Unity Editor is opening..."
echo "   Project: $PROJECT_PATH"
echo ""
echo "💡 The editor will open in a separate window."
echo "   You can close this terminal once Unity has loaded."

