#!/usr/bin/env bash
# Security Tier D: supply-chain — vulnerable + deprecated package scan.
# Scans application + key kernel projects (avoids Nexo.Core YamlDotNet registration bug on some NuGet clients).
# Strict mode (SECURITY_GATE_STRICT_SUPPLY_CHAIN=1) fails on any vulnerable package or scan failure.
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

REPORT_DIR=".nexo/security-gate"
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
dotnet restore application/Nexo.Application.sln >/dev/null
dotnet restore src/Nexo.Hosting/Nexo.Hosting.csproj >/dev/null
dotnet restore src/Nexo.Infrastructure/Nexo.Infrastructure.csproj >/dev/null

echo "== Security Tier D: dotnet list package --vulnerable =="
scan_vulnerable application/Nexo.Application.sln
scan_vulnerable src/Nexo.Hosting/Nexo.Hosting.csproj
scan_vulnerable src/Nexo.Infrastructure/Nexo.Infrastructure.csproj

echo "== Security Tier D: dotnet list package --deprecated =="
scan_deprecated application/Nexo.Application.sln
scan_deprecated src/Nexo.Hosting/Nexo.Hosting.csproj
scan_deprecated src/Nexo.Infrastructure/Nexo.Infrastructure.csproj

if [ "$VULN_FOUND" -eq 1 ]; then
  if [ "${SECURITY_GATE_STRICT_SUPPLY_CHAIN:-0}" = "1" ]; then
    echo "Vulnerable packages detected (SECURITY_GATE_STRICT_SUPPLY_CHAIN=1)" >&2
    exit 1
  else
    echo "::warning::Vulnerable packages detected — see $VULN_REPORT (set SECURITY_GATE_STRICT_SUPPLY_CHAIN=1 to fail)"
  fi
fi

if [ "$SCAN_FAIL" -eq 1 ] && [ "${SECURITY_GATE_STRICT_SUPPLY_CHAIN:-0}" = "1" ]; then
  echo "One or more supply-chain scans failed (SECURITY_GATE_STRICT_SUPPLY_CHAIN=1)" >&2
  exit 1
fi

if [ "$SCAN_FAIL" -eq 1 ]; then
  echo "::warning::Some scans failed — see reports in $REPORT_DIR (Nexo.Core/YamlDotNet may need manual audit)"
fi

echo ""
echo "security-gate-tier-d: PASS"
