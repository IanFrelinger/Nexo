#!/usr/bin/env bash
set -euo pipefail

# Start Unity with debugger connection and log capture
# Usage: ./scripts/unity-debugger-listener.sh [project-path] [command] [args...]

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ART="$(cd "$SCRIPT_DIR/.." && pwd)"

UNITY="${UNITY_BIN:-/Applications/Unity/Hub/Editor/6000.3.0f1/Unity.app/Contents/MacOS/Unity}"
PROJECT_PATH="${1:-$ART/IterativeGameDemo}"
COMMAND="${2:-}"
shift 2 2>/dev/null || shift 1 2>/dev/null || true
ARGS=("$@")

# Create logs directory
LOGS_DIR="$ART/UnityLogs"
mkdir -p "$LOGS_DIR"

# Generate unique log file name
TIMESTAMP=$(date +"%Y%m%d_%H%M%S")
LOG_FILE="$LOGS_DIR/unity_${COMMAND}_${TIMESTAMP}.log"
ANALYSIS_FILE="$LOGS_DIR/unity_${COMMAND}_${TIMESTAMP}_analysis.json"

echo "🔍 Unity Debugger Listener"
echo "=========================="
echo "  Project: $PROJECT_PATH"
echo "  Command: $COMMAND"
echo "  Log file: $LOG_FILE"
echo ""

# Build Unity command with logging
UNITY_ARGS=(
  -batchmode
  -nographics
  -projectPath "$PROJECT_PATH"
  -logFile "$LOG_FILE"
  -quit
)

# Add execute method if provided
if [[ -n "$COMMAND" ]]; then
  UNITY_ARGS+=(-executeMethod "$COMMAND")
fi

# Add any additional arguments
UNITY_ARGS+=("${ARGS[@]}")

echo "🚀 Running Unity with debug logging..."
echo "   Command: $UNITY ${UNITY_ARGS[*]}"
echo ""

# Run Unity and capture exit code
"$UNITY" "${UNITY_ARGS[@]}" 2>&1 | tee -a "$LOG_FILE" || EXIT_CODE=$?

# Analyze logs immediately
echo ""
echo "📋 Analyzing Unity logs..."
./scripts/capture-unity-logs.sh "$LOG_FILE" "$ANALYSIS_FILE"

# Check for errors
HAS_ERRORS=$(jq -r '.hasErrors' "$ANALYSIS_FILE" 2>/dev/null || echo "false")

if [[ "$HAS_ERRORS" == "true" ]]; then
  echo ""
  echo "❌ Unity execution completed with errors!"
  echo "   Check log file: $LOG_FILE"
  echo "   Analysis: $ANALYSIS_FILE"
  exit ${EXIT_CODE:-1}
else
  echo ""
  echo "✅ Unity execution completed successfully"
  echo "   Log file: $LOG_FILE"
  echo "   Analysis: $ANALYSIS_FILE"
  exit 0
fi

