#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

echo "=== Perf gate ==="

if [ "${PERF_GATE_SKIP_PRIOR:-1}" != "1" ]; then
  RC_GATE_SKIP_PRIOR=1 bash scripts/rc-gate.sh
fi

bash scripts/perf-gate-tier-a.sh
bash scripts/perf-gate-tier-b.sh
bash scripts/perf-gate-tier-c.sh
if [ "${PERF_GATE_SKIP_TIER_D:-0}" != "1" ]; then
  bash scripts/perf-gate-tier-d.sh
fi
bash scripts/perf-gate-baseline.sh

echo ""
echo "perf-gate: PASS"
