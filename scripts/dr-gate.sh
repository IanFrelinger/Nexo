#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

echo "=== DR gate ==="

if [ "${DR_GATE_SKIP_PRIOR:-1}" != "1" ]; then
  COMPAT_GATE_SKIP_PRIOR=1 bash scripts/compat-gate.sh
fi

bash scripts/dr-gate-tier-a.sh
bash scripts/dr-gate-tier-b.sh
bash scripts/dr-gate-tier-c.sh

echo ""
echo "dr-gate: PASS"
