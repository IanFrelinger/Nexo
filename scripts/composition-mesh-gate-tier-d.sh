#!/usr/bin/env bash
# Composition + mesh Tier D: Docker mesh virtual lab (workers + async task schedule/placement).
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

export DOCKER_DEFAULT_PLATFORM="${DOCKER_DEFAULT_PLATFORM:-linux/amd64}"

if ! command -v docker >/dev/null 2>&1 || ! docker info >/dev/null 2>&1; then
  echo "error: composition-mesh-gate-tier-d requires a working Docker daemon; refusing to skip the mesh lab" >&2
  exit 2
fi

ENV_FILE="${1:-.env.mesh-lab}"
if [[ ! -f "$ENV_FILE" ]]; then
  echo "== Mesh Tier D: one-shot E2E (temp secrets) =="
  MESH_LAB_E2E_WORKERS=1 bash scripts/run-mesh-lab-e2e.sh
else
  echo "== Mesh Tier D: workers profile + verify ($ENV_FILE) =="
  MESH_LAB_E2E_WORKERS=1 bash scripts/run-mesh-lab-e2e.sh "$ENV_FILE"
fi

if [[ "${COMPOSITION_MESH_GATE_DEEP:-0}" = "1" ]]; then
  echo "== Mesh Tier D: deep verify (checkpoint / migrate / reschedule) =="
  if [[ -f "$ENV_FILE" ]]; then
    ./scripts/mesh-lab-verify-deep.sh "$ENV_FILE"
  else
    echo "Deep verify requires persistent .env.mesh-lab; set COMPOSITION_MESH_GATE_DEEP=0 or provide env file" >&2
    exit 1
  fi
fi

echo ""
echo "composition-mesh-gate-tier-d: PASS"
