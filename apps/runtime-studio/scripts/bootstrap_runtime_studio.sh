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
echo
echo "Next steps:"
echo "  Optimize for your hardware (scaffold spec → benchmark → recommend → optional daemon):"
echo "    bash apps/runtime-studio/scripts/optimize_agent_cluster.sh --objective 'your task' --verbose"
echo
echo "  Or skip optimization and run the agent set directly:"
echo "    bash apps/runtime-studio/scripts/run_agent_set_local.sh --duration 5m --disable-observation"
