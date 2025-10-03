#!/usr/bin/env bash
set -euo pipefail

# Preview demo configuration and show what would be generated
# Usage: ./scripts/demo-preview.sh [config-file]

CONFIG="${1:-nexo.custom-demo.json}"

echo "🎮 Demo Preview - Nexo Framework"
echo "================================"
echo ""

if [[ ! -f "$CONFIG" ]]; then
  echo "❌ Config file not found: $CONFIG"
  echo "Available configs:"
  ls -1 *.json 2>/dev/null | grep -E "(demo|pipeline)" || echo "  No demo configs found"
  exit 1
fi

echo "📋 Configuration Preview:"
echo "  File: $CONFIG"
echo "  Name: $(jq -r '.name' "$CONFIG")"
echo "  Version: $(jq -r '.version' "$CONFIG")"
echo "  Seed: $(jq -r '.seed' "$CONFIG")"
echo "  Mode: $(jq -r '.review.mode' "$CONFIG")"
echo ""

echo "🎯 Game Prompt:"
echo "  $(jq -r '.prompt' "$CONFIG")"
echo ""

echo "⚙️  Pipeline Phases:"
jq -r '.phases[] | "  - \(.token): \(.type) (timeout: \(.timeoutMs)ms, cache: \(.cache))"' "$CONFIG"
echo ""

echo "🤖 AI Adapters:"
if jq -e '.adapters' "$CONFIG" >/dev/null 2>&1; then
  ADAPTERS_FILE=$(jq -r '.adapters' "$CONFIG")
  if [[ -f "$ADAPTERS_FILE" ]]; then
    echo "  Ollama: $(jq -r '.ollama.model // "not configured"' "$ADAPTERS_FILE")"
    echo "  ComfyUI: $(jq -r '.comfy.endpoint // "not configured"' "$ADAPTERS_FILE")"
    echo "  Piper: $(jq -r '.piper.voice // "not configured"' "$ADAPTERS_FILE")"
  else
    echo "  Adapters file not found: $ADAPTERS_FILE"
  fi
else
  echo "  No adapters configured"
fi
echo ""

echo "🎨 Custom Assets:"
if jq -e '.customAssets' "$CONFIG" >/dev/null 2>&1; then
  echo "  Materials: $(jq -r '.customAssets.materials | length' "$CONFIG")"
  echo "  Prefabs: $(jq -r '.customAssets.prefabs | length' "$CONFIG")"
  echo "  Audio: $(jq -r '.customAssets.audio | length' "$CONFIG")"
else
  echo "  No custom assets configured"
fi
echo ""

echo "📊 Review Configuration:"
REVIEW=$(jq -r '.review' "$CONFIG")
echo "  Mode: $(echo "$REVIEW" | jq -r '.mode')"
echo "  Canary Seeds: $(echo "$REVIEW" | jq -r '.canarySeeds | join(", ")')"
echo "  Min Engagement: $(echo "$REVIEW" | jq -r '.minEngagement')"
echo "  Min Gameplay Quality: $(echo "$REVIEW" | jq -r '.minGameplayQuality')"
echo ""

echo "🚀 What This Demo Would Generate:"
echo "  ✅ Game plan and layout design"
echo "  ✅ 3D scene with custom cyberpunk assets"
echo "  ✅ Interactive terminals and security systems"
echo "  ✅ Atmospheric lighting and particle effects"
echo "  ✅ Hacking mechanics and stealth gameplay"
echo "  ✅ Custom materials (neon, metal, holographic)"
echo "  ✅ Audio (ambient, SFX, voice)"
echo "  ✅ JUnit test results and validation reports"
echo "  ✅ Review summary with engagement metrics"
echo ""

echo "📁 Output Structure:"
echo "  Artifacts/<runId>/"
echo "    ├── Plan/output.json          (game design)"
echo "    ├── Layout/output.json        (scene layout)"
echo "    ├── Placement/output.json     (object placement)"
echo "    ├── Content/output.json       (generated assets)"
echo "    └── Validate/output.json      (validation results)"
echo "  playmode-*.junit.xml            (test results)"
echo "  review_summary.json             (engagement metrics)"
echo "  CUSTOM_DEMO_SUMMARY.md          (human-readable summary)"
echo ""

echo "🎯 To Run This Demo:"
echo "  ./scripts/run-custom-demo.sh $CONFIG"
echo ""
echo "🎯 To Run in Unity Editor:"
echo "  1. Open DirectorStudioUnity project in Unity"
echo "  2. Go to Nexo → Director Studio"
echo "  3. Paste the prompt and configure settings"
echo "  4. Click 'Plan Game Slice' and follow the pipeline"
echo ""

echo "✨ Ready to generate your custom game demo!"
