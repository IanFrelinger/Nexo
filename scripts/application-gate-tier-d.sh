#!/usr/bin/env bash
# Application Tier D: agent-server Compose dry run.
# This tier is the dry run. Skipping it and printing PASS used to hide a
# missing engine behind a green gate.
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

export DOCKER_DEFAULT_PLATFORM="${DOCKER_DEFAULT_PLATFORM:-linux/amd64}"

if ! command -v docker >/dev/null 2>&1 || ! docker info >/dev/null 2>&1; then
  echo "error: application-gate-tier-d requires a working Docker daemon; refusing to skip the agent-server dry run" >&2
  exit 2
fi

echo "== Application Tier D: agent-server prod-shaped dry run =="
bash scripts/prod-dry-run.sh --agent-server

echo ""
echo "application-gate-tier-d: PASS"
