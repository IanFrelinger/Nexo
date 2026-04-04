#!/usr/bin/env bash
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$REPO_ROOT"

if [[ $# -lt 1 ]]; then
  cat <<'EOF'
Usage:
  bash scripts/unity-sidecar-demo.sh generate --prompt "add a dash ability"
  bash scripts/unity-sidecar-demo.sh validate
  bash scripts/unity-sidecar-demo.sh run-demo --prompt "add a health pickup system"
  bash scripts/unity-sidecar-demo.sh supervise --game "co-op dungeon crawler" --iterations 2
EOF
  exit 1
fi

dotnet run --project tools/Nexo.UnitySidecarDemo -- "$@"
