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

# The dotnet devcontainer image ships python3-minimal only; its stripped stdlib lacks
# modules such as `dataclasses`, which scripts/verify-open-commercial-dependency-boundary.py
# (run by scripts/dependency-boundary-gate.sh) imports. Install the full stdlib once.
if ! python3 -c 'import dataclasses' >/dev/null 2>&1; then
  PY_MM="$(python3 -c 'import sys; print("%d.%d" % sys.version_info[:2])')"
  echo "post-create: installing libpython${PY_MM}-stdlib (python3-minimal lacks the full stdlib)..."
  APT_SUDO=""
  if [ "$(id -u)" != "0" ] && command -v sudo >/dev/null 2>&1 && sudo -n true 2>/dev/null; then
    APT_SUDO="sudo -n"
  fi
  ${APT_SUDO} apt-get update -qq
  ${APT_SUDO} apt-get install -y --no-install-recommends "libpython${PY_MM}-stdlib"
  python3 -c 'import dataclasses'
fi

echo "post-create: restoring NuGet graph..."
dotnet restore src/Ashlar.Core.Application/Ashlar.Core.Application.csproj --verbosity minimal
dotnet restore src/Ashlar.Infrastructure/Ashlar.Infrastructure.csproj --verbosity minimal
dotnet restore application/src/Ashlar.CLI/Ashlar.CLI.csproj --verbosity minimal
dotnet restore src/Ashlar.Tests.Infrastructure/scripts/copy-assemblies.csproj --verbosity minimal
dotnet restore src/Ashlar.Tests.Infrastructure/Ashlar.Tests.Infrastructure.csproj --verbosity minimal
echo "post-create: ok"
