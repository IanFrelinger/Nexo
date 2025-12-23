#!/usr/bin/env bash
set -euo pipefail

# Create a new Unity project
# Usage: ./scripts/create-unity-project.sh [project-name] [project-path]

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ART="$(cd "$SCRIPT_DIR/.." && pwd)"
UNITY="${UNITY_BIN:-/Applications/Unity/Hub/Editor/6000.2.6f1/Unity.app/Contents/MacOS/Unity}"

PROJECT_NAME="${1:-IterativeGameDemo}"
PROJECT_PATH="${2:-$ART/$PROJECT_NAME}"

echo "🎮 Creating Unity Project"
echo "========================"
echo "  Name: $PROJECT_NAME"
echo "  Path: $PROJECT_PATH"
echo ""

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

