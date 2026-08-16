#!/usr/bin/env bash
# Ops Tier E: end-to-end "oh sh*t" demo (bootstrap, chat, orchestration, dogfood smoke).
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

echo "== Ops Tier E: oh-shit demo (quick) =="
# No --no-build: Tiers A-C only build src/Nexo.Tests.Infrastructure and the
# workflow has no CLI build step, so the demo must build application/src/Nexo.CLI itself.
bash scripts/oh-shit-demo.sh --quick

echo ""
echo "ops-gate-tier-e: PASS"
