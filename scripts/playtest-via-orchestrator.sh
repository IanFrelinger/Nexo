#!/usr/bin/env bash
set -euo pipefail

# Run playtesting via orchestrator (no Unity Editor scripts required)
# Usage: ./scripts/playtest-via-orchestrator.sh [generation-results] [output-file]

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ART="$(cd "$SCRIPT_DIR/.." && pwd)"

GENERATION_RESULTS="${1:-$ART/Artifacts/generated-game/orchestrator-output.json}"
OUTPUT_FILE="${2:-$ART/playtest-results.json}"

echo "🤖 Running Playtest via Orchestrator"
echo "===================================="
echo "  Generation results: $GENERATION_RESULTS"
echo "  Output: $OUTPUT_FILE"
echo ""

# Create playtest request
REQUEST="Run AI playtesting on the generated game. Test gameplay mechanics, balance, and player experience. Generate a detailed playtest report with issues, metrics, and recommendations."

if command -v nexo &> /dev/null; then
  echo "🚀 Running orchestrator for playtesting..."
  
  nexo orchestrate "$REQUEST" --format-json > "$OUTPUT_FILE" 2>&1
  
  if [[ $? -eq 0 ]] && [[ -f "$OUTPUT_FILE" ]]; then
    echo "✅ Playtesting completed"
    echo "   Results saved to: $OUTPUT_FILE"
    
    if command -v jq &> /dev/null; then
      echo ""
      echo "📊 Playtest Summary:"
      jq -r '.data.output // .data // "See playtest-results.json for details"' "$OUTPUT_FILE" | head -20
    fi
  else
    echo "⚠️  Orchestrator had issues, creating placeholder results..."
    
    # Create placeholder playtest results
    cat > "$OUTPUT_FILE" << EOF
{
  "reportId": "placeholder-$(date +%s)",
  "generatedAt": "$(date -u +"%Y-%m-%dT%H:%M:%SZ")",
  "status": "placeholder",
  "message": "Nexo CLI not fully configured. Install and configure for full playtesting.",
  "issues": [],
  "metrics": {
    "overallQuality": 5.0,
    "playability": 5.0,
    "balance": 5.0
  }
}
EOF
  fi
else
  echo "⚠️  Nexo CLI not found. Creating placeholder playtest results..."
  
  cat > "$OUTPUT_FILE" << EOF
{
  "reportId": "placeholder-$(date +%s)",
  "generatedAt": "$(date -u +"%Y-%m-%dT%H:%M:%SZ")",
  "status": "placeholder",
  "message": "Nexo CLI not available. Install with: dotnet tool install --global Nexo.CLI",
  "issues": [
    {
      "issueId": "placeholder-1",
      "title": "Placeholder Issue",
      "description": "Install Nexo CLI for real playtesting",
      "severity": "Low",
      "affectedDomains": ["Gameplay"]
    }
  ],
  "metrics": {
    "overallQuality": 5.0,
    "playability": 5.0,
    "balance": 5.0
  }
}
EOF
fi

echo ""
echo "📁 Playtest results: $OUTPUT_FILE"
echo ""

