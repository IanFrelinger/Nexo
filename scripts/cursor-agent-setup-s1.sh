#!/usr/bin/env bash
set -euo pipefail

# Cursor background-agent provisioning for Spike S1 harness.
# Installs .NET 8 SDK (when missing) and dotnet-stryker for the mutation dimension.

if ! command -v dotnet >/dev/null 2>&1; then
  wget -q https://dot.net/v1/dotnet-install.sh -O /tmp/dotnet-install.sh
  bash /tmp/dotnet-install.sh --channel 8.0 --install-dir "$HOME/.dotnet"
  export PATH="$HOME/.dotnet:$PATH"
fi

export PATH="$HOME/.dotnet/tools:$PATH"
export DOTNET_ROOT="${DOTNET_ROOT:-$HOME/.dotnet}"

dotnet --version

if ! dotnet stryker --help >/dev/null 2>&1; then
  dotnet tool install -g dotnet-stryker
fi

dotnet stryker --help >/dev/null
