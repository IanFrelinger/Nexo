#!/usr/bin/env bash
set -euo pipefail

# Run custom game demo with custom assets and configuration
# Usage: ./scripts/run-custom-demo.sh [config-file] [unity-path]

CONFIG="${1:-nexo.custom-demo.json}"
UNITY_BIN="${2:-/Applications/Unity/Hub/Editor/6000.0.0f1/Unity.app/Contents/MacOS/Unity}"
PROJ="/Users/ianfrelinger/CursorProjects/Nexo/DirectorStudioUnity"
ART="/Users/ianfrelinger/CursorProjects/Nexo"

echo "🎮 Running Custom Game Demo"
echo "=========================="
echo "Config: $CONFIG"
echo "Unity: $UNITY_BIN"
echo "Project: $PROJ"
echo ""

# Ensure config exists
if [[ ! -f "$CONFIG" ]]; then
  echo "❌ Config file not found: $CONFIG"
  exit 1
fi

# Create artifacts directory
mkdir -p "$ART/Artifacts"

# Copy config to pipeline.json for Unity to use
cp "$CONFIG" "$ART/nexo.pipeline.json"

echo "📋 Configuration loaded:"
echo "  Name: $(jq -r '.name' "$CONFIG")"
echo "  Prompt: $(jq -r '.prompt' "$CONFIG")"
echo "  Seed: $(jq -r '.seed' "$CONFIG")"
echo "  Mode: $(jq -r '.review.mode' "$CONFIG")"
echo ""

# Run the Unity pipeline
echo "🚀 Starting Unity pipeline..."
"$UNITY_BIN" \
  -batchmode \
  -quit \
  -projectPath "$PROJ" \
  -executeMethod NexoDirectorStudio.Editor.DirectorCliRunner.RunPipeline \
  -logFile "$ART/unity-custom-demo.log" \
  -artifactsPath "$ART"

# Check if generation was successful
if [[ $? -eq 0 ]]; then
  echo "✅ Custom demo generation completed successfully!"
  
  # Show generated artifacts
  echo ""
  echo "📁 Generated artifacts:"
  find "$ART/Artifacts" -name "*.json" -o -name "*.xml" | head -10
  
  # Run review mode if artifacts exist
  if [[ -d "$ART/Artifacts" ]] && [[ "$(ls -A "$ART/Artifacts" 2>/dev/null)" ]]; then
    echo ""
    echo "🎯 Running review mode..."
    ./scripts/run-for-review.sh || true
    
    # Aggregate results
    echo ""
    echo "📊 Aggregating results..."
    ./scripts/aggregate-junit.sh playmode-custom-demo.junit.xml Artifacts/*/playmode-smoke.junit.xml || true
    
    # Generate summary
    echo ""
    echo "📝 Generating summary..."
    ./scripts/review-summary-md.sh review_summary.json CUSTOM_DEMO_SUMMARY.md || true
    
    echo ""
    echo "🎉 Custom demo complete! Check:"
    echo "  - Artifacts: $ART/Artifacts/"
    echo "  - JUnit: playmode-custom-demo.junit.xml"
    echo "  - Summary: CUSTOM_DEMO_SUMMARY.md"
    echo "  - Log: unity-custom-demo.log"
  fi
else
  echo "❌ Custom demo generation failed. Check unity-custom-demo.log for details."
  exit 1
fi
