#!/usr/bin/env bash
set -euo pipefail
out="${1:-playmode-review-aggregate.junit.xml}"
shift || true
files=("$@")
[[ ${#files[@]} -eq 0 ]] && files=(Artifacts/*/playmode-smoke.junit.xml)

tests=0; fails=0; time=0
body=""
for f in "${files[@]}"; do
  [[ -s "$f" ]] || continue
  t=$(xmllint --xpath 'string(/testsuite/@tests)' "$f" 2>/dev/null || echo 0)
  fz=$(xmllint --xpath 'string(/testsuite/@failures)' "$f" 2>/dev/null || echo 0)
  tm=$(xmllint --xpath 'string(/testsuite/@time)' "$f" 2>/dev/null || echo 0)
  tests=$((tests + ${t:-0}))
  fails=$((fails + ${fz:-0}))
  time=$(python3 -c "print(float('${time}')+float('${tm:-0}'))")
  body+=$(xmllint --xpath '/testsuite/testcase' "$f" 2>/dev/null || echo "")
done
cat > "$out" <<XML
<?xml version="1.0" encoding="UTF-8"?>
<testsuite name="ReviewAggregate" tests="${tests}" failures="${fails}" time="${time}">
${body}
</testsuite>
XML
echo "Wrote $out (tests=${tests}, failures=${fails})"
