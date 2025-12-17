#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ART="$(cd "$SCRIPT_DIR/.." && pwd)"
PROJ="${PROJ:-$ART/DirectorStudioUnity}"
UNITY="${UNITY_BIN:-/Applications/Unity/Hub/Editor/6000.2.6f1/Unity.app/Contents/MacOS/Unity}"
mkdir -p "$ART"

echo "🎮 === COMPLETE GAME DEVELOPMENT WORKFLOW ==="
echo "============================================="
echo ""
echo "This workflow will guide you through the complete process:"
echo "1. 🧠 Game Specification Wizard (Socratic dialogue)"
echo "2. 🎯 Specification-Driven Generation"
echo "3. 🤖 AI Playtesting & Validation"
echo "4. 📊 Results Analysis"
echo ""

# Check if specification already exists
if [[ -f "$ART/game-specification.json" ]]; then
  echo "📋 Found existing specification: $ART/game-specification.json"
  echo "Do you want to:"
  echo "  1. Use existing specification"
  echo "  2. Create new specification"
  echo "  3. Exit"
  echo ""
  read -p "Enter choice (1-3): " choice
  
  case $choice in
    1)
      echo "✅ Using existing specification"
      ;;
    2)
      echo "🧠 Starting Game Specification Wizard..."
      ./scripts/unity-game-spec-wizard.sh
      if [[ $? -ne 0 ]]; then
        echo "❌ Specification wizard failed or was cancelled"
        exit 1
      fi
      ;;
    3)
      echo "👋 Exiting workflow"
      exit 0
      ;;
    *)
      echo "❌ Invalid choice, using existing specification"
      ;;
  esac
else
  echo "🧠 No existing specification found. Starting Game Specification Wizard..."
  ./scripts/unity-game-spec-wizard.sh
  if [[ $? -ne 0 ]]; then
    echo "❌ Specification wizard failed or was cancelled"
    exit 1
  fi
fi

echo ""
echo "🎯 Starting Specification-Driven Generation..."
./scripts/unity-spec-driven-generator.sh

if [[ $? -eq 0 ]]; then
  echo ""
  echo "🎉 === WORKFLOW COMPLETED SUCCESSFULLY! ==="
  echo ""
  echo "📁 Generated Files:"
  ls -la "$ART"/*.json 2>/dev/null | grep -E "(spec|results)" || echo "No result files found"
  echo ""
  echo "🎮 Generated Scenes:"
  ls -la "$PROJ/Assets/GeneratedScenes/" 2>/dev/null | tail -5 || echo "No scenes found"
  echo ""
  echo "✅ Your game has been generated and tested!"
  echo "💡 Check the Unity project to see your generated scene."
else
  echo "❌ Workflow failed. Check the log files for details."
  exit 1
fi
