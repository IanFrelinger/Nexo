#!/usr/bin/env bash
# Setup-gate restore graph (same projects as scripts/docker-restore.ps1).
# Full Nexo.sln restore is skipped here: MAUI/Android workloads are not installed in this image.
set -euo pipefail
cd "$(dirname "$0")/.."

echo "post-create: restoring NuGet graph..."
dotnet restore src/Nexo.Core.Application/Nexo.Core.Application.csproj --verbosity minimal
dotnet restore src/Nexo.Infrastructure/Nexo.Infrastructure.csproj --verbosity minimal
dotnet restore src/Nexo.CLI/Nexo.CLI.csproj --verbosity minimal
dotnet restore src/Nexo.Tests.Infrastructure/scripts/copy-assemblies.csproj --verbosity minimal
dotnet restore src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj --verbosity minimal
echo "post-create: ok"
