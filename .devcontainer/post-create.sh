#!/usr/bin/env bash
# Setup-gate restore graph (same projects as scripts/docker-restore.ps1).
# Full Ashlar.sln restore is skipped here in favor of the same minimal graph as scripts/docker-restore.ps1 (fast dev-container bootstrap).
set -euo pipefail
cd "$(dirname "$0")/.."

# Named volume at ~/.nuget/packages can leave ~/.nuget (or parents) owned by root; dotnet then cannot create ~/.nuget/NuGet.
mkdir -p "${HOME}/.nuget"
if command -v sudo >/dev/null 2>&1 && sudo -n true 2>/dev/null; then
  sudo -n chown -R "$(id -un)":"$(id -gn)" "${HOME}/.nuget"
fi

echo "post-create: restoring NuGet graph..."
dotnet restore src/Ashlar.Core.Application/Ashlar.Core.Application.csproj --verbosity minimal
dotnet restore src/Ashlar.Infrastructure/Ashlar.Infrastructure.csproj --verbosity minimal
dotnet restore application/src/Ashlar.CLI/Ashlar.CLI.csproj --verbosity minimal
dotnet restore src/Ashlar.Tests.Infrastructure/scripts/copy-assemblies.csproj --verbosity minimal
dotnet restore src/Ashlar.Tests.Infrastructure/Ashlar.Tests.Infrastructure.csproj --verbosity minimal
echo "post-create: ok"
