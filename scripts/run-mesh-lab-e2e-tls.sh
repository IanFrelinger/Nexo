#!/usr/bin/env bash
# One-shot: mesh lab + TLS override + mesh-lab-verify-tls.sh
#
#   ./scripts/run-mesh-lab-e2e-tls.sh [.env.mesh-lab]

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

export DOCKER_DEFAULT_PLATFORM="${DOCKER_DEFAULT_PLATFORM:-linux/amd64}"
COMPOSE_PROJECT_NAME="${COMPOSE_PROJECT_NAME:-nexo_mesh_lab_tls_local}"
export COMPOSE_PROJECT_NAME

ENV_FILE="${1:-}"
TEMP_ENV=false
if [[ -z "$ENV_FILE" || ! -f "$ENV_FILE" ]]; then
  ENV_FILE="$(mktemp "${TMPDIR:-/tmp}/nexo-mesh-lab-tls.XXXXXX.env")"
  TEMP_ENV=true
  KEY="$(openssl rand -hex 24 2>/dev/null || printf 'meshlabtls%d%s' "$RANDOM" "$$")"
  {
    echo "Nexo__Security__ApiKey=${KEY}"
    echo "Nexo__Security__PeerB__BearerToken=${KEY}-bearer"
    echo "Nexo__Security__Worker__BasicAuthPassword=${KEY}-basic"
    echo "MESH_LAB_PEER_REGISTRATION_KEY=${KEY}-peer-registration"
    echo "Nexo__Mesh__Persistence__Provider=LiteDb"
    echo "MESH_LAB_PEER_A_PUBLISH=127.0.0.1:18081"
    echo "MESH_LAB_PEER_B_PUBLISH=127.0.0.1:18082"
    echo "MESH_LAB_TLS_PUBLISH=127.0.0.1:18443"
  } >"$ENV_FILE"
fi

cleanup() {
  docker compose -f deploy/compose/docker-compose.mesh-lab.yml -f deploy/compose/docker-compose.mesh-lab-tls.override.yml \
    --env-file "$ENV_FILE" down -v >/dev/null 2>&1 || true
  if [[ "$TEMP_ENV" == true ]]; then
    rm -f "$ENV_FILE"
  fi
}
trap cleanup EXIT

chmod +x scripts/mesh-lab-tls-certs.sh scripts/mesh-lab-tls-curl.sh
# shellcheck source=scripts/mesh-lab-tls-curl.sh
source "${ROOT}/scripts/mesh-lab-tls-curl.sh"
export MESH_LAB_TLS_CERT_DIR
MESH_LAB_TLS_CERT_DIR="$(./scripts/mesh-lab-tls-certs.sh)"
export MESH_LAB_TLS_CERT_DIR

echo "== Mesh lab TLS E2E (project=${COMPOSE_PROJECT_NAME}) =="
docker compose -f deploy/compose/docker-compose.mesh-lab.yml -f deploy/compose/docker-compose.mesh-lab-tls.override.yml \
  --env-file "$ENV_FILE" up -d --build

for _i in $(seq 1 60); do
  if mesh_lab_tls_curl "https://127.0.0.1:18443/health" >/dev/null 2>&1; then
    break
  fi
  sleep 2
done

chmod +x scripts/mesh-lab-verify-tls.sh
./scripts/mesh-lab-verify-tls.sh "$ENV_FILE"

echo "== Mesh lab TLS E2E: OK =="
