#!/usr/bin/env bash
# Open-core / commercial dependency boundary gate.
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

export DEPENDENCY_BOUNDARY_STRICT="${DEPENDENCY_BOUNDARY_STRICT:-1}"
python3 scripts/verify-open-commercial-dependency-boundary.py

echo ""
echo "dependency-boundary-gate: PASS"
