#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ART="$(cd "$SCRIPT_DIR/.." && pwd)"
PROJ="${PROJ:-$ART/DirectorStudioUnity}"
UNITY="${UNITY_BIN:-/Applications/Unity/Hub/Editor/6000.2.6f1/Unity.app/Contents/MacOS/Unity}"
mkdir -p "$ART"

# Default parameters
PROMPT="${1:-FPS shooter with enemies, power-ups, and keycard doors}"
DURATION="${2:-30}"
RESULTS="${3:-$ART/playtest-results.json}"

echo "🤖 Starting AI Playtest Pipeline"
echo "📝 Prompt: $PROMPT"
echo "⏱️ Duration: ${DURATION}s"
echo "📊 Results: $RESULTS"
echo ""

# Create logs directory
LOGS_DIR="$ART/UnityLogs"
mkdir -p "$LOGS_DIR"

# Generate unique log file name
TIMESTAMP=$(date +"%Y%m%d_%H%M%S")
LOG_FILE="$LOGS_DIR/unity_playtest_${TIMESTAMP}.log"

# Use Nexo CLI if available (check both global install and dotnet run), otherwise fallback to direct Unity call
if command -v nexo &> /dev/null || [[ -f "$SCRIPT_DIR/../src/Nexo.CLI/Nexo.CLI.csproj" ]]; then
  UNITY_ARGS=(
    -prompt "$PROMPT"
    -testDuration "$DURATION"
    -enableMetrics true
    -results "$RESULTS"
  )
  
  # Use dotnet run if nexo not globally installed
  if command -v nexo &> /dev/null; then
    if [[ -n "${UNITY_BIN:-}" ]]; then
      nexo unity run "$PROJ" "NexoDirectorStudio.Editor.PlaytestCliRunner.Run" \
        --unity-bin "${UNITY_BIN}" \
        --log-file "$LOG_FILE" \
        --args "${UNITY_ARGS[@]}"
    else
      nexo unity run "$PROJ" "NexoDirectorStudio.Editor.PlaytestCliRunner.Run" \
        --log-file "$LOG_FILE" \
        --args "${UNITY_ARGS[@]}"
    fi
  else
    # Use dotnet run as fallback
    if [[ -n "${UNITY_BIN:-}" ]]; then
      dotnet run --project "$SCRIPT_DIR/../src/Nexo.CLI/Nexo.CLI.csproj" -- unity run "$PROJ" "NexoDirectorStudio.Editor.PlaytestCliRunner.Run" \
        --unity-bin "${UNITY_BIN}" \
        --log-file "$LOG_FILE" \
        --args "${UNITY_ARGS[@]}"
    else
      dotnet run --project "$SCRIPT_DIR/../src/Nexo.CLI/Nexo.CLI.csproj" -- unity run "$PROJ" "NexoDirectorStudio.Editor.PlaytestCliRunner.Run" \
        --log-file "$LOG_FILE" \
        --args "${UNITY_ARGS[@]}"
    fi
  fi
else
  echo "⚠️  Nexo CLI not found, using direct Unity call..."
  "$UNITY" \
    -batchmode -nographics \
    -projectPath "$PROJ" \
    -executeMethod NexoDirectorStudio.Editor.PlaytestCliRunner.Run \
    -prompt "$PROMPT" \
    -testDuration "$DURATION" \
    -enableMetrics true \
    -results "$RESULTS" \
    -logFile "$LOG_FILE" \
    -quit
  
  # Analyze logs for errors
  if [[ -f "$LOG_FILE" ]]; then
    ANALYSIS_FILE="$LOGS_DIR/unity_playtest_${TIMESTAMP}_analysis.json"
    ./scripts/capture-unity-logs.sh "$LOG_FILE" "$ANALYSIS_FILE" 2>/dev/null || true
  fi
fi

echo ""
echo "📊 Playtest Results:"
if [[ -f "$RESULTS" ]]; then
  echo "✅ Results saved to: $RESULTS"
  echo "📄 Content preview:"
  head -20 "$RESULTS"
else
  echo "❌ No results file generated"
fi

echo ""
if [[ -n "${LOG_FILE:-}" ]] && [[ -f "${LOG_FILE:-}" ]]; then
  echo "📋 Log file: $LOG_FILE"
  if [[ -f "${ANALYSIS_FILE:-}" ]]; then
    echo "📊 Log analysis: $ANALYSIS_FILE"
  fi
else
  echo "📋 Log file: $ART/unity-playtest.log"
fi
