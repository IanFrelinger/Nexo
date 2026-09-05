#!/usr/bin/env bash
# Security Tier D: supply-chain — vulnerable + deprecated package scan.
# Scans application + key kernel projects (avoids Ashlar.Core YamlDotNet registration bug on some NuGet clients).
# A failed scan or any vulnerable package is a FAIL.
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

REPORT_DIR=".ashlar/security-gate"
mkdir -p "$REPORT_DIR"
VULN_REPORT="$REPORT_DIR/vulnerable-packages.txt"
DEPRECATED_REPORT="$REPORT_DIR/deprecated-packages.txt"
SCAN_FAIL=0
VULN_FOUND=0

scan_vulnerable() {
  local target="$1"
  echo "--- $target ---" | tee -a "$VULN_REPORT"
  set +e
  local out
  out="$(dotnet list "$target" package --vulnerable --include-transitive 2>&1)"
  local ec=$?
  set -e
  echo "$out" | tee -a "$VULN_REPORT"
  if [ "$ec" -ne 0 ]; then
    echo "::warning::vulnerable scan failed for $target (exit $ec)" >&2
    SCAN_FAIL=1
    return
  fi
  if echo "$out" | grep -qiE 'has the following vulnerable|Severity:'; then
    VULN_FOUND=1
  fi
}

scan_deprecated() {
  local target="$1"
  echo "--- $target ---" | tee -a "$DEPRECATED_REPORT"
  set +e
  local out
  out="$(dotnet list "$target" package --deprecated 2>&1)"
  local ec=$?
  set -e
  echo "$out" | tee -a "$DEPRECATED_REPORT"
  if [ "$ec" -ne 0 ]; then
    echo "::warning::deprecated scan failed for $target (exit $ec)" >&2
    SCAN_FAIL=1
  fi
}

: >"$VULN_REPORT"
: >"$DEPRECATED_REPORT"

echo "== Security Tier D: restore =="
dotnet restore application/Ashlar.Application.sln >/dev/null
dotnet restore src/Ashlar.Hosting/Ashlar.Hosting.csproj >/dev/null
dotnet restore src/Ashlar.Infrastructure/Ashlar.Infrastructure.csproj >/dev/null

echo "== Security Tier D: dotnet list package --vulnerable =="
scan_vulnerable application/Ashlar.Application.sln
scan_vulnerable src/Ashlar.Hosting/Ashlar.Hosting.csproj
scan_vulnerable src/Ashlar.Infrastructure/Ashlar.Infrastructure.csproj

echo "== Security Tier D: dotnet list package --deprecated =="
scan_deprecated application/Ashlar.Application.sln
scan_deprecated src/Ashlar.Hosting/Ashlar.Hosting.csproj
scan_deprecated src/Ashlar.Infrastructure/Ashlar.Infrastructure.csproj

if [ "$SCAN_FAIL" -eq 1 ]; then
  echo "security-gate-tier-d: FAIL (supply-chain scan could not run; see $REPORT_DIR)" >&2
  exit 1
fi

if [ "$VULN_FOUND" -eq 1 ]; then
  echo "Vulnerable packages detected — see $VULN_REPORT" >&2
  echo "security-gate-tier-d: FAIL (vulnerable packages; see $REPORT_DIR)" >&2
  exit 1
fi

echo ""
echo "security-gate-tier-d: PASS"
