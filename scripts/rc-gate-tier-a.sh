#!/usr/bin/env bash
# RC Tier A: full local readiness stack (ashlar-ready-gate).
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

echo "== RC Tier A: ashlar-ready-gate (full stack) =="
ASHLAR_READY_SKIP_DOCKER="${RC_GATE_SKIP_DOCKER:-1}" \
  bash scripts/ashlar-ready-gate.sh

echo ""
echo "rc-gate-tier-a: PASS"
