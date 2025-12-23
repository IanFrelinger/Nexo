#!/usr/bin/env bash
set -euo pipefail

# Capture and parse Unity console logs/errors using Nexo CLI
# Usage: ./scripts/capture-unity-logs.sh [log-file] [output-json]

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ART="$(cd "$SCRIPT_DIR/.." && pwd)"

LOG_FILE="${1:-}"
OUTPUT_JSON="${2:-$ART/unity-logs-analysis.json}"

if [[ -z "$LOG_FILE" ]] || [[ ! -f "$LOG_FILE" ]]; then
  echo "⚠️  Unity log file not found: ${LOG_FILE:-<not provided>}"
  echo "   Creating empty analysis..."
  jq -n '{
    errors: [],
    warnings: [],
    info: [],
    errorCount: 0,
    warningCount: 0,
    hasErrors: false,
    hasWarnings: false,
    summary: "No log file available"
  }' > "$OUTPUT_JSON"
  exit 0
fi

# Use Nexo CLI if available (check both global install and dotnet run), otherwise fallback to bash implementation
if command -v nexo &> /dev/null || [[ -f "$SCRIPT_DIR/../src/Nexo.CLI/Nexo.CLI.csproj" ]]; then
  # Use dotnet run if nexo not globally installed
  if command -v nexo &> /dev/null; then
    nexo unity logs "$LOG_FILE" --output "$OUTPUT_JSON"
  else
    dotnet run --project "$SCRIPT_DIR/../src/Nexo.CLI/Nexo.CLI.csproj" -- unity logs "$LOG_FILE" --output "$OUTPUT_JSON"
  fi
  exit $?
fi

echo "⚠️  Nexo CLI not found, using fallback bash implementation..."
echo "📋 Analyzing Unity logs..."
echo "   Log file: $LOG_FILE"
echo "   Output: $OUTPUT_JSON"
echo ""

# Parse Unity log file
# Unity logs have format: [timestamp] [level] [message]
# Errors: "Aborting batchmode due to failure", "Exception:", "Error:"
# Warnings: "Warning:", "warning"

ERRORS=()
WARNINGS=()
INFO_MESSAGES=()

# Read log file line by line
while IFS= read -r line || [[ -n "$line" ]]; do
  # Check for errors
  if [[ "$line" =~ (Aborting batchmode|Exception:|Error:|Failed|could not be found|does not exist) ]]; then
    ERRORS+=("$line")
  # Check for warnings
  elif [[ "$line" =~ (Warning:|warning|⚠|WARN) ]]; then
    WARNINGS+=("$line")
  # Info messages (exclude verbose Unity internal messages)
  elif [[ "$line" =~ (Info:|INFO|✅|📋|🎮) ]] && [[ ! "$line" =~ (Unity|Editor|AssetDatabase) ]]; then
    INFO_MESSAGES+=("$line")
  fi
done < "$LOG_FILE"

# Create JSON output
jq -n \
  --argjson errors "$(printf '%s\n' "${ERRORS[@]}" | jq -R . | jq -s .)" \
  --argjson warnings "$(printf '%s\n' "${WARNINGS[@]}" | jq -R . | jq -s .)" \
  --argjson info "$(printf '%s\n' "${INFO_MESSAGES[@]}" | jq -R . | jq -s .)" \
  --arg logFile "$LOG_FILE" \
  --arg timestamp "$(date -u +"%Y-%m-%dT%H:%M:%SZ")" \
  '{
    logFile: $logFile,
    timestamp: $timestamp,
    errors: $errors,
    warnings: $warnings,
    info: $info,
    errorCount: ($errors | length),
    warningCount: ($warnings | length),
    infoCount: ($info | length),
    hasErrors: (($errors | length) > 0),
    hasWarnings: (($warnings | length) > 0),
    summary: (
      if ($errors | length) > 0 then
        "Found " + ($errors | length | tostring) + " error(s) and " + ($warnings | length | tostring) + " warning(s)"
      elif ($warnings | length) > 0 then
        "Found " + ($warnings | length | tostring) + " warning(s), no errors"
      else
        "No errors or warnings found"
      end
    )
  }' > "$OUTPUT_JSON"

ERROR_COUNT=$(jq -r '.errorCount' "$OUTPUT_JSON")
WARNING_COUNT=$(jq -r '.warningCount' "$OUTPUT_JSON")

echo "📊 Log Analysis Results:"
echo "   Errors: $ERROR_COUNT"
echo "   Warnings: $WARNING_COUNT"
echo ""

if [[ "$ERROR_COUNT" -gt 0 ]]; then
  echo "❌ Errors found:"
  jq -r '.errors[]' "$OUTPUT_JSON" | head -5 | while read -r err; do
    echo "   - $err"
  done
  echo ""
fi

if [[ "$WARNING_COUNT" -gt 0 ]]; then
  echo "⚠️  Warnings found:"
  jq -r '.warnings[]' "$OUTPUT_JSON" | head -5 | while read -r warn; do
    echo "   - $warn"
  done
  echo ""
fi

echo "✅ Analysis saved to: $OUTPUT_JSON"

