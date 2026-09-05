#!/usr/bin/env bash
# Ops Gate Tier C — closed-loop self-improvement on this repository.
#
# Makefile closed-loop and phase-F targets remain developer shortcuts. This
# gate does not invoke them. A raw project-wide filter can pass while the
# listed class is missing, so the counted wrapper is the only path here.
# Phase F stays opt-in via OPS_GATE_RUN_PHASE_F=1 and is also counted.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

INFRA="$ROOT/src/Ashlar.Tests.Infrastructure/Ashlar.Tests.Infrastructure.csproj"
MIN_CLOSEDLOOP="${OPS_GATE_MIN_CLOSEDLOOP_TESTS:-1}"
MIN_PHASE_F="${OPS_GATE_MIN_PHASE_F_TESTS:-2}"

echo "==> ops-gate-tier-c: counted closed-loop (min ${MIN_CLOSEDLOOP})"
python3 "$ROOT/scripts/run-dotnet-test-counted.py" \
  --project "$INFRA" \
  --expected-prefix "Ashlar.Tests.Infrastructure.Tests.Dogfood.DogfoodClosedLoopTests." \
  --min-tests "$MIN_CLOSEDLOOP" \
  -- \
  -c Release \
  -f net8.0 \
  --filter "FullyQualifiedName~DogfoodClosedLoopTests" \
  --verbosity minimal

if [ "${OPS_GATE_RUN_PHASE_F:-0}" = "1" ]; then
  echo "==> ops-gate-tier-c: counted phase F (min ${MIN_PHASE_F})"
  python3 "$ROOT/scripts/run-dotnet-test-counted.py" \
    --project "$INFRA" \
    --expected-prefix "Ashlar.Tests.Infrastructure.Tests.Dogfood.DogfoodPhaseFTests." \
    --min-tests "$MIN_PHASE_F" \
    -- \
    -c Release \
    -f net8.0 \
    --filter "FullyQualifiedName~DogfoodPhaseFTests" \
    --verbosity minimal
fi

echo ""
echo "ops-gate-tier-c: PASS (verified: counted-closed-loop)"
