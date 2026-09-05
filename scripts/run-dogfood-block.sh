#!/usr/bin/env bash
# Fail-close one Makefile dogfood block.
# `dotnet test --filter` exits 0 when discovery matches nothing.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

CLASS="${1:-}"
MIN_TESTS="${2:-1}"
if [[ -z "${CLASS}" ]]; then
  echo "usage: run-dogfood-block.sh <Dogfood*Tests class> [min-tests]" >&2
  exit 2
fi

INFRA="$ROOT/src/Ashlar.Tests.Infrastructure/Ashlar.Tests.Infrastructure.csproj"

dotnet build "$INFRA" -v minimal
python3 "$ROOT/scripts/run-dotnet-test-counted.py" \
  --project "$INFRA" \
  --expected-prefix "Ashlar.Tests.Infrastructure.Tests.Dogfood.${CLASS}." \
  --min-tests "${MIN_TESTS}" \
  -- \
  --no-build \
  -f net8.0 \
  --filter "FullyQualifiedName~${CLASS}" \
  -v minimal
