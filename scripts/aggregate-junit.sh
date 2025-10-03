#!/usr/bin/env bash
set -euo pipefail

# Aggregate multiple per-run JUnit files into a single testsuite for easy CI consumption.
# Usage:
#   scripts/aggregate-junit.sh playmode-review-aggregate.junit.xml Artifacts/*/playmode-smoke.junit.xml
# If no files are passed, it will glob Artifacts/*/playmode-smoke.junit.xml

OUT="${1:-playmode-review-aggregate.junit.xml}"
shift || true
FILES=("$@")
if [[ ${#FILES[@]} -eq 0 ]]; then
  # Use find instead of mapfile for better compatibility
  while IFS= read -r -d '' file; do
    FILES+=("$file")
  done < <(find Artifacts -name "playmode-smoke.junit.xml" -print0 2>/dev/null || true)
fi

# Ensure FILES is defined even if empty
FILES=("${FILES[@]:-}")

tests=0; fails=0; total_time=0
BODY=""

need_xmllint=0
if ! command -v xmllint >/dev/null 2>&1; then
  echo "WARN: xmllint not found; attempting naive concatenation (no totals)." >&2
  need_xmllint=1
fi

for f in "${FILES[@]}"; do
  [[ -s "$f" ]] || continue
  if [[ $need_xmllint -eq 0 ]]; then
    t=$(xmllint --xpath 'string(/testsuite/@tests)' "$f" 2>/dev/null || echo 0)
    z=$(xmllint --xpath 'string(/testsuite/@failures)' "$f" 2>/dev/null || echo 0)
    tm=$(xmllint --xpath 'string(/testsuite/@time)' "$f" 2>/dev/null || echo 0)
    tests=$((tests + ${t:-0}))
    fails=$((fails + ${z:-0}))
    total_time=$(python3 - <<EOF
import sys
print(float("${total_time}") + float("${tm or 0}"))
EOF
)
    BODY+=$(xmllint --xpath '/testsuite/testcase' "$f" 2>/dev/null || echo "")
  else
    # Fallback: crude grep of testcases
    BODY+=$(grep -o '<testcase[^>]*>.*</testcase>' "$f" || true)
  fi
done

cat > "$OUT" <<XML
<?xml version="1.0" encoding="UTF-8"?>
<testsuite name="ReviewAggregate" tests="${tests}" failures="${fails}" time="${total_time}">
${BODY}
</testsuite>
XML

echo "Wrote $OUT (tests=${tests}, failures=${fails})"