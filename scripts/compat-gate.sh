#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

echo "=== Compat gate ==="

if [ "${COMPAT_GATE_SKIP_PRIOR:-1}" != "1" ]; then
  PERF_GATE_SKIP_PRIOR=1 bash scripts/perf-gate.sh
fi

bash scripts/compat-gate-tier-a.sh
bash scripts/compat-gate-tier-b.sh
bash scripts/compat-gate-tier-c.sh

echo ""
echo "compat-gate: PASS"
