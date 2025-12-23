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

echo "🔄 === ITERATIVE GAME CREATION & TESTING DEMO ==="
echo "=================================================="
echo ""
echo "This demo will iteratively create and test a game, using"
echo "playtest feedback to improve the design until quality threshold is met."
echo ""
echo "Configuration:"
echo "  Max iterations: $MAX_ITERATIONS"
echo "  Min quality score: $MIN_QUALITY_SCORE"
echo "  Project: $PROJ"
echo ""

# Step 1: Ensure we have a game specification
if [[ ! -f "$ART/game-specification.json" ]]; then
  echo "📋 No game specification found. Creating one..."
  echo ""
  ./scripts/unity-game-spec-wizard.sh
  if [[ $? -ne 0 ]]; then
    echo "❌ Failed to create game specification"
    exit 1
  fi
  echo ""
else
  echo "✅ Using existing game specification: $ART/game-specification.json"
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
  ./scripts/unity-spec-driven-generator.sh \
    "$ART/game-specification.json" \
    "true" \
    "$ART/iteration-${ITERATION}-generation-results.json"
  
  if [[ $? -ne 0 ]]; then
    echo "❌ Generation failed in iteration $ITERATION"
    exit 1
  fi
  
  echo ""
  echo "✅ Game generated successfully"
  echo ""
  
  # Step 3: Run comprehensive playtesting
  echo "🤖 Step 2: Running AI playtesting..."
  ./scripts/unity-playtest-run.sh \
    "Test iteration $ITERATION" \
    "30" \
    "$ART/iteration-${ITERATION}-playtest-results.json"
  
  if [[ $? -ne 0 ]]; then
    echo "⚠️  Playtesting had issues, but continuing..."
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
    
    # Step 5: If not converged and not last iteration, prepare for next iteration
    if [[ "$CONVERGED" != "true" ]] && [[ $ITERATION -lt $MAX_ITERATIONS ]]; then
      echo "🔄 Step 4: Preparing for next iteration..."
      echo "   (In a full implementation, feedback would be synthesized and spec updated)"
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
    cp "$ART/iteration-${ITERATION}-generation-results.json" "$ITER_DIR/"
  fi
  if [[ -f "$ART/iteration-${ITERATION}-playtest-results.json" ]]; then
    cp "$ART/iteration-${ITERATION}-playtest-results.json" "$ITER_DIR/"
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

