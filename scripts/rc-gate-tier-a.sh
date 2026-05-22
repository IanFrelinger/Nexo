#!/usr/bin/env bash
# RC Tier A: full local readiness stack (nexo-ready-gate).
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

echo "== RC Tier A: nexo-ready-gate (full stack) =="
NEXO_READY_SKIP_DOCKER="${RC_GATE_SKIP_DOCKER:-1}" \
  bash scripts/nexo-ready-gate.sh

echo ""
echo "rc-gate-tier-a: PASS"
