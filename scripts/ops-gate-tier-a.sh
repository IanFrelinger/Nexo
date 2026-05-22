#!/usr/bin/env bash
# Ops Tier A: dogfood blocks 1–6 (observe → analyze → adapt → improve → autonomy → self-context).
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

echo "== Ops Tier A: dogfood phase C (blocks 1–6) =="
make dogfood-phase-c

echo ""
echo "ops-gate-tier-a: PASS"
