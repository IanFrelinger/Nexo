#!/usr/bin/env bash
# Post-RC waterproofing: perf → compat → DR → RC policy tiers.
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

echo "=== Waterproofing gate (perf → compat → DR → RC policy) ==="

PERF_GATE_SKIP_PRIOR=1 bash scripts/perf-gate.sh
COMPAT_GATE_SKIP_PRIOR=1 bash scripts/compat-gate.sh
DR_GATE_SKIP_PRIOR=1 bash scripts/dr-gate.sh

if [ "${WATERPROOFING_SKIP_RC_POLICY:-0}" != "1" ]; then
  bash scripts/rc-gate-tier-e.sh
fi

echo ""
echo "waterproofing-gate: PASS"
