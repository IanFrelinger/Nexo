#!/usr/bin/env bash
set -euo pipefail

# Cursor background-agent provisioning for Spike S2 adaptive harness.
# Installs .NET 8 SDK (when missing), dotnet-stryker, builds S2, runs mock harness headless.

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

if ! command -v dotnet >/dev/null 2>&1; then
  wget -q https://dot.net/v1/dotnet-install.sh -O /tmp/dotnet-install.sh
  bash /tmp/dotnet-install.sh --channel 8.0 --install-dir "$HOME/.dotnet"
  export PATH="$HOME/.dotnet:$PATH"
fi

export PATH="$HOME/.dotnet/tools:$PATH"
export DOTNET_ROOT="${DOTNET_ROOT:-$HOME/.dotnet}"
unset NEXO_S2_ADVERSARY
unset OPENAI_API_KEY
unset ANTHROPIC_API_KEY
unset NEXO_LLM_API_KEY

dotnet --version

if ! dotnet stryker --help >/dev/null 2>&1; then
  dotnet tool install -g dotnet-stryker
fi

dotnet build src/Nexo.Spike.S2/Nexo.Spike.S2.csproj -c Release
dotnet build src/Nexo.Tests.Spike.S2/Nexo.Tests.Spike.S2.csproj -c Release
dotnet test src/Nexo.Tests.Spike.S2/Nexo.Tests.Spike.S2.csproj -c Release --no-build

dotnet run --project src/Nexo.Spike.S2 --no-build -c Release -- \
  --intents 1 \
  --attempts 3 \
  --out artifacts/s2
