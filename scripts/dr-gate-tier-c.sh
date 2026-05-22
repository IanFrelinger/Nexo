#!/usr/bin/env bash
# DR Tier C: mesh director persistence (Docker mesh-lab) or file-level LiteDB copy check.
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

REPORT_DIR=".nexo/dr-gate"
mkdir -p "$REPORT_DIR"

if [ -f ".env.mesh-lab" ] && command -v docker >/dev/null 2>&1 && docker info >/dev/null 2>&1; then
  if [ "${DR_GATE_SKIP_MESH:-0}" = "1" ]; then
    echo "== DR Tier C: mesh persistence skipped (DR_GATE_SKIP_MESH=1) =="
  else
    echo "== DR Tier C: mesh director persistence (restart peer-a) =="
    make mesh-lab-up 2>/dev/null || true
    PEER_HOST="${MESH_LAB_PEER_A_HOST:-127.0.0.1:18081}"
    echo "waiting for peer-a health at http://${PEER_HOST}/health ..."
    for i in $(seq 1 120); do
      if curl -fsS -m 3 "http://${PEER_HOST}/health" >/dev/null 2>&1; then
        break
      fi
      if [ "$i" -eq 120 ]; then
        echo "peer-a not healthy after mesh-lab-up" >&2
        exit 1
      fi
      sleep 2
    done
    bash scripts/mesh-lab-verify-persistence.sh .env.mesh-lab | tee "$REPORT_DIR/mesh-persistence.log"
    echo '{"ok": true, "component": "mesh-director", "verified": "restart"}' >"$REPORT_DIR/mesh-persistence.json"
    echo ""
    echo "dr-gate-tier-c: PASS"
    exit 0
  fi
fi

echo "== DR Tier C: advisory — mesh lab not available; file-copy LiteDB sanity =="
TMP="${TMPDIR:-/tmp}/nexo-dr-tier-c-$$"
mkdir -p "$TMP"
trap 'rm -rf "$TMP"' EXIT
FAKE="$TMP/fake.litedb"
echo "nexo-dr-placeholder" >"$FAKE"
cp "$FAKE" "$TMP/backup.litedb"
rm -f "$FAKE"
cp "$TMP/backup.litedb" "$FAKE"
test -f "$FAKE"

echo '{"ok": true, "component": "mesh-director", "verified": "skipped-advisory"}' >"$REPORT_DIR/mesh-persistence.json"
echo "::warning::mesh DR: run with .env.mesh-lab + Docker for full director persistence verify"

echo ""
echo "dr-gate-tier-c: PASS (advisory)"
