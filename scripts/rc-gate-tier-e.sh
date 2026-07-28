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
import sys, datetime, re
from pathlib import Path

path = Path(sys.argv[1])
text = path.read_text(encoding="utf-8")

def parse_with_yaml(src):
    try:
        import yaml  # type: ignore
    except ImportError:
        return None
    return yaml.safe_load(src) or {}

def parse_minimal(src):
    """Tiny YAML subset parser sufficient for docs/exceptions.yaml.

    Supports:
      key: value
      key: []            (empty inline list)
      key:               (block list of mappings)
        - foo: ba
          baz: qux
    """
    lines = [ln.rstrip() for ln in src.splitlines()
             if ln.strip() and not ln.lstrip().startswith("#")]
    root = {}
    i = 0
    while i < len(lines):
        line = lines[i]
        m = re.match(r"^(\S+):\s*(.*)$", line)
        if not m:
            i += 1
            continue
        key, rest = m.group(1), m.group(2).strip()
        if rest == "[]":
            root[key] = []
            i += 1
            continue
        if rest:
            root[key] = rest
            i += 1
            continue
        items = []
        i += 1
        while i < len(lines) and lines[i].startswith(" "):
            block = lines[i]
            if block.lstrip().startswith("- "):
                item = {}
                kv = block.lstrip()[2:]
                if ":" in kv:
                    k, v = kv.split(":", 1)
                    item[k.strip()] = v.strip()
                items.append(item)
                i += 1
                while i < len(lines) and lines[i].startswith(" ") \
                        and not lines[i].lstrip().startswith("- "):
                    inner = lines[i].strip()
                    if ":" in inner:
                        k, v = inner.split(":", 1)
                        item[k.strip()] = v.strip()
                    i += 1
            else:
                i += 1
        root[key] = items
    return root

data = parse_with_yaml(text)
parser_used = "PyYAML"
if data is None:
    data = parse_minimal(text)
    parser_used = "fallback"

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
print(f"exceptions: {len(items)} entries, High/Critical policy OK ({parser_used})")
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
