#!/usr/bin/env bash
# Security Tier E: air-gapped no-network smoke (kernel/profile + safety probes).
# Validates that core flows do not require network egress.
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

INFRA="src/Ashlar.Tests.Infrastructure/Ashlar.Tests.Infrastructure.csproj"

if [ "${SECURITY_GATE_AIRGAPPED_CONTAINER:-0}" = "1" ]; then
  if ! command -v docker >/dev/null 2>&1 || ! docker info >/dev/null 2>&1; then
    echo "error: SECURITY_GATE_AIRGAPPED_CONTAINER=1 requires a working Docker daemon; refusing to skip --network none proof" >&2
    exit 1
  fi
fi

echo "== Security Tier E: air-gapped + safety in-process tests =="
dotnet build "$INFRA" -f net8.0 -v minimal
ASHLAR_ALLOW_MOCK=1 dotnet test "$INFRA" -f net8.0 --no-build \
  --filter "FullyQualifiedName~AirGapped|FullyQualifiedName~Ashlar.Tests.Infrastructure.Tests.Safety" \
  --blame-hang-timeout 120s --blame-hang-dump-type none

if [ "${SECURITY_GATE_AIRGAPPED_CONTAINER:-0}" = "1" ]; then
  echo "== Security Tier E: --network none container suite =="
  dotnet run --project application/src/Ashlar.CLI/Ashlar.CLI.csproj -- \
    test multi-env --suite framework --env ubuntu-8.0 --no-network
fi

echo ""
echo "security-gate-tier-e: PASS"
