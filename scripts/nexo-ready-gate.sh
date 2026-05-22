#!/usr/bin/env bash
# Meta gate: kernel → application → composition/mesh → ship → ops (with Docker tiers skippable).
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

echo "=== Nexo ready gate (full stack) ==="

if [ "${NEXO_READY_SKIP_KERNEL:-0}" != "1" ]; then
  KERNEL_GATE_SKIP_TIER_E="${NEXO_READY_SKIP_DOCKER:-1}" \
  KERNEL_GATE_SKIP_TIER_D="${NEXO_READY_SKIP_DOCKER:-0}" \
  KERNEL_GATE_SKIP_TIER_C="${NEXO_READY_SKIP_DOCKER:-0}" \
    make kernel-gate-full
fi

if [ "${NEXO_READY_SKIP_APPLICATION:-0}" != "1" ]; then
  APPLICATION_GATE_SKIP_TIER_D="${NEXO_READY_SKIP_DOCKER:-1}" make application-gate-full
fi

if [ "${NEXO_READY_SKIP_COMPOSITION:-0}" != "1" ]; then
  COMPOSITION_MESH_GATE_SKIP_TIER_D="${NEXO_READY_SKIP_DOCKER:-1}" make composition-mesh-gate-full
fi

if [ "${NEXO_READY_SKIP_SHIP:-0}" != "1" ]; then
  SHIP_GATE_SKIP_PRIOR=1 make ship-gate-full
fi

if [ "${NEXO_READY_SKIP_OPS:-0}" != "1" ]; then
  OPS_GATE_SKIP_PRIOR=1 \
  OPS_GATE_SKIP_TIER_D="${NEXO_READY_SKIP_DOCKER:-1}" \
    make ops-gate-full
fi

if [ "${NEXO_READY_SKIP_SECURITY:-0}" != "1" ]; then
  SECURITY_GATE_SKIP_PRIOR=1 \
  SECURITY_GATE_SKIP_TIER_E="${NEXO_READY_SKIP_DOCKER:-0}" \
    make security-gate-full
fi

echo ""
echo "nexo-ready-gate: PASS"
