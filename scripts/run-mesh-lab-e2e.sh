#!/usr/bin/env bash
# One-shot: bring up deploy/compose/docker-compose.mesh-lab.yml, run mesh-lab-verify.sh, tear down.
#
#   ./scripts/run-mesh-lab-e2e.sh
#   ./scripts/run-mesh-lab-e2e.sh .env.mesh-lab   # use your secrets file (gitignored)
#   MESH_LAB_E2E_WORKERS=1 ./scripts/run-mesh-lab-e2e.sh
#   ./scripts/run-mesh-lab-e2e.sh --workers [.env.mesh-lab]
#
# Requires: Docker Engine + Compose v2; publishes peers on 127.0.0.1:18081 and :18082.

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

# grpc.tools protoc sometimes segfaults (exit 139) on linux_arm64 during docker build.
# CI mesh-lab uses amd64 runners; default to amd64 here for reproducibility (QEMU on ARM Macs).
export DOCKER_DEFAULT_PLATFORM="${DOCKER_DEFAULT_PLATFORM:-linux/amd64}"

if ! docker info >/dev/null 2>&1; then
  echo "Docker daemon not reachable — start Docker Desktop (or dockerd), then retry." >&2
  exit 1
fi

COMPOSE_PROJECT_NAME="${COMPOSE_PROJECT_NAME:-nexo_mesh_lab_local}"
export COMPOSE_PROJECT_NAME

WORKERS_PROFILE_ARGS=()
if [[ "${MESH_LAB_E2E_WORKERS:-}" == "1" || "${MESH_LAB_E2E_WORKERS:-}" == "true" ]]; then
  WORKERS_PROFILE_ARGS=(--profile workers)
fi

POSITIONAL=()
for arg in "$@"; do
  case "$arg" in
    --workers)
      WORKERS_PROFILE_ARGS=(--profile workers)
      ;;
    *)
      POSITIONAL+=("$arg")
      ;;
  esac
done

ENV_FILE=""
TEMP_ENV=false
if [[ "${POSITIONAL[0]:-}" == "" ]]; then
  ENV_FILE="$(mktemp "${TMPDIR:-/tmp}/nexo-mesh-lab.XXXXXX.env")"
  TEMP_ENV=true
  KEY="$(openssl rand -hex 24 2>/dev/null || printf 'meshlab%d%s' "$RANDOM" "$$")"
  BEARER="${KEY}-bearer"
  BASIC="${KEY}-basic"
  {
    echo "Nexo__Security__ApiKey=${KEY}"
    echo "Nexo__Security__PeerB__BearerToken=${BEARER}"
    echo "Nexo__Security__Worker__BasicAuthPassword=${BASIC}"
    echo "Nexo__Security__CopilotScopedApiKey=${KEY}-copilot-scoped"
    echo "Nexo__Entitlements__MaxCopilotSubmissionsPerHour=2"
    echo "MESH_LAB_PEER_REGISTRATION_KEY=${KEY}-peer-registration"
    echo "Nexo__Mesh__Persistence__Provider=LiteDb"
    echo "MESH_LAB_PEER_A_PUBLISH=127.0.0.1:18081"
    echo "MESH_LAB_PEER_B_PUBLISH=127.0.0.1:18082"
  } >"$ENV_FILE"
elif [[ -f "${POSITIONAL[0]}" ]]; then
  ENV_FILE="${POSITIONAL[0]}"
else
  echo "Usage: $0 [--workers] [.env.mesh-lab]" >&2
  exit 1
fi

compose_cmd() {
  if [[ "${#WORKERS_PROFILE_ARGS[@]}" -gt 0 ]]; then
    docker compose "${WORKERS_PROFILE_ARGS[@]}" -f deploy/compose/docker-compose.mesh-lab.yml --env-file "$ENV_FILE" "$@"
  else
    docker compose -f deploy/compose/docker-compose.mesh-lab.yml --env-file "$ENV_FILE" "$@"
  fi
}

cleanup() {
  compose_cmd down -v >/dev/null 2>&1 || true
  if [[ "$TEMP_ENV" == true ]]; then
    rm -f "$ENV_FILE"
  fi
}
trap cleanup EXIT

echo "== Mesh lab E2E (project=${COMPOSE_PROJECT_NAME}) =="
if [[ "${#WORKERS_PROFILE_ARGS[@]}" -gt 0 ]]; then
  echo "(Compose profile: workers)"
fi
compose_cmd up -d --build

chmod +x scripts/mesh-lab-verify.sh scripts/mesh-lab-verify-deep.sh 2>/dev/null || true
./scripts/mesh-lab-verify.sh "$ENV_FILE"
if [[ "${MESH_LAB_VERIFY_DEEP:-}" == "1" || "${MESH_LAB_VERIFY_DEEP:-}" == "true" ]]; then
  ./scripts/mesh-lab-verify-deep.sh "$ENV_FILE"
fi

if [[ "${MESH_LAB_RUN_STRESS:-}" == "1" || "${MESH_LAB_RUN_STRESS:-}" == "true" ]]; then
  if [[ "${#WORKERS_PROFILE_ARGS[@]}" -eq 0 ]]; then
    echo "MESH_LAB_RUN_STRESS requires workers profile (MESH_LAB_E2E_WORKERS=1 or --workers)" >&2
    exit 1
  fi
  chmod +x scripts/mesh-lab-stress-ramp.sh 2>/dev/null || true
  STRESS_MAX="${MESH_LAB_STRESS_MAX_WORKERS:-4}"
  STRESS_STEP="${MESH_LAB_STRESS_STEP:-2}"
  STRESS_REQS="${MESH_LAB_STRESS_REQUESTS:-15}"
  STRESS_PAUSE="${MESH_LAB_STRESS_PAUSE_SEC:-2}"
  echo "== Mesh lab stress ramp (max=${STRESS_MAX} step=${STRESS_STEP} reqs=${STRESS_REQS}) =="
  ./scripts/mesh-lab-stress-ramp.sh "$ENV_FILE" "$STRESS_MAX" "$STRESS_STEP" "$STRESS_REQS" "$STRESS_PAUSE"

  if [[ "${MESH_LAB_SKIP_POST_STRESS_VERIFY:-}" != "1" && "${MESH_LAB_SKIP_POST_STRESS_VERIFY:-}" != "true" ]]; then
    chmod +x scripts/mesh-lab-verify-post-stress.sh 2>/dev/null || true
    ./scripts/mesh-lab-verify-post-stress.sh "$ENV_FILE"
  fi
fi

echo "== Mesh lab E2E: OK =="
