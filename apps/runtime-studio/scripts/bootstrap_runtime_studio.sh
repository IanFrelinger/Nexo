#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../../.." && pwd)"

bash "${ROOT}/scripts/sandbox/init-agent-sandbox.sh" --project-root "${ROOT}" --profile runtime-studio

mkdir -p "${ROOT}/.nexo/tools/cache/tmp"
mkdir -p "${ROOT}/.nexo/tools/cache/nuget"
mkdir -p "${ROOT}/.nexo/tools/cache/npm"
mkdir -p "${ROOT}/.nexo/agents/workspaces/runtime-studio"

echo
echo "Runtime Studio bootstrap complete."
echo "Next: bash apps/runtime-studio/scripts/run_agent_set_local.sh --duration 5m --disable-observation"
