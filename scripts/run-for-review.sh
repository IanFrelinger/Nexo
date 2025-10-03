#!/usr/bin/env bash
set -euo pipefail

UNITY="/Applications/Unity/Hub/Editor/6000.2.6f1/Unity.app/Contents/MacOS/Unity"
PROJ="$(cd "$(dirname "${BASH_SOURCE[0]}")/../DirectorStudioUnity" && pwd)"
ART="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

echo "🎬 Running DirectorStudio in Review Mode..."

# Set review mode to always-green for guaranteed playable results
MODE="${1:-always-green}"
SEEDS="${2:-1337,7,42}"

echo "Mode: $MODE"
echo "Seeds: $SEEDS"

# Run the review CLI
"$UNITY" -batchmode -nographics \
  -projectPath "$PROJ" \
  -executeMethod NexoDirectorStudio.Editor.CLI.DirectorReviewCli.Run \
  -config "nexo.pipeline.json" \
  -mode "$MODE" \
  -seeds "$SEEDS" \
  -logFile "$ART/review-run.log" \
  -quit

echo "📊 Review Results:"
echo "Summary: $ART/Artifacts/review/summary.json"
echo "JUnit: $ART/Artifacts/review/review.junit.xml"

if [[ -f "$ART/Artifacts/review/summary.json" ]]; then
  echo "Review Summary:"
  cat "$ART/Artifacts/review/summary.json" | head -20
fi

echo "🎯 Review complete! Check Artifacts/review/ for results."
