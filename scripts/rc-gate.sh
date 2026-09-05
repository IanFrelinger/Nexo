#!/usr/bin/env bash
# RC gate: local readiness + release bundle + evidence + GitHub workflow verification.
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

echo "=== RC gate (release candidate) ==="

if [ "${RC_GATE_SKIP_PRIOR:-1}" != "1" ]; then
  bash scripts/rc-gate-tier-a.sh
else
  echo "== RC Tier A: skipped (RC_GATE_SKIP_PRIOR=1) =="
fi

bash scripts/rc-gate-tier-b.sh
SECURITY_GATE_STRICT_SUPPLY_CHAIN=1 bash scripts/security-gate-tier-d.sh
bash scripts/rc-gate-tier-c.sh
bash scripts/rc-gate-tier-d.sh

if [ "${RC_GATE_SKIP_TIER_E:-0}" != "1" ]; then
  bash scripts/rc-gate-tier-e.sh
fi

echo ""
echo "rc-gate: PASS"
