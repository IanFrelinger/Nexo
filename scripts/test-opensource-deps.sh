#!/usr/bin/env bash
# Offline-friendly regression for the opensource deps lock/pull contract.
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
LOCK="$ROOT/opensource.deps.lock"
fail=0

[ -f "$LOCK" ] || { echo "FAIL: missing opensource.deps.lock"; exit 1; }
# shellcheck disable=SC1090
set -a; . "$LOCK"; set +a
for key in OLLAMA_IMAGE DOCKER_CLI_IMAGE UBUNTU_IMAGE OLLAMA_MODEL; do
  val="${!key:-}"
  val="${val//$'\r'/}"
  if [ -z "$val" ]; then
    echo "FAIL: $key empty"
    fail=1
  else
    echo "OK: $key=$val"
  fi
done

out="$(bash "$ROOT/scripts/pull-opensource-deps.sh" --dry-run --no-model)"
echo "$out" | grep -q 'open-source deps' || { echo "FAIL: dry-run header"; fail=1; }
echo "$out" | grep -Eq 'present:|would-pull:' || { echo "FAIL: dry-run body"; fail=1; }
echo "$out" | grep -q 'PASS: open-source deps ready' || { echo "FAIL: dry-run pass line"; fail=1; }

# Must not claim bespoke agent/tool images.
if grep -qiE 'nexo-dep-extract|DEPLOY_AGENT' "$LOCK"; then
  echo "FAIL: opensource lock must not include bespoke agent/tool refs"
  fail=1
fi

[ "$fail" = 0 ] && echo "PASS: opensource deps contract" || exit 1
