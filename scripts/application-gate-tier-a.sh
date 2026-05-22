#!/usr/bin/env bash
# Application Tier A: build product solution + CLI smoke.
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

if [ "${APPLICATION_GATE_SKIP_KERNEL:-0}" != "1" ]; then
  echo "== Application Tier A: kernel prerequisite (make kernel-gate) =="
  make kernel-gate
fi

echo "== Application Tier A: build Nexo.Application.sln =="
dotnet build application/Nexo.Application.sln -v minimal

echo "== Application Tier A: CLI --help =="
dotnet run --project application/src/Nexo.CLI/Nexo.CLI.csproj --no-build -- --help >/dev/null

echo "== Application Tier A: pipeline validate smoke =="
TMP="$(mktemp -d)"
trap 'rm -rf "$TMP"' EXIT
cat > "$TMP/smoke.json" <<'JSON'
{
  "templateId": "app-gate-smoke",
  "version": "1",
  "stages": [{ "id": "s1", "name": "S1", "mode": "Deterministic" }]
}
JSON
NEXO_ALLOW_MOCK=1 dotnet run --project application/src/Nexo.CLI/Nexo.CLI.csproj --no-build -- \
  pipeline validate --template "$TMP/smoke.json"

echo ""
echo "application-gate-tier-a: PASS"
