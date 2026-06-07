#!/usr/bin/env bash
# Application Tier D: agent-server Compose dry run + optional GameDomain smoke.
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

export DOCKER_DEFAULT_PLATFORM="${DOCKER_DEFAULT_PLATFORM:-linux/amd64}"

if command -v docker >/dev/null 2>&1 && docker info >/dev/null 2>&1; then
  echo "== Application Tier D: agent-server prod-shaped dry run =="
  bash scripts/prod-dry-run.sh --agent-server
else
  echo "== Application Tier D: agent-server dry run skipped (no Docker) =="
fi

if [ "${APPLICATION_GATE_GAMEDOMAIN:-0}" = "1" ]; then
  echo "== Application Tier D: GameDomain tests =="
  dotnet test commercial/tests/Nexo.Commercial.Tests.GameDomain/Nexo.Commercial.Tests.GameDomain.csproj -f net8.0 \
    --blame-hang-timeout 120s --blame-hang-dump-type none
else
  echo "== Application Tier D: GameDomain skipped (APPLICATION_GATE_GAMEDOMAIN=1 to enable) =="
fi

echo ""
echo "application-gate-tier-d: PASS"
