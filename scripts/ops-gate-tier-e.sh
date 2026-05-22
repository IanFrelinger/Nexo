#!/usr/bin/env bash
# Ops Tier E: end-to-end "oh sh*t" demo (bootstrap, chat, orchestration, dogfood smoke).
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

echo "== Ops Tier E: oh-shit demo (quick) =="
bash scripts/oh-shit-demo.sh --quick --no-build

echo ""
echo "ops-gate-tier-e: PASS"
