#!/usr/bin/env bash
# Thin wrapper — see e2e-adapt-projects.py
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
if grep -q $'\r' "$0" 2>/dev/null; then
  tmp="$(mktemp)"; tr -d '\r' < "$0" > "$tmp"; chmod +x "$tmp"; exec bash "$tmp" "$@"
fi
exec python3 "$ROOT/scripts/e2e-adapt-projects.py" "$@"
