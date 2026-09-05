#!/usr/bin/env bash
# Meta gate: kernel → application → composition/mesh → ship → ops (with Docker tiers skippable).
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

echo "=== Ashlar ready gate (full stack) ==="

if [ "${ASHLAR_READY_SKIP_KERNEL:-0}" != "1" ]; then
  KERNEL_GATE_SKIP_TIER_E="${ASHLAR_READY_SKIP_DOCKER:-1}" \
  KERNEL_GATE_SKIP_TIER_D="${ASHLAR_READY_SKIP_DOCKER:-0}" \
  KERNEL_GATE_SKIP_TIER_C="${ASHLAR_READY_SKIP_DOCKER:-0}" \
    make kernel-gate-full
fi

if [ "${ASHLAR_READY_SKIP_APPLICATION:-0}" != "1" ]; then
  APPLICATION_GATE_SKIP_TIER_D="${ASHLAR_READY_SKIP_DOCKER:-1}" make application-gate-full
fi

if [ "${ASHLAR_READY_SKIP_COMPOSITION:-0}" != "1" ]; then
  COMPOSITION_MESH_GATE_SKIP_TIER_D="${ASHLAR_READY_SKIP_DOCKER:-1}" make composition-mesh-gate-full
fi

if [ "${ASHLAR_READY_SKIP_INGRESS:-0}" != "1" ]; then
  make ingress-unit-gate
fi

if [ "${ASHLAR_READY_SKIP_SHIP:-0}" != "1" ]; then
  SHIP_GATE_SKIP_PRIOR=1 make ship-gate-full
fi

if [ "${ASHLAR_READY_SKIP_OPS:-0}" != "1" ]; then
  OPS_GATE_SKIP_PRIOR=1 \
  OPS_GATE_SKIP_TIER_D="${ASHLAR_READY_SKIP_DOCKER:-1}" \
    make ops-gate-full
fi

if [ "${ASHLAR_READY_SKIP_SECURITY:-0}" != "1" ]; then
  SECURITY_GATE_SKIP_PRIOR=1 \
  SECURITY_GATE_SKIP_TIER_E="${ASHLAR_READY_SKIP_DOCKER:-0}" \
    make security-gate-full
fi

echo ""
echo "ashlar-ready-gate: PASS"
