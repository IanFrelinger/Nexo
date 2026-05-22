#!/usr/bin/env bash
# Compat Tier B: cross-version CLI durability (LiteDB resume across processes).
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

# Reuse kernel Tier B resume scenario (same CLI + store contract).
bash scripts/kernel-gate-tier-b.sh | tee /tmp/compat-gate-tier-b.log

REPORT_DIR=".nexo/compat"
mkdir -p "$REPORT_DIR"
cp /tmp/compat-gate-tier-b.log "$REPORT_DIR/cli-resume.log" 2>/dev/null || true

echo ""
echo "compat-gate-tier-b: PASS"
