#!/usr/bin/env bash
# Setup-gate restore graph (same projects as scripts/docker-restore.ps1).
# Full Nexo.sln restore is skipped here: MAUI/Android workloads are not installed in this image.
set -euo pipefail
cd "$(dirname "$0")/.."

# Named volume at ~/.nuget/packages can leave ~/.nuget (or parents) owned by root; dotnet then cannot create ~/.nuget/NuGet.
mkdir -p "${HOME}/.nuget"
if command -v sudo >/dev/null 2>&1 && sudo -n true 2>/dev/null; then
  sudo -n chown -R "$(id -un)":"$(id -gn)" "${HOME}/.nuget"
fi

echo "post-create: restoring NuGet graph..."
dotnet restore src/Nexo.Core.Application/Nexo.Core.Application.csproj --verbosity minimal
dotnet restore src/Nexo.Infrastructure/Nexo.Infrastructure.csproj --verbosity minimal
dotnet restore application/src/Nexo.CLI/Nexo.CLI.csproj --verbosity minimal
dotnet restore src/Nexo.Tests.Infrastructure/scripts/copy-assemblies.csproj --verbosity minimal
dotnet restore src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj --verbosity minimal
echo "post-create: ok"
