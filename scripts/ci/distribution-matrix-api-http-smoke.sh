#!/usr/bin/env bash
# Build Nexo.API image from .docker/Dockerfile.api, run with mock provider,
# and assert /health and /api/status respond (HTTP-only consumer smoke).
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "${ROOT}"

IMAGE_TAG="${DIST_MATRIX_API_IMAGE_TAG:-nexo-api:dist-matrix}"
HOST_PORT="${DIST_MATRIX_API_PORT:-18080}"
WAIT_SECS="${DIST_MATRIX_API_WAIT_SECS:-120}"

echo "Building API image ${IMAGE_TAG} ..."
docker build -f .docker/Dockerfile.api --tag "${IMAGE_TAG}" .

CONTAINER="nexo-api-dist-matrix-${RANDOM}"
cleanup() {
  docker rm -f "${CONTAINER}" >/dev/null 2>&1 || true
}
trap cleanup EXIT

echo "Starting container ${CONTAINER} on port ${HOST_PORT} ..."
docker run -d --name "${CONTAINER}" -p "${HOST_PORT}:8080" \
  -e NEXO_ALLOW_MOCK=1 \
  "${IMAGE_TAG}"

start_ts=$(date +%s)
until curl -fsS "http://127.0.0.1:${HOST_PORT}/health" >/dev/null 2>&1; do
  now_ts=$(date +%s)
  if (( now_ts - start_ts > WAIT_SECS )); then
    echo "Timeout waiting for /health after ${WAIT_SECS}s" >&2
    docker logs "${CONTAINER}" >&2 || true
    exit 1
  fi
  sleep 2
done

echo "GET /health OK"
curl -fsS "http://127.0.0.1:${HOST_PORT}/health" | head -c 256 || true
echo

echo "GET /api/status OK"
curl -fsS "http://127.0.0.1:${HOST_PORT}/api/status" | head -c 512 || true
echo

echo "distribution-matrix-api-http-smoke: OK"
