#!/usr/bin/env bash
# Safe validation script: sequential, minimal memory footprint.
# Run from Nexo repo root. Prefer running in external terminal (not Cursor) to avoid memory explosion.
#
# Phase 0: Build + smoke tests + nexo validate (equivalent to make ci-verify)
# Phase 1 (dogfood): Run separately: make dogfood-all or: dotnet run --project src/Nexo.CLI -- dogfood all

set -e

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$REPO_ROOT"

if [ ! -f Nexo.sln ]; then
  echo "validate-safe: Not in Nexo repo (Nexo.sln not found). Run from repository root." >&2
  exit 1
fi

echo "=== Validate Safe: Build ==="
dotnet build -v minimal

echo "=== Validate Safe: Smoke Tests ==="
dotnet test src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj \
  --no-build \
  --blame-hang-timeout 30s \
  --blame-hang-dump-type none \
  --filter "FullyQualifiedName~BaseFrameworkSmokeTests" \
  --verbosity minimal

echo "=== Validate Safe: Architecture Validation ==="
dotnet run --project src/Nexo.CLI -- validate

echo "=== Validate Safe: All checks passed ==="
echo ""
echo "Optional: Run dogfood separately (not in parallel):"
echo "  make dogfood-all"
echo "  or: dotnet run --project src/Nexo.CLI -- dogfood all"
