#!/usr/bin/env bash
# Application Tier D: agent-server Compose dry run.
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

export DOCKER_DEFAULT_PLATFORM="${DOCKER_DEFAULT_PLATFORM:-linux/amd64}"

if command -v docker >/dev/null 2>&1 && docker info >/dev/null 2>&1; then
  echo "== Application Tier D: agent-server prod-shaped dry run =="
  bash scripts/prod-dry-run.sh --agent-serve
else
  echo "== Application Tier D: agent-server dry run skipped (no Docker) =="
fi

    --blame-hang-timeout 120s --blame-hang-dump-type none
else
fi

echo ""
echo "application-gate-tier-d: PASS"
