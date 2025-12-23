#!/usr/bin/env bash
set -euo pipefail

# Synthesize feedback from playtest results using the orchestrator
# Usage: ./scripts/synthesize-feedback.sh <playtest-results> [output-file]

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ART="$(cd "$SCRIPT_DIR/.." && pwd)"

PLAYTEST_RESULTS="${1:-}"
OUTPUT_FILE="${2:-$ART/feedback-synthesis.json}"

if [[ -z "$PLAYTEST_RESULTS" ]]; then
  echo "Usage: $0 <playtest-results> [output-file]"
  echo "  playtest-results: Path to playtest results JSON"
  echo "  output-file: Path to save feedback synthesis (default: \$ART/feedback-synthesis.json)"
  exit 1
fi

if [[ ! -f "$PLAYTEST_RESULTS" ]]; then
  echo "❌ Playtest results file not found: $PLAYTEST_RESULTS"
  exit 1
fi

echo "🧠 Synthesizing feedback from playtest results..."
echo "   Input: $PLAYTEST_RESULTS"
echo "   Output: $OUTPUT_FILE"
echo ""

# Use orchestrator to synthesize feedback
# The orchestrator will decompose the request and run FeedbackSynthesizerAgent
REQUEST="Synthesize design feedback from playtest results in $PLAYTEST_RESULTS and generate change requests for the game specification"

if command -v nexo &> /dev/null; then
  echo "Using Nexo CLI orchestrator..."
  nexo orchestrate "$REQUEST" --format-json > "$OUTPUT_FILE.tmp"
  
  if [[ $? -eq 0 ]] && [[ -f "$OUTPUT_FILE.tmp" ]]; then
    # Extract feedback synthesis from orchestrator output
    if command -v jq &> /dev/null; then
      # Try to extract the FeedbackSynthesis from the orchestrator output
      jq -r '.data.output // .data // .' "$OUTPUT_FILE.tmp" > "$OUTPUT_FILE" 2>/dev/null || \
        cp "$OUTPUT_FILE.tmp" "$OUTPUT_FILE"
    else
      cp "$OUTPUT_FILE.tmp" "$OUTPUT_FILE"
    fi
    rm -f "$OUTPUT_FILE.tmp"
    
    if [[ -f "$OUTPUT_FILE" ]] && [[ -s "$OUTPUT_FILE" ]]; then
      echo "✅ Feedback synthesis saved to: $OUTPUT_FILE"
      exit 0
    fi
  fi
fi

# Fallback: Create a simple feedback structure from playtest results
echo "Using fallback feedback synthesis (extracting from playtest results)..."
if command -v jq &> /dev/null; then
  # Extract issues and create basic change requests
  jq -c '{
    synthesisId: (.reportId // "fallback-" + (now | tostring)),
    sourceReportId: (.reportId // "unknown"),
    generatedAt: (now | toiso8601),
    changeRequests: (
      (.issues // []) | map({
        requestId: ("req-" + (.issueId // "unknown")),
        sourceIssueId: (.issueId // "unknown"),
        domain: (.affectedDomains[0] // "Gameplay"),
        changeType: "Modify",
        targetElement: (.title // "unknown"),
        proposedValue: ((.suggestedFixes[0].description // "Improve balance")),
        rationale: (.description // "Address balance issue")
      })
    ),
    iterationRecommendation: {
      shouldIterate: ((.issues // []) | length > 0),
      priority: (if ((.issues // []) | length) > 5 then "High" else "Normal" end),
      estimatedEffort: "PT2H",
      summary: ("Found " + ((.issues // []) | length | tostring) + " issues requiring changes")
    }
  }' "$PLAYTEST_RESULTS" > "$OUTPUT_FILE"
  
  if [[ $? -eq 0 ]] && [[ -f "$OUTPUT_FILE" ]]; then
    echo "✅ Feedback synthesis created from playtest results"
    echo "   Note: This is a simplified synthesis. For full synthesis, use the orchestrator."
    exit 0
  fi
fi

echo "❌ Failed to synthesize feedback"
exit 1

