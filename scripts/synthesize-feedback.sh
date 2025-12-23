#!/usr/bin/env bash
set -euo pipefail

# Synthesize feedback from playtest results using Nexo CLI
# Usage: ./scripts/synthesize-feedback.sh <playtest-results-path> <output-feedback-path>

PLAYTEST_RESULTS_PATH="$1"
OUTPUT_FEEDBACK_PATH="$2"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ART="$(cd "$SCRIPT_DIR/.." && pwd)"

echo "🧠 Synthesizing feedback from playtest results..."
echo "   Input: $PLAYTEST_RESULTS_PATH"
echo "   Output: $OUTPUT_FEEDBACK_PATH"
echo ""

# Use Nexo CLI if available (check both global install and dotnet run), otherwise fallback to orchestrator or bash
if command -v nexo &> /dev/null || [[ -f "$SCRIPT_DIR/../src/Nexo.CLI/Nexo.CLI.csproj" ]]; then
  # Use dotnet run if nexo not globally installed
  if command -v nexo &> /dev/null; then
    nexo demo synthesize-feedback "$PLAYTEST_RESULTS_PATH" "$OUTPUT_FEEDBACK_PATH"
  else
    dotnet run --project "$SCRIPT_DIR/../src/Nexo.CLI/Nexo.CLI.csproj" -- demo synthesize-feedback "$PLAYTEST_RESULTS_PATH" "$OUTPUT_FEEDBACK_PATH"
  fi
  exit $?
fi

# Fallback: Try using orchestrator if available
if command -v nexo &> /dev/null; then
  # Use orchestrator to synthesize feedback
  ORCHESTRATION_REQUEST="Synthesize feedback from playtest results in $PLAYTEST_RESULTS_PATH and create design change requests"
  nexo orchestrate "$ORCHESTRATION_REQUEST" --format-json > "$OUTPUT_FEEDBACK_PATH.orchestration-output.json"
  if [[ $? -eq 0 ]]; then
    echo "✅ Feedback synthesis completed via orchestrator"
    # Extract relevant output
    jq '.data.output' "$OUTPUT_FEEDBACK_PATH.orchestration-output.json" > "$OUTPUT_FEEDBACK_PATH"
    exit 0
  fi
fi

# Fallback: Extract issues from playtest results if orchestrator is not available
echo "Using fallback feedback synthesis (extracting from playtest results)..."
if command -v jq &> /dev/null; then
  # Extract issues and format into a simplified FeedbackSynthesis structure
  NOW=$(date -u +"%Y-%m-%dT%H:%M:%SZ")
  REPORT_ID=$(jq -r '.reportId // "unknown"' "$PLAYTEST_RESULTS_PATH" 2>/dev/null || echo "unknown")
  ISSUES_JSON=$(jq -c '[.issues[]? | select(.severity >= 2) | {requestId: (.issueId // "unknown"), sourceIssueId: (.issueId // "unknown"), domain: ((.affectedDomains[0] // "Gameplay")), changeType: "Modify", targetElement: "Placeholder Issue", currentValue: "N/A", proposedValue: (.title // "Improve"), rationale: (.description // "Based on feedback")}]' "$PLAYTEST_RESULTS_PATH" 2>/dev/null || echo "[]")
  
  jq -n \
    --arg now "$NOW" \
    --arg reportId "$REPORT_ID" \
    --argjson issues "$ISSUES_JSON" \
    '{
      synthesisId: ( "synth-" + ($now | split("T")[0]) ),
      sourceReportId: $reportId,
      generatedAt: $now,
      changeRequests: $issues,
      iterationRecommendation: {
        shouldIterate: ($issues | length > 0),
        priority: (if ($issues | length > 2) then "High" else "Normal" end),
        estimatedEffort: "PT1H",
        summary: ("Synthesized " + ($issues | length | tostring) + " changes from playtest results")
      }
    }' > "$OUTPUT_FEEDBACK_PATH"
  
  if [[ $? -eq 0 ]] && [[ -f "$OUTPUT_FEEDBACK_PATH" ]]; then
    echo "✅ Feedback synthesis created from playtest results"
    echo "   Note: This is a simplified synthesis. For full synthesis, use the orchestrator."
    exit 0
  else
    echo "❌ Failed to create feedback synthesis"
    exit 1
  fi
else
  echo "❌ jq not found. Cannot perform fallback feedback synthesis."
  exit 1
fi
