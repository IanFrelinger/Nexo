#!/usr/bin/env bash
# Safe validation script: sequential, minimal memory footprint.
# Run from Ashlar repo root. Prefer running in external terminal (not Cursor) to avoid memory explosion.
#
# Phase 0: Build + smoke tests + ashlar validate (equivalent to make ci-verify)
# Phase 1 (dogfood): Run separately: make dogfood-all or: dotnet run --project application/src/Ashlar.CLI -- dogfood all

set -e

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$REPO_ROOT"

if [ ! -f Ashlar.sln ]; then
  echo "validate-safe: Not in Ashlar repo (Ashlar.sln not found). Run from repository root." >&2
  exit 1
fi

echo "=== Validate Safe: Build ==="
dotnet build -v minimal

echo "=== Validate Safe: Smoke Tests ==="
# Counted wrapper: a silent empty BaseFrameworkSmoke filter used to pass.
ASHLAR_ALLOW_MOCK=1 python3 scripts/run-dotnet-test-counted.py \
  --project src/Ashlar.Tests.Infrastructure/Ashlar.Tests.Infrastructure.csproj \
  --expected-prefix "Ashlar.Tests.Infrastructure." \
  --min-tests 9 \
  -- \
  -f net8.0 \
  --no-build \
  --filter "FullyQualifiedName~BaseFrameworkSmokeTests" \
  --blame-hang-timeout 30s \
  --blame-hang-dump-type none

echo "=== Validate Safe: Architecture Validation ==="
dotnet run --project application/src/Ashlar.CLI -- validate

echo "=== Validate Safe: All checks passed ==="
echo ""
echo "Optional: Run dogfood separately (not in parallel):"
echo "  make dogfood-all"
echo "  or: dotnet run --project application/src/Ashlar.CLI -- dogfood all"
