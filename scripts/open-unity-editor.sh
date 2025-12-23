#!/usr/bin/env bash
set -euo pipefail

# Open a Unity project in the editor using Nexo CLI
# Usage: ./scripts/open-unity-editor.sh [project-path] [unity-path]

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ART="$(cd "$SCRIPT_DIR/.." && pwd)"

PROJECT_PATH="${1:-$ART/IterativeGameDemo}"
UNITY_BIN="${2:-${UNITY_BIN:-}}"

echo "🚀 Opening Unity Editor"
echo "======================"
echo "  Project: $PROJECT_PATH"
echo ""

if [[ ! -d "$PROJECT_PATH" ]]; then
  echo "❌ Project path not found: $PROJECT_PATH"
  exit 1
fi

# Use Nexo CLI if available (check both global install and dotnet run), otherwise fallback to direct Unity call
if command -v nexo &> /dev/null || [[ -f "$SCRIPT_DIR/../src/Nexo.CLI/Nexo.CLI.csproj" ]]; then
  UNITY_ARGS=()
  if [[ -n "$UNITY_BIN" ]]; then
    UNITY_ARGS+=(--unity-bin "$UNITY_BIN")
  fi
  
  # Use dotnet run if nexo not globally installed
  if command -v nexo &> /dev/null; then
    nexo unity open "$PROJECT_PATH" "${UNITY_ARGS[@]}"
  else
    dotnet run --project "$SCRIPT_DIR/../src/Nexo.CLI/Nexo.CLI.csproj" -- unity open "$PROJECT_PATH" "${UNITY_ARGS[@]}"
  fi
  exit $?
else
  echo "⚠️  Nexo CLI not found, using direct Unity call..."
  UNITY="${UNITY_BIN:-/Applications/Unity/Hub/Editor/6000.2.6f1/Unity.app/Contents/MacOS/Unity}"
  
  echo "Opening Unity Editor for project..."
  
  # Determine OS and open command
  if [[ "$OSTYPE" == "darwin"* ]]; then
    # macOS
    open -a "$UNITY" --args -projectPath "$PROJECT_PATH" -logFile "$ART/unity-editor.log" &
  elif [[ "$OSTYPE" == "linux-gnu"* ]]; then
    # Linux
    "$UNITY" -projectPath "$PROJECT_PATH" -logFile "$ART/unity-editor.log" &
  elif [[ "$OSTYPE" == "cygwin" ]] || [[ "$OSTYPE" == "msys" ]] || [[ "$OSTYPE" == "win32" ]]; then
    # Windows (Git Bash / WSL)
    start "" "$UNITY" -projectPath "$PROJECT_PATH" -logFile "$ART/unity-editor.log" &
  else
    echo "⚠️  Unsupported OS for opening Unity Editor automatically."
    echo "   Please open Unity manually: '$UNITY -projectPath \"$PROJECT_PATH\"'"
    exit 1
  fi
  
  echo "✅ Unity Editor is opening..."
  echo "   Project: $PROJECT_PATH"
  echo ""
  echo "💡 The editor will open in a separate window."
  echo "   You can close this terminal once Unity has loaded."
fi
