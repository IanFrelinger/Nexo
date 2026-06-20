#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

if ! command -v dotnet >/dev/null 2>&1; then
  wget -q https://dot.net/v1/dotnet-install.sh -O /tmp/dotnet-install.sh
  bash /tmp/dotnet-install.sh --channel 8.0 --install-dir "$HOME/.dotnet"
  export PATH="$HOME/.dotnet:$PATH"
fi

export PATH="$HOME/.dotnet/tools:$PATH"
export DOTNET_ROOT="${DOTNET_ROOT:-$HOME/.dotnet}"
unset NEXO_S3_GENERATOR
unset ANTHROPIC_API_KEY
unset OPENAI_API_KEY
unset NEXO_LLM_API_KEY

dotnet --version

if ! dotnet stryker --help >/dev/null 2>&1; then
  dotnet tool install -g dotnet-stryker
fi

dotnet build src/Nexo.Spike.S3/Nexo.Spike.S3.csproj -c Release
dotnet build src/Nexo.Tests.Spike.S3/Nexo.Tests.Spike.S3.csproj -c Release
dotnet test src/Nexo.Tests.Spike.S3/Nexo.Tests.Spike.S3.csproj -c Release --no-build

dotnet run --project src/Nexo.Spike.S3 --no-build -c Release -- \
  --out artifacts/s3 \
  --reset-registry \
  --density-seeds 2 \
  --escape-seeds 2
