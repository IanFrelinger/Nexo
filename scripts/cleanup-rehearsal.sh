#!/usr/bin/env bash
# Remove only isolated rehearsal resources; shared production/model volumes remain untouched.
# Never pass --volumes here: compose stacks share nexo-ollama-models by explicit name.
set -euo pipefail
project="${1:-nexo-release-rehearsal}"
docker compose -p "$project" down --remove-orphans || true
# Project-prefixed anonymous volumes only (never nexo-ollama-models).
docker volume ls -q --filter "name=^${project}_" | xargs -r docker volume rm || true
docker network ls -q --filter "name=^${project}_" | xargs -r docker network rm || true
docker ps -a --filter "label=com.docker.compose.project=$project" --format '{{.Names}} {{.Status}}'
# Guardrail: shared model volume must survive cleanup.
if ! docker volume inspect nexo-ollama-models >/dev/null 2>&1; then
  echo "warn: shared volume nexo-ollama-models absent after cleanup (ok if never pulled)" >&2
fi
