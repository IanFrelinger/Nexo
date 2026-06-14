#!/usr/bin/env bash
# Advisory-only size check for CLI command files.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
LIMIT="${1:-600}"

status=0
shopt -s globstar nullglob
for file in "$ROOT"/application/src/Nexo.CLI/Commands/**/*.cs; do
  lines="$(wc -l < "$file" | tr -d ' ')"
  if [ "$lines" -gt "$LIMIT" ]; then
    rel="${file#"$ROOT"/}"
    echo "warning: ${rel} has ${lines} lines (advisory limit: ${LIMIT})"
    status=1
  fi
done

if [ "$status" -eq 0 ]; then
  echo "command-file-size: OK (limit ${LIMIT})"
else
  echo "command-file-size: advisory warnings only"
fi

exit 0
