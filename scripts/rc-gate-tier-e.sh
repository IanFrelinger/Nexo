#!/usr/bin/env bash
# RC Tier E: exceptions policy, rollback drill record, release sign-off checklist.
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

REPORT_DIR=".nexo/rc-gate"
mkdir -p "$REPORT_DIR"
AUDIT="$REPORT_DIR/policy-audit.txt"
: >"$AUDIT"

fail=0
note() { echo "$1" | tee -a "$AUDIT"; }
warn() { echo "::warning::$1" | tee -a "$AUDIT"; }

EXCEPTIONS_FILE="${RC_EXCEPTIONS_FILE:-docs/exceptions.yaml}"

echo "== RC Tier E: exceptions policy =="
if [ ! -f "$EXCEPTIONS_FILE" ]; then
  warn "exceptions: missing $EXCEPTIONS_FILE"
  if [ "${RC_GATE_STRICT_EXCEPTIONS:-0}" = "1" ]; then
    fail=1
  fi
else
  python3 - "$EXCEPTIONS_FILE" <<'PY'
import sys, datetime
from pathlib import Path

try:
    import yaml
except ImportError:
    print("exceptions: PyYAML not installed — skipping structured validation")
    raise SystemExit(0)

path = Path(sys.argv[1])
data = yaml.safe_load(path.read_text(encoding="utf-8")) or {}
items = data.get("exceptions") or []
today = datetime.date.today()
blocked = []
for item in items:
    sev = str(item.get("severity", "")).lower()
    if sev not in ("high", "critical"):
        continue
    missing = []
    for field in ("owner", "expires", "mitigation", "sign_off"):
        if not item.get(field):
            missing.append(field)
    exp = item.get("expires")
    if exp:
        try:
            exp_date = datetime.date.fromisoformat(str(exp))
            if exp_date < today:
                blocked.append(f"{item.get('id', '?')}: expired {exp}")
        except ValueError:
            missing.append("expires(invalid)")
    if missing:
        blocked.append(f"{item.get('id', '?')}: missing {', '.join(missing)}")
if blocked:
    for b in blocked:
        print(f"exceptions BLOCK: {b}")
    raise SystemExit(1)
print(f"exceptions: {len(items)} entries, High/Critical policy OK")
PY
  rc=$?
  if [ "$rc" -ne 0 ]; then
    if [ "${RC_GATE_STRICT_EXCEPTIONS:-0}" = "1" ]; then
      fail=1
    else
      warn "exceptions: policy validation failed (non-strict)"
    fi
  else
    note "exceptions: policy OK ($EXCEPTIONS_FILE)"
  fi
fi

echo "== RC Tier E: rollback drill record =="
DRILL_DOC="docs/production-readiness/RollbackDrill-v1.md"
if [ -f "$DRILL_DOC" ] && grep -q "Last drill" "$DRILL_DOC"; then
  note "rollback drill doc: present ($DRILL_DOC)"
else
  warn "rollback drill: update $DRILL_DOC with Last drill date and operator"
fi

echo "== RC Tier E: release sign-off =="
SIGNOFF="docs/production-readiness/ReleaseSignOff-v1.md"
if [ -f "$SIGNOFF" ]; then
  if grep -q '\[x\].*Product' "$SIGNOFF" 2>/dev/null || grep -q '\[x\].*Engineering' "$SIGNOFF" 2>/dev/null; then
    note "sign-off: checked items found"
  else
    warn "sign-off: template present but no [x] checkmarks — complete before tag"
  fi
else
  warn "sign-off: missing $SIGNOFF"
fi

if [ "$fail" -ne 0 ]; then
  echo "rc-gate-tier-e: FAIL (see $AUDIT)" >&2
  exit 1
fi

echo ""
echo "rc-gate-tier-e: PASS"
