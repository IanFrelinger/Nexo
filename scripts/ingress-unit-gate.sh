#!/usr/bin/env bash
# Ingress unit suites that used to be sln-only / UNOWNED (AwsSns + DynamoDb).
# Counted floors are unique identities; a net-framework mismatch cannot pass empty.
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

SNS_TESTS="src/Ashlar.Ingress.AwsSns.Tests/Ashlar.Ingress.AwsSns.Tests.csproj"
DDB_TESTS="src/Ashlar.Ingress.DynamoDb.Tests/Ashlar.Ingress.DynamoDb.Tests.csproj"

echo "== Ingress units: AwsSns (net8.0, counted) =="
python3 scripts/run-dotnet-test-counted.py \
  --project "$SNS_TESTS" \
  --expected-prefix "Ashlar.Ingress.AwsSns.Tests." \
  --min-tests 11 \
  -- \
  -c Release \
  -f net8.0 \
  --blame-hang-timeout 180s \
  --blame-hang-dump-type none

echo ""
echo "== Ingress units: DynamoDb (net8.0, counted) =="
python3 scripts/run-dotnet-test-counted.py \
  --project "$DDB_TESTS" \
  --expected-prefix "Ashlar.Ingress.DynamoDb.Tests." \
  --min-tests 2 \
  -- \
  -c Release \
  -f net8.0 \
  --blame-hang-timeout 180s \
  --blame-hang-dump-type none

echo ""
echo "ingress-unit-gate: PASS"
