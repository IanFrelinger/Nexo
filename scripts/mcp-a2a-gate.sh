#!/usr/bin/env bash
# MCP + A2A protocol gate. A raw project run still exits 0 when discovery
# matches nothing — the counted wrapper is the fail-closed runner.
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

MODE="${1:-all}"
export ASHLAR_ALLOW_MOCK="${ASHLAR_ALLOW_MOCK:-1}"

run_adapters() {
  echo "== MCP/A2A: server bridge (net8.0, counted) =="
  python3 scripts/run-dotnet-test-counted.py \
    --project src/Ashlar.Mcp.Server.Tests/Ashlar.Mcp.Server.Tests.csproj \
    --expected-prefix "Ashlar.Mcp.Server.Tests." \
    --min-tests 40 \
    -- \
    -f net8.0 \
    --blame-hang-timeout 120s \
    --blame-hang-dump-type none

  echo ""
  echo "== MCP/A2A: client protocol (net8.0, counted) =="
  python3 scripts/run-dotnet-test-counted.py \
    --project src/Ashlar.Mcp.Client.Tests/Ashlar.Mcp.Client.Tests.csproj \
    --expected-prefix "Ashlar.Mcp.Client.Tests." \
    --min-tests 33 \
    -- \
    -f net8.0 \
    --blame-hang-timeout 120s \
    --blame-hang-dump-type none

  echo ""
  echo "== MCP/A2A: transport (net8.0, counted) =="
  python3 scripts/run-dotnet-test-counted.py \
    --project src/Ashlar.Transport.A2A.Tests/Ashlar.Transport.A2A.Tests.csproj \
    --expected-prefix "Ashlar.Transport.A2A.Tests." \
    --min-tests 39 \
    -- \
    -f net8.0 \
    --blame-hang-timeout 120s \
    --blame-hang-dump-type none

  echo ""
  echo "== MCP/A2A: server TestServer round trips (net10.0, counted) =="
  python3 scripts/run-dotnet-test-counted.py \
    --project src/Ashlar.Transport.A2A.Server.Tests/Ashlar.Transport.A2A.Server.Tests.csproj \
    --expected-prefix "Ashlar.Transport.A2A.Server.Tests." \
    --min-tests 19 \
    -- \
    -f net10.0 \
    --blame-hang-timeout 120s \
    --blame-hang-dump-type none
}

run_prodstyle() {
  echo "== MCP/A2A: API protocol ingress ProdStyle (net10.0, counted) =="
  python3 scripts/run-dotnet-test-counted.py \
    --project src/Ashlar.Tests.Infrastructure/Ashlar.Tests.Infrastructure.csproj \
    --expected-prefix "Ashlar.Tests.Infrastructure." \
    --min-tests 7 \
    -- \
    -f net10.0 \
    --filter "FullyQualifiedName~McpA2AProtocolIngress|FullyQualifiedName~AirGappedProfileApiHostProdStyleTests" \
    --blame-hang-timeout 120s \
    --blame-hang-dump-type none
}

case "$MODE" in
  all)
    run_adapters
    echo ""
    run_prodstyle
    ;;
  adapters)
    run_adapters
    ;;
  prodstyle)
    run_prodstyle
    ;;
  *)
    echo "usage: $0 [all|adapters|prodstyle]" >&2
    exit 64
    ;;
esac

echo ""
echo "mcp-a2a-gate: PASS"
