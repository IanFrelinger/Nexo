#!/usr/bin/env bash
set -euo pipefail

# Analyze Unity errors and automatically fix syntax errors
# Usage: ./scripts/analyze-and-fix-unity-errors.sh <unity-log-file> <project-path> [output-analysis-json]

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ART="$(cd "$SCRIPT_DIR/.." && pwd)"

UNITY_LOG_FILE="${1:-}"
PROJECT_PATH="${2:-$ART/IterativeGameDemo}"
OUTPUT_ANALYSIS="${3:-}"

if [[ -z "$UNITY_LOG_FILE" ]]; then
  echo "❌ Usage: $0 <unity-log-file> [project-path] [output-analysis-json]"
  exit 1
fi

if [[ ! -f "$UNITY_LOG_FILE" ]]; then
  echo "❌ Unity log file not found: $UNITY_LOG_FILE"
  exit 1
fi

echo "🔍 Analyzing Unity Errors"
echo "========================"
echo "  Log file: $UNITY_LOG_FILE"
echo "  Project: $PROJECT_PATH"
echo ""

# Use Nexo CLI if available
if command -v nexo &> /dev/null || [[ -f "$SCRIPT_DIR/../src/Nexo.CLI/Nexo.CLI.csproj" ]]; then
  # Analyze logs first
  ANALYSIS_OUTPUT="${OUTPUT_ANALYSIS:-${UNITY_LOG_FILE%.log}_analysis.json}"
  
  echo "📋 Step 1: Analyzing Unity logs..."
  if command -v nexo &> /dev/null; then
    nexo unity logs "$UNITY_LOG_FILE" --output "$ANALYSIS_OUTPUT" --format-json 2>&1 | grep -v "^info:" || true
  else
    dotnet run --project "$SCRIPT_DIR/../src/Nexo.CLI/Nexo.CLI.csproj" -- unity logs "$UNITY_LOG_FILE" --output "$ANALYSIS_OUTPUT" --format-json 2>&1 | grep -v "^info:" || true
  fi
  
  # Check if there are syntax errors
  if [[ -f "$ANALYSIS_OUTPUT" ]] && command -v jq &> /dev/null; then
    HAS_ERRORS=$(jq -r '.hasErrors // false' "$ANALYSIS_OUTPUT" 2>/dev/null || echo "false")
    ERROR_COUNT=$(jq -r '.errorCount // 0' "$ANALYSIS_OUTPUT" 2>/dev/null || echo "0")
    
    if [[ "$HAS_ERRORS" == "true" ]] && [[ "$ERROR_COUNT" -gt 0 ]]; then
      echo ""
      echo "⚠️  Found $ERROR_COUNT error(s) in Unity log"
      echo ""
      echo "🤖 Step 2: Attempting to fix syntax errors via orchestration..."
      echo ""
      
      # Create orchestration request to analyze and fix errors
      ORCHESTRATION_REQUEST="Analyze Unity errors from log file $UNITY_LOG_FILE in project $PROJECT_PATH and automatically fix any C# syntax errors found."
      
      # Use orchestrator if available
      if command -v nexo &> /dev/null; then
        nexo orchestrate "$ORCHESTRATION_REQUEST" --format-json 2>&1 | grep -v "^info:" || true
      else
        echo "⚠️  Nexo CLI not available. Install with: dotnet tool install --global Nexo.CLI"
        echo "   For now, errors are logged but not automatically fixed."
      fi
    else
      echo "✅ No errors found in Unity log"
    fi
  fi
else
  echo "⚠️  Nexo CLI not found. Using basic log analysis..."
  
  # Basic error detection
  ERROR_COUNT=$(grep -c "error CS" "$UNITY_LOG_FILE" 2>/dev/null || echo "0")
  if [[ "$ERROR_COUNT" -gt 0 ]]; then
    echo "⚠️  Found $ERROR_COUNT C# compilation error(s)"
    echo "   Install Nexo CLI for automatic error fixing: dotnet tool install --global Nexo.CLI"
  else
    echo "✅ No C# compilation errors found"
  fi
fi

echo ""
echo "📊 Analysis complete"

