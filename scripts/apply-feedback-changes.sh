#!/usr/bin/env bash
set -euo pipefail

# Apply feedback changes to a game specification JSON file using Nexo CLI
# Usage: ./scripts/apply-feedback-changes.sh <feedback-json-path> <input-spec-path> [output-spec-path]

FEEDBACK_JSON_PATH="$1"
INPUT_SPEC_PATH="$2"
OUTPUT_SPEC_PATH="${3:-$INPUT_SPEC_PATH}"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ART="$(cd "$SCRIPT_DIR/.." && pwd)"

echo "🔄 Applying feedback changes to specification..."
echo "  Feedback: $FEEDBACK_JSON_PATH"
echo "  Input Spec: $INPUT_SPEC_PATH"
echo "  Output Spec: $OUTPUT_SPEC_PATH"
echo ""

# Use Nexo CLI if available (check both global install and dotnet run), otherwise fallback to C# tool
if command -v nexo &> /dev/null || [[ -f "$SCRIPT_DIR/../src/Nexo.CLI/Nexo.CLI.csproj" ]]; then
  # Use dotnet run if nexo not globally installed
  if command -v nexo &> /dev/null; then
    if [[ "$OUTPUT_SPEC_PATH" != "$INPUT_SPEC_PATH" ]]; then
      nexo demo apply-feedback "$FEEDBACK_JSON_PATH" "$INPUT_SPEC_PATH" --output "$OUTPUT_SPEC_PATH"
    else
      nexo demo apply-feedback "$FEEDBACK_JSON_PATH" "$INPUT_SPEC_PATH"
    fi
  else
    if [[ "$OUTPUT_SPEC_PATH" != "$INPUT_SPEC_PATH" ]]; then
      dotnet run --project "$SCRIPT_DIR/../src/Nexo.CLI/Nexo.CLI.csproj" -- demo apply-feedback "$FEEDBACK_JSON_PATH" "$INPUT_SPEC_PATH" --output "$OUTPUT_SPEC_PATH"
    else
      dotnet run --project "$SCRIPT_DIR/../src/Nexo.CLI/Nexo.CLI.csproj" -- demo apply-feedback "$FEEDBACK_JSON_PATH" "$INPUT_SPEC_PATH"
    fi
  fi
  exit $?
else
  echo "⚠️  Nexo CLI not found, using fallback C# tool..."
  TOOL_DIR="$ART/tools/ApplyFeedbackChanges"
  
  # Ensure the tool is built
  dotnet build "$TOOL_DIR" --configuration Debug --nologo --verbosity quiet
  
  # Run the tool
  dotnet run --project "$TOOL_DIR" -- "$FEEDBACK_JSON_PATH" "$INPUT_SPEC_PATH" "$OUTPUT_SPEC_PATH"
fi
