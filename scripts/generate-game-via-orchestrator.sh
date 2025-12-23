#!/usr/bin/env bash
set -euo pipefail

# Generate game content using the orchestrator (no Unity Editor scripts required)
# Usage: ./scripts/generate-game-via-orchestrator.sh <spec-file> [output-dir]

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ART="$(cd "$SCRIPT_DIR/.." && pwd)"

SPEC_FILE="${1:-$ART/game-specification.json}"
OUTPUT_DIR="${2:-$ART/Artifacts/generated-game}"

if [[ ! -f "$SPEC_FILE" ]]; then
  echo "❌ Specification file not found: $SPEC_FILE"
  exit 1
fi

echo "🎮 Generating Game via Orchestrator"
echo "===================================="
echo "  Specification: $SPEC_FILE"
echo "  Output: $OUTPUT_DIR"
echo ""

mkdir -p "$OUTPUT_DIR"

# Read spec to create orchestration request
if command -v jq &> /dev/null; then
  SPEC_NAME=$(jq -r '.name // "Game"' "$SPEC_FILE" 2>/dev/null || echo "Game")
  SPEC_GENRE=$(jq -r '.genre // "Action"' "$SPEC_FILE" 2>/dev/null || echo "Action")
  SPEC_DESC=$(jq -r '.description // ""' "$SPEC_FILE" 2>/dev/null || echo "")
else
  SPEC_NAME="Game"
  SPEC_GENRE="Action"
  SPEC_DESC=""
fi

# Create orchestration request
REQUEST="Generate a $SPEC_GENRE game called '$SPEC_NAME'. $SPEC_DESC Create game assets including scenes, characters, and gameplay mechanics based on the specification in $SPEC_FILE"

echo "📋 Orchestration Request:"
echo "   $REQUEST"
echo ""

# Use orchestrator if available
if command -v nexo &> /dev/null; then
  echo "🚀 Running orchestrator..."
  ORCHESTRATOR_OUTPUT="$OUTPUT_DIR/orchestrator-output.json"
  
  nexo orchestrate "$REQUEST" --format-json > "$ORCHESTRATOR_OUTPUT" 2>&1
  
  if [[ $? -eq 0 ]] && [[ -f "$ORCHESTRATOR_OUTPUT" ]]; then
    echo "✅ Orchestration completed"
    echo "   Results saved to: $ORCHESTRATOR_OUTPUT"
    
    # Extract generated content if available
    if command -v jq &> /dev/null; then
      echo ""
      echo "📊 Generated Content Summary:"
      jq -r '.data.output // .data // "See orchestrator-output.json for details"' "$ORCHESTRATOR_OUTPUT" | head -20
    fi
  else
    echo "⚠️  Orchestrator had issues, but continuing..."
  fi
else
  echo "⚠️  Nexo CLI not found. Creating placeholder generation results..."
  
  # Create placeholder results
  cat > "$OUTPUT_DIR/generation-results.json" << EOF
{
  "generatedAt": "$(date -u +"%Y-%m-%dT%H:%M:%SZ")",
  "specification": "$SPEC_FILE",
  "status": "placeholder",
  "message": "Nexo CLI not available. Install with: dotnet tool install --global Nexo.CLI",
  "assets": {
    "scenes": [],
    "characters": [],
    "mechanics": []
  }
}
EOF
  
  echo "   Placeholder results created at: $OUTPUT_DIR/generation-results.json"
  echo "   Install Nexo CLI for full generation: dotnet tool install --global Nexo.CLI"
fi

echo ""
echo "📁 Output directory: $OUTPUT_DIR"
echo ""

