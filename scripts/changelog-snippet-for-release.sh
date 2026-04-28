#!/usr/bin/env bash
# Print commits since last v* tag (or last 30) for pasting into GitHub Release notes.
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"
RANGE="$(git describe --tags --abbrev=0 --match 'v*.*.*' 2>/dev/null || echo "")"
if [[ -n "$RANGE" ]]; then
  echo "## Changes since ${RANGE}"
  git log "${RANGE}..HEAD" --oneline --no-decorate | head -n 80
else
  echo "## Recent commits (no prior v*.*.* tag found)"
  git log -n 30 --oneline --no-decorate
fi
