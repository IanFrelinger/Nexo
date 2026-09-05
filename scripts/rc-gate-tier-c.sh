#!/usr/bin/env bash
# RC Tier C: evidence audit (release bundle, security reports, rollback doc).
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

REPORT_DIR=".ashlar/rc-gate"
mkdir -p "$REPORT_DIR"
AUDIT="$REPORT_DIR/evidence-audit.txt"
: >"$AUDIT"

fail=0

note() { echo "$1" | tee -a "$AUDIT"; }
warn() { echo "::warning::$1" | tee -a "$AUDIT"; }

echo "== RC Tier C: release bundle report =="
BUNDLE_JSON="${RC_GATE_BUNDLE_JSON:-.ashlar/release-bundle/last-run/release-bundle-report.json}"
if [ -f "$BUNDLE_JSON" ]; then
  if grep -q '"Verdict": "PASS"' "$BUNDLE_JSON" || grep -q '"verdict": "PASS"' "$BUNDLE_JSON"; then
    note "release-bundle: PASS ($BUNDLE_JSON)"
  else
    note "release-bundle: FAIL ($BUNDLE_JSON)"
    fail=1
  fi
else
  note "release-bundle: missing ($BUNDLE_JSON)"
  fail=1
fi

echo "== RC Tier C: security supply-chain reports =="
VULN_REPORT="${RC_GATE_VULN_REPORT:-.ashlar/security-gate/vulnerable-packages.txt}"
if [ ! -f "$VULN_REPORT" ]; then
  note "security: no vulnerable-packages report ($VULN_REPORT)"
  fail=1
elif grep -qiE 'Severity:\s*(High|Critical)|critical|high severity' "$VULN_REPORT"; then
  note "security: High/Critical CVEs detected ($VULN_REPORT)"
  fail=1
else
  note "security: no High/Critical in $VULN_REPORT"
fi

echo "== RC Tier C: runtime SLO evidence =="
SLO_CANDIDATES=(
  ".ashlar/runtime/runtime-release-gate-slo.json"
  ".ashlar/runtime/release-gate/last-run/evidence.json"
)
slo_found=0
for slo in "${SLO_CANDIDATES[@]}"; do
  if [ -f "$slo" ]; then
    note "runtime SLO artifact: present ($slo)"
    slo_found=1
  fi
done
if [ "$slo_found" -eq 0 ]; then
  warn "runtime SLO artifact: none of ${SLO_CANDIDATES[*]} present"
fi

echo "== RC Tier C: rollback readiness doc =="
ROLLBACK_CANDIDATES=(
  "docs/production-readiness/ReleaseAndPromotion.md"
  "docs/production-readiness/OperatorDeployment.md"
  "docs/ReleaseCandidateChecklist-v1.md"
)
found=0
for doc in "${ROLLBACK_CANDIDATES[@]}"; do
  if [ -f "$doc" ] && grep -qi rollback "$doc"; then
    note "rollback doc: $doc"
    found=1
  fi
done
if [ "$found" -eq 0 ]; then
  warn "rollback: no rollback section found in standard docs"
fi

if [ "$fail" -ne 0 ]; then
  echo "rc-gate-tier-c: FAIL (see $AUDIT)" >&2
  exit 1
fi

echo ""
echo "rc-gate-tier-c: PASS"
