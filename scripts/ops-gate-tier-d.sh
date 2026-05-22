#!/usr/bin/env bash
# Ops Tier D: mesh resilience (deep E2E or chaos-lite network-negative).
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

export DOCKER_DEFAULT_PLATFORM="${DOCKER_DEFAULT_PLATFORM:-linux/amd64}"

if ! command -v docker >/dev/null 2>&1 || ! docker info >/dev/null 2>&1; then
  echo "== Ops Tier D: skipped (Docker not available) =="
  exit 0
fi

if [ "${OPS_GATE_MESH_DEEP:-0}" = "1" ]; then
  echo "== Ops Tier D: mesh lab E2E deep (workers + checkpoint/migrate) =="
  MESH_ENV="${OPS_GATE_MESH_ENV:-}"
  if [ -z "$MESH_ENV" ] && [ -f ".env.mesh-lab" ]; then
    MESH_ENV=".env.mesh-lab"
  fi
  if [ -n "$MESH_ENV" ]; then
    MESH_LAB_E2E_WORKERS=1 MESH_LAB_VERIFY_DEEP=1 bash scripts/run-mesh-lab-e2e.sh "$MESH_ENV"
  else
    MESH_LAB_E2E_WORKERS=1 MESH_LAB_VERIFY_DEEP=1 bash scripts/run-mesh-lab-e2e.sh
  fi
elif [ "${OPS_GATE_CHAOS_LITE:-0}" = "1" ] && [ -f ".env.mesh-lab" ]; then
  echo "== Ops Tier D: chaos-lite (network-negative on running lab) =="
  make mesh-lab-up
  ./scripts/mesh-lab-verify-network-negative.sh .env.mesh-lab
  make mesh-lab-down
else
  echo "== Ops Tier D: skipped (OPS_GATE_MESH_DEEP=1 or OPS_GATE_CHAOS_LITE=1 + .env.mesh-lab) =="
fi

echo ""
echo "ops-gate-tier-d: PASS"
