#!/usr/bin/env bash
# Clean-host style deploy rehearsal. Run from a shipped bundle or repo root.
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
EVIDENCE="${EVIDENCE_DIR:-$ROOT/.nexo/evidence}"
mkdir -p "$EVIDENCE"
export COMPOSE_PROJECT_NAME="${COMPOSE_PROJECT_NAME:-nexo-rehearsal}"
export NEXO_USE_DEPLOY=1
# Keep shared model volume across rehearsals (do not wipe nexo-ollama-models).
export OLLAMA_VOLUME_NAME="${OLLAMA_VOLUME_NAME:-nexo-ollama-models}"

record() { printf '%s %s\n' "$(date -u +%FT%TZ)" "$*" | tee -a "$EVIDENCE/rehearsal.log"; }
record "preflight"
python3 "$ROOT/scripts/deployment-preflight.py" --workspace "$ROOT" --output "$EVIDENCE/preflight.json"
record "cold install"
bash "$ROOT/scripts/run.sh" "$@" up
record "health / pack check"
bash "$ROOT/scripts/run.sh" pack-check
record "restart recovery"
bash "$ROOT/scripts/run.sh" down
bash "$ROOT/scripts/run.sh" "$@" up
bash "$ROOT/scripts/run.sh" pack-check
record "teardown"
bash "$ROOT/scripts/run.sh" down
python3 "$ROOT/scripts/deployment-status.py" --preflight "$EVIDENCE/preflight.json" --out "$EVIDENCE/deployment_evidence.jsonl"
record "PASS"
