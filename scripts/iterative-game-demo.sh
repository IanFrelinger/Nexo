#!/usr/bin/env bash
set -euo pipefail

# Iterative Game Creation and Testing Demo
# This script demonstrates the full iterative loop:
# 1. Create/load game specification
# 2. Generate game from specification
# 3. Run AI playtesting
# 4. Analyze balance and generate feedback
# 5. Synthesize feedback into design changes
# 6. Apply changes and iterate (if needed)

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ART="$(cd "$SCRIPT_DIR/.." && pwd)"
PROJ="${PROJ:-$ART/DirectorStudioUnity}"
UNITY="${UNITY_BIN:-/Applications/Unity/Hub/Editor/6000.2.6f1/Unity.app/Contents/MacOS/Unity}"
mkdir -p "$ART"

# Configuration
MAX_ITERATIONS="${MAX_ITERATIONS:-5}"
MIN_QUALITY_SCORE="${MIN_QUALITY_SCORE:-7.0}"
ITERATION_DELAY="${ITERATION_DELAY:-2}"
PROJECT_NAME="${PROJECT_NAME:-IterativeGameDemo}"
OPEN_EDITOR="${OPEN_EDITOR:-true}"
OPEN_EDITOR_EARLY="${OPEN_EDITOR_EARLY:-true}"

# Update project path if using custom project name
if [[ "$PROJECT_NAME" != "DirectorStudioUnity" ]]; then
  PROJ="$ART/$PROJECT_NAME"
fi

echo "🔄 === ITERATIVE GAME CREATION & TESTING DEMO ==="
echo "=================================================="
echo ""
echo "This demo will iteratively create and test a game, using"
echo "playtest feedback to improve the design until quality threshold is met."
echo ""
echo "⚠️  NOTE: This demo requires Unity Editor scripts from DirectorStudioUnity project."
echo "   If you're using a fresh Unity project, some features may not be available."
echo ""
echo "Configuration:"
echo "  Max iterations: $MAX_ITERATIONS"
echo "  Min quality score: $MIN_QUALITY_SCORE"
echo "  Project name: $PROJECT_NAME"
echo "  Project path: $PROJ"
echo "  Open editor: $OPEN_EDITOR"
echo ""

# Step 0: Create Unity project if it doesn't exist
if [[ ! -d "$PROJ" ]] || [[ ! -f "$PROJ/ProjectSettings/ProjectSettings.asset" ]]; then
  echo "📦 Step 0: Creating new Unity project..."
  echo ""
  ./scripts/create-unity-project.sh "$PROJECT_NAME" "$PROJ"
  
  if [[ $? -ne 0 ]]; then
    echo "❌ Failed to create Unity project"
    exit 1
  fi
  echo ""
else
  echo "✅ Unity project already exists: $PROJ"
  echo ""
fi

# Step 1: Ensure we have a game specification
if [[ ! -f "$ART/game-specification.json" ]]; then
  echo "📋 No game specification found. Creating one..."
  echo ""
  
  # Try Unity wizard first
  ./scripts/unity-game-spec-wizard.sh || true
  
  # If wizard failed (e.g., Unity scripts not available), create a default spec
  if [[ ! -f "$ART/game-specification.json" ]]; then
    echo "⚠️  Unity wizard unavailable, creating default game specification..."
    cat > "$ART/game-specification.json" << 'EOF'
{
  "name": "Iterative Game Demo",
  "version": "1.0.0",
  "genre": "Action",
  "description": "A game created through iterative development and AI playtesting",
  "coreMechanics": {
    "playerMovement": "Third-person",
    "combat": "Real-time action",
    "progression": "Level-based"
  },
  "targetPlatform": "PC",
  "artStyle": "Stylized",
  "createdAt": "2024-12-22T00:00:00Z"
}
EOF
    echo "✅ Default game specification created"
  fi
  echo ""
else
  echo "✅ Using existing game specification: $ART/game-specification.json"
  echo ""
fi

# Open Unity editor early if requested (so user can watch changes)
if [[ "$OPEN_EDITOR_EARLY" == "true" ]] && [[ "$OPEN_EDITOR" == "true" ]]; then
  echo "🚀 Opening Unity Editor to watch changes in real-time..."
  echo ""
  ./scripts/open-unity-editor.sh "$PROJ" &
  EDITOR_PID=$!
  echo "   Unity Editor PID: $EDITOR_PID"
  echo "   Editor is opening - you can watch changes as they happen!"
  echo "   Waiting 3 seconds for editor to initialize..."
  sleep 3
  echo ""
fi

# Iteration loop
ITERATION=1
LAST_QUALITY=0.0
CONVERGED=false

while [[ $ITERATION -le $MAX_ITERATIONS ]] && [[ "$CONVERGED" != "true" ]]; do
  echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
  echo "🔄 ITERATION $ITERATION of $MAX_ITERATIONS"
  echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
  echo ""
  
  # Step 2: Generate game from specification
  echo "🎯 Step 1: Generating game from specification..."
  
  # Try Unity-based generation first, fallback to orchestrator
  if ./scripts/unity-spec-driven-generator.sh \
    "$ART/game-specification.json" \
    "true" \
    "$ART/iteration-${ITERATION}-generation-results.json" 2>/dev/null; then
    echo "✅ Game generated successfully (via Unity)"
  else
    echo "⚠️  Unity generation unavailable, using orchestrator..."
    ./scripts/generate-game-via-orchestrator.sh \
      "$ART/game-specification.json" \
      "$ART/Artifacts/iteration-${ITERATION}-generated"
    
    # Create results file for consistency
    if [[ -f "$ART/Artifacts/iteration-${ITERATION}-generated/orchestrator-output.json" ]]; then
      cp "$ART/Artifacts/iteration-${ITERATION}-generated/orchestrator-output.json" \
         "$ART/iteration-${ITERATION}-generation-results.json" 2>/dev/null || true
    fi
  fi
  
  # Create visible Unity assets for this iteration
  echo "🎨 Creating visible Unity assets for iteration $ITERATION..."
  ./scripts/create-unity-demo-assets.sh "$PROJ" "$ITERATION"
  
  echo ""
  
  # Capture Unity logs from generation step
  GENERATION_LOGS_DIR="$ART/UnityLogs"
  mkdir -p "$GENERATION_LOGS_DIR"
  
  # Find the most recent generation log for this iteration
  GENERATION_LOG=$(ls -t "$GENERATION_LOGS_DIR"/unity_generation_*.log 2>/dev/null | head -1)
  GENERATION_ANALYSIS=$(ls -t "$GENERATION_LOGS_DIR"/unity_generation_*_analysis.json 2>/dev/null | head -1)
  
  if [[ -n "$GENERATION_LOG" ]] && [[ -f "$GENERATION_LOG" ]]; then
    echo "📋 Analyzing Unity generation logs..."
    if [[ -z "$GENERATION_ANALYSIS" ]] || [[ ! -f "$GENERATION_ANALYSIS" ]]; then
      GENERATION_ANALYSIS="${GENERATION_LOG%.log}_analysis.json"
      ./scripts/capture-unity-logs.sh "$GENERATION_LOG" "$GENERATION_ANALYSIS" 2>/dev/null || true
    fi
    
    # Check for Unity errors and add to playtest context
    if [[ -f "$GENERATION_ANALYSIS" ]]; then
      HAS_ERRORS=$(jq -r '.hasErrors' "$GENERATION_ANALYSIS" 2>/dev/null || echo "false")
      if [[ "$HAS_ERRORS" == "true" ]]; then
        ERROR_COUNT=$(jq -r '.errorCount' "$GENERATION_ANALYSIS" 2>/dev/null || echo "0")
        echo "   ⚠️  Found $ERROR_COUNT Unity error(s) during generation"
        echo "   These will be included in playtest analysis"
        
        # Add Unity errors to generation results if available
        if [[ -f "$ART/iteration-${ITERATION}-generation-results.json" ]]; then
          jq --slurpfile unity_logs "$GENERATION_ANALYSIS" '
            .unityErrors = $unity_logs[0].errors |
            .unityWarnings = $unity_logs[0].warnings |
            .unityErrorCount = $unity_logs[0].errorCount |
            .hasUnityErrors = $unity_logs[0].hasErrors |
            .unityLogFile = $unity_logs[0].logFile
          ' "$ART/iteration-${ITERATION}-generation-results.json" > "${ART}/iteration-${ITERATION}-generation-results.json.tmp" && \
          mv "${ART}/iteration-${ITERATION}-generation-results.json.tmp" "$ART/iteration-${ITERATION}-generation-results.json"
        fi
      else
        echo "   ✅ No Unity errors found during generation"
      fi
    fi
    echo ""
  fi
  
  # Step 3: Run comprehensive playtesting
  echo "🤖 Step 2: Running AI playtesting..."
  
  # Try Unity-based playtesting first, fallback to orchestrator
  if ./scripts/unity-playtest-run.sh \
    "Test iteration $ITERATION" \
    "30" \
    "$ART/iteration-${ITERATION}-playtest-results.json" 2>/dev/null; then
    echo "✅ Playtesting completed (via Unity)"
  else
    echo "⚠️  Unity playtesting unavailable, using orchestrator..."
    ./scripts/playtest-via-orchestrator.sh \
      "$ART/iteration-${ITERATION}-generation-results.json" \
      "$ART/iteration-${ITERATION}-playtest-results.json"
  fi
  
  echo ""
  
  # Step 4: Check if we have results to analyze
  PLAYTEST_RESULTS="$ART/iteration-${ITERATION}-playtest-results.json"
  if [[ -f "$PLAYTEST_RESULTS" ]]; then
    echo "📊 Step 3: Analyzing playtest results..."
    
    # Extract quality metrics (if available in results)
    # This is a placeholder - actual implementation would parse the JSON
    if command -v jq &> /dev/null; then
      QUALITY=$(jq -r '.qualityScore // .metrics.overallQuality // 0' "$PLAYTEST_RESULTS" 2>/dev/null || echo "0")
      if [[ "$QUALITY" != "null" ]] && [[ "$QUALITY" != "0" ]]; then
        LAST_QUALITY=$QUALITY
        echo "   Quality Score: $QUALITY / 10.0"
        
        # Check if we've met the quality threshold
        if (( $(echo "$QUALITY >= $MIN_QUALITY_SCORE" | bc -l 2>/dev/null || echo "0") )); then
          echo ""
          echo "🎉 Quality threshold met! ($QUALITY >= $MIN_QUALITY_SCORE)"
          CONVERGED=true
        else
          echo "   Target: $MIN_QUALITY_SCORE (need improvement)"
        fi
      else
        echo "   Quality metrics not available in results"
      fi
      
      # Show issue count
      ISSUE_COUNT=$(jq -r '.issues | length // 0' "$PLAYTEST_RESULTS" 2>/dev/null || echo "0")
      if [[ "$ISSUE_COUNT" != "null" ]] && [[ "$ISSUE_COUNT" != "0" ]]; then
        echo "   Issues found: $ISSUE_COUNT"
      fi
    else
      echo "   (jq not available - skipping detailed analysis)"
    fi
    
    echo ""
    
    # Step 5: Synthesize feedback and apply changes for next iteration
    if [[ "$CONVERGED" != "true" ]] && [[ $ITERATION -lt $MAX_ITERATIONS ]]; then
      echo "🔄 Step 4: Synthesizing feedback and updating specification..."
      
      # Archive directory for this iteration (define early)
      ITER_DIR="$ART/Artifacts/iteration-${ITERATION}"
      mkdir -p "$ITER_DIR"
      
      # Synthesize feedback from playtest results
      FEEDBACK_FILE="$ART/iteration-${ITERATION}-feedback-synthesis.json"
      ./scripts/synthesize-feedback.sh "$PLAYTEST_RESULTS" "$FEEDBACK_FILE"
      
      if [[ $? -eq 0 ]] && [[ -f "$FEEDBACK_FILE" ]]; then
        # Apply feedback changes to specification
        echo "   Applying feedback changes to game specification..."
        ./scripts/apply-feedback-changes.sh "$FEEDBACK_FILE" "$ART/game-specification.json" "$ART/game-specification.json"
        
        if [[ $? -eq 0 ]]; then
          echo "   ✅ Specification updated with feedback changes"
          
          # Archive feedback for this iteration
          cp "$FEEDBACK_FILE" "$ITER_DIR/feedback-synthesis.json" 2>/dev/null || true
        else
          echo "   ⚠️  Failed to apply feedback changes, continuing with current spec"
        fi
      else
        echo "   ⚠️  Feedback synthesis failed, continuing with current spec"
      fi
      
      echo "   Waiting ${ITERATION_DELAY}s before next iteration..."
      sleep "$ITERATION_DELAY"
      echo ""
    fi
  else
    echo "⚠️  No playtest results file found at $PLAYTEST_RESULTS"
    echo "   Continuing to next iteration..."
    echo ""
  fi
  
  # Archive this iteration's results
  ITER_DIR="$ART/Artifacts/iteration-${ITERATION}"
  mkdir -p "$ITER_DIR"
  
  if [[ -f "$ART/iteration-${ITERATION}-generation-results.json" ]]; then
    cp "$ART/iteration-${ITERATION}-generation-results.json" "$ITER_DIR/" 2>/dev/null || true
  fi
  if [[ -f "$ART/iteration-${ITERATION}-playtest-results.json" ]]; then
    cp "$ART/iteration-${ITERATION}-playtest-results.json" "$ITER_DIR/" 2>/dev/null || true
  fi
  if [[ -f "$ART/iteration-${ITERATION}-feedback-synthesis.json" ]]; then
    cp "$ART/iteration-${ITERATION}-feedback-synthesis.json" "$ITER_DIR/" 2>/dev/null || true
  fi
  
  ((ITERATION++))
done

echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "📊 === ITERATION SUMMARY ==="
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""
echo "Total iterations: $((ITERATION - 1))"
echo "Final quality score: $LAST_QUALITY"
echo "Converged: $CONVERGED"
echo ""
echo "📁 Results archived in: $ART/Artifacts/"
echo ""

if [[ "$CONVERGED" == "true" ]]; then
  echo "✅ SUCCESS: Game quality threshold met!"
  echo "   The iterative process successfully improved the game design."
elif [[ $ITERATION -gt $MAX_ITERATIONS ]]; then
  echo "⚠️  Maximum iterations reached."
  echo "   Consider:"
  echo "   - Increasing MAX_ITERATIONS"
  echo "   - Adjusting MIN_QUALITY_SCORE"
  echo "   - Reviewing playtest feedback manually"
else
  echo "✅ Iteration complete."
fi

echo ""
echo "💡 Next steps:"
echo "   - Review playtest results: $ART/Artifacts/iteration-*/"
echo "   - Check Unity project for generated scenes: $PROJ/Assets/GeneratedScenes/"
echo "   - Run manual playtest: ./scripts/unity-simple-playtest.sh"
echo ""

# Open Unity editor if requested (and not already opened early)
if [[ "$OPEN_EDITOR" == "true" ]] && [[ "$OPEN_EDITOR_EARLY" != "true" ]]; then
  echo "🚀 Opening Unity Editor..."
  echo ""
  ./scripts/open-unity-editor.sh "$PROJ"
  echo ""
  echo "✅ Unity Editor should be opening now!"
  echo "   You can explore the generated game in the Unity project."
elif [[ "$OPEN_EDITOR_EARLY" == "true" ]]; then
  echo "✅ Unity Editor is already open - check it to see the changes!"
fi
echo ""

