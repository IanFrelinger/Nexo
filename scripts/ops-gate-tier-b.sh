#!/usr/bin/env bash
# Ops Tier B: dogfood blocks 7–9 + local IPC mesh.
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

echo "== Ops Tier B: dogfood phase D+E (blocks 7–9) =="
make dogfood-phase-de
make dogfood-block9-ipc

echo ""
echo "ops-gate-tier-b: PASS"
