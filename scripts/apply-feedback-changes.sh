#!/usr/bin/env bash
set -euo pipefail

# Apply feedback changes to game specification
# Usage: ./scripts/apply-feedback-changes.sh <feedback-file> [spec-file] [output-file]

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ART="$(cd "$SCRIPT_DIR/.." && pwd)"

FEEDBACK_FILE="${1:-}"
SPEC_FILE="${2:-$ART/game-specification.json}"
OUTPUT_FILE="${3:-$SPEC_FILE}"

if [[ -z "$FEEDBACK_FILE" ]]; then
  echo "Usage: $0 <feedback-file> [spec-file] [output-file]"
  echo "  feedback-file: Path to feedback-synthesis.json"
  echo "  spec-file: Path to game-specification.json (default: \$ART/game-specification.json)"
  echo "  output-file: Output path (default: overwrites spec-file)"
  exit 1
fi

# Build the tool if needed
TOOL_DIR="$ART/tools/ApplyFeedbackChanges"
TOOL_DLL="$TOOL_DIR/bin/Debug/net8.0/ApplyFeedbackChanges.dll"

if [[ ! -f "$TOOL_DLL" ]]; then
  echo "🔨 Building ApplyFeedbackChanges tool..."
  dotnet build "$TOOL_DIR/ApplyFeedbackChanges.csproj" -c Debug
  if [[ $? -ne 0 ]]; then
    echo "❌ Failed to build tool"
    exit 1
  fi
fi

# Run the tool
echo "🔄 Applying feedback changes to specification..."
dotnet "$TOOL_DLL" "$SPEC_FILE" "$FEEDBACK_FILE" "$OUTPUT_FILE"

exit $?

