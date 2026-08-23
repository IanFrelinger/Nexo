#!/usr/bin/env bash
# Smoke published GHCR images by immutable sha tag: CLI --help, API /health (ephemeral container).
set -euo pipefail
CLI_IMAGE="${RELEASE_SMOKE_CLI_IMAGE:?set RELEASE_SMOKE_CLI_IMAGE (e.g. ghcr.io/org/nexo-cli:sha-abc)}"
API_IMAGE="${RELEASE_SMOKE_API_IMAGE:?set RELEASE_SMOKE_API_IMAGE}"

echo "CLI: ${CLI_IMAGE}"
docker run --rm "${CLI_IMAGE}" --help

echo "API: ${API_IMAGE}"
cid="$(docker run -d -p 127.0.0.1:18080:8080 "${API_IMAGE}")"
trap 'docker rm -f "${cid}" >/dev/null 2>&1 || true' EXIT
for i in $(seq 1 30); do
  if curl -fsS "http://127.0.0.1:18080/health" >/dev/null 2>&1; then
    echo "release-smoke-published-docker: OK"
    exit 0
  fi
  sleep 2
done
echo "::error::API health check failed"
exit 1
