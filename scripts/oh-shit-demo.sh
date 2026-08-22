#!/usr/bin/env bash
# "Oh sh*t" demo orchestrator for Ashlar CLI (Linux-first).
# Runs a high-signal, low-friction sequence:
#  1) bootstrap readiness
#  2) interactive chat-style one-shot
#  3) explicit orchestration output (JSON)
#  4) North Star dogfood validation

set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$REPO_ROOT"

QUICK=0
NO_BUILD=0

while [[ $# -gt 0 ]]; do
  case "$1" in
    --quick)
      QUICK=1
      shift
      ;;
    --no-build)
      NO_BUILD=1
      shift
      ;;
    -h|--help)
      cat <<'EOF'
Usage: bash scripts/oh-shit-demo.sh [--quick] [--no-build]

Options:
  --quick      Fast mode: runs dogfood block1 instead of dogfood all.
  --no-build   Skip pre-demo CLI build.
EOF
      exit 0
      ;;
    *)
      echo "Unknown argument: $1" >&2
      exit 2
      ;;
  esac
done

run_with_retry() {
  local attempts="$1"
  shift

  local n=1
  while true; do
    if "$@"; then
      return 0
    fi
    if [[ "$n" -ge "$attempts" ]]; then
      return 1
    fi
    echo "Command failed (attempt $n/$attempts). Retrying..."
    n=$((n + 1))
    sleep 1
  done
}

if [[ ! -f "Ashlar.sln" ]]; then
  echo "Not in Ashlar repository root (Ashlar.sln not found)." >&2
  exit 1
fi

echo "=== OH SH*T DEMO: build ==="
if [[ "$NO_BUILD" -eq 0 ]]; then
  dotnet build application/src/Ashlar.CLI/Ashlar.CLI.csproj -v minimal
else
  echo "Skipping build (--no-build)."
fi

echo
echo "=== OH SH*T DEMO: bootstrap readiness (linux-demo) ==="
dotnet run --project application/src/Ashlar.CLI --no-build -- bootstrap check --json

echo
echo "=== OH SH*T DEMO: chat one-shot (local demo mode) ==="
dotnet run --project application/src/Ashlar.CLI --no-build -- chat --prompt "Give me three high-impact improvements for this repository."

echo
echo "=== OH SH*T DEMO: explicit orchestrate JSON ==="
ASHLAR_ALLOW_MOCK=1 dotnet run --project application/src/Ashlar.CLI --no-build -- orchestrate "Create a practical plan to harden this codebase for release." --provider mock-json --prefer-model deterministic --format-json

echo
if [[ "$QUICK" -eq 1 ]]; then
  echo "=== OH SH*T DEMO: north star gate (quick) ==="
  run_with_retry 2 dotnet run --project application/src/Ashlar.CLI --no-build -- dogfood block1 --format-json
else
  echo "=== OH SH*T DEMO: north star gates (full) ==="
  run_with_retry 2 dotnet run --project application/src/Ashlar.CLI --no-build -- dogfood all --format-json
fi

echo
echo "=== OH SH*T DEMO COMPLETE ==="
