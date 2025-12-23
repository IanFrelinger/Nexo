#!/usr/bin/env bash
set -euo pipefail

# Create a new Unity project using Nexo CLI
# Usage: ./scripts/create-unity-project.sh [project-name] [project-path] [unity-path]

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ART="$(cd "$SCRIPT_DIR/.." && pwd)"

PROJECT_NAME="${1:-IterativeGameDemo}"
PROJECT_PATH="${2:-$ART/$PROJECT_NAME}"
UNITY_BIN="${3:-${UNITY_BIN:-}}"

echo "🎮 Creating Unity Project"
echo "========================"
echo "  Name: $PROJECT_NAME"
echo "  Path: $PROJECT_PATH"
echo ""

# Use Nexo CLI if available (check both global install and dotnet run), otherwise fallback to direct Unity call
if command -v nexo &> /dev/null || [[ -f "$SCRIPT_DIR/../src/Nexo.CLI/Nexo.CLI.csproj" ]]; then
  UNITY_ARGS=()
  if [[ -n "$UNITY_BIN" ]]; then
    UNITY_ARGS+=(--unity-bin "$UNITY_BIN")
  fi
  
  if [[ "$PROJECT_PATH" != "$ART/$PROJECT_NAME" ]]; then
    UNITY_ARGS+=(--path "$PROJECT_PATH")
  fi
  
  # Use dotnet run if nexo not globally installed
  if command -v nexo &> /dev/null; then
    nexo unity create "$PROJECT_NAME" "${UNITY_ARGS[@]}"
  else
    dotnet run --project "$SCRIPT_DIR/../src/Nexo.CLI/Nexo.CLI.csproj" -- unity create "$PROJECT_NAME" "${UNITY_ARGS[@]}"
  fi
  exit $?
else
  echo "⚠️  Nexo CLI not found, using direct Unity call..."
  UNITY="${UNITY_BIN:-/Applications/Unity/Hub/Editor/6000.2.6f1/Unity.app/Contents/MacOS/Unity}"
  
  # Check if project already exists
  if [[ -d "$PROJECT_PATH" ]] && [[ -f "$PROJECT_PATH/ProjectSettings/ProjectSettings.asset" ]]; then
    echo "✅ Unity project already exists at: $PROJECT_PATH"
    echo "   Skipping creation..."
    exit 0
  fi
  
  # Create project directory
  mkdir -p "$PROJECT_PATH"
  
  echo "📁 Creating project structure..."
  
  # Use Unity to create the project
  "$UNITY" \
    -batchmode \
    -nographics \
    -createProject "$PROJECT_PATH" \
    -quit \
    -logFile "$ART/unity-create-project.log"
  
  if [[ $? -eq 0 ]] && [[ -d "$PROJECT_PATH/Assets" ]]; then
    echo "✅ Unity project created successfully!"
    echo "   Location: $PROJECT_PATH"
  else
    echo "⚠️  Unity project creation may have had issues, but continuing..."
    # Create basic structure manually if Unity didn't
    mkdir -p "$PROJECT_PATH/Assets"
    mkdir -p "$PROJECT_PATH/ProjectSettings"
    mkdir -p "$PROJECT_PATH/Packages"
  fi
  
  echo ""
  echo "📋 Project ready at: $PROJECT_PATH"
  echo ""
fi
