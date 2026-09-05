#!/usr/bin/env bash
# DR Tier C: mesh director persistence.
# Docker mesh-lab restarts peer-a when .env.mesh-lab and a working engine exist.
# Otherwise the counted LiteDbMeshDirectorPersistenceTests slice must prove
# reopen plus file backup/wipe/restore. A placeholder copy is not evidence.
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

REPORT_DIR=".ashlar/dr-gate"
mkdir -p "$REPORT_DIR"
FLEET_TESTS="commercial/tests/Ashlar.Commercial.Tests.Fleet/Ashlar.Commercial.Tests.Fleet.csproj"

run_host_litedb_persistence() {
  echo "== DR Tier C: mesh director LiteDB (host, counted) =="
  python3 scripts/run-dotnet-test-counted.py \
    --project "$FLEET_TESTS" \
    --expected-prefix "Ashlar.Commercial.Tests.Fleet." \
    --min-tests 2 \
    -- \
    -f net8.0 \
    --filter "FullyQualifiedName~LiteDbMeshDirectorPersistenceTests" \
    --blame-hang-timeout 60s --blame-hang-dump-type none

  echo '{"ok": true, "component": "mesh-director", "verified": "host-litedb-backup-restore"}' \
    >"$REPORT_DIR/mesh-persistence.json"
  echo ""
  echo "dr-gate-tier-c: PASS"
}

if [ -f ".env.mesh-lab" ] && command -v docker >/dev/null 2>&1 && docker info >/dev/null 2>&1 \
  && [ "${DR_GATE_SKIP_MESH:-0}" != "1" ]; then
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

run_host_litedb_persistence
