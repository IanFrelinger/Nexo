#!/usr/bin/env bash
# Ops Tier C: closed-loop self-improvement on Ashlar codebase.
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

echo "== Ops Tier C: dogfood closed-loop =="
make dogfood-closedloop

if [ "${OPS_GATE_RUN_PHASE_F:-0}" = "1" ]; then
  echo "== Ops Tier C: dogfood phase F =="
  make dogfood-phasef
fi

echo ""
echo "ops-gate-tier-c: PASS"
