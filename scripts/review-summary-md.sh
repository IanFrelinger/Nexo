#!/usr/bin/env bash
set -euo pipefail

IN="${1:-review_summary.json}"
OUT="${2:-REVIEW_SUMMARY.md}"

if [[ ! -s "$IN" ]]; then
  echo "# Review Summary" > "$OUT"
  echo "_No review_summary.json found._" >> "$OUT"
  exit 0
fi

echo "# Review Summary" > "$OUT"
echo "" >> "$OUT"
echo "\`\`\`json" >> "$OUT"
jq '.' "$IN" >> "$OUT"
echo "\`\`\`" >> "$OUT"

# Quick verdict line (best-effort)
OK=$(jq -r '[.seeds[]?.status == "playable" ] | all' "$IN" 2>/dev/null || echo "")
FALL=$(jq -r '[.seeds[]?.fallbacksUsed ] | any' "$IN" 2>/dev/null || echo "")
echo "" >> "$OUT"
echo "- **Playable across seeds:** ${OK:-unknown}" >> "$OUT"
echo "- **Fallbacks used:** ${FALL:-unknown}" >> "$OUT"
echo "" >> "$OUT"

echo "Wrote $OUT"
