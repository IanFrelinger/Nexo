#!/usr/bin/env bash
# Run Stryker mutation testing on kernel components.
# Install first: dotnet tool install -g dotnet-stryker
set -e
cd "$(dirname "$0")/.."

echo "=== Mutation testing: Nexo.Policies.Dev (PathAllowlist, MaxWriteSize) ==="
dotnet stryker --project src/Nexo.Policies.Dev/Nexo.Policies.Dev.csproj --test-project src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj

echo ""
echo "=== Mutation testing: Nexo.Runtime (AgentHost, PolicyEngine) ==="
dotnet stryker --project src/Nexo.Runtime/Nexo.Runtime.csproj --test-project src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj

echo ""
echo "=== Mutation testing: Nexo.Infrastructure (Rollback, ImmutableCoreRegistry) ==="
dotnet stryker --project src/Nexo.Infrastructure/Nexo.Infrastructure.csproj --test-project src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj --mutate "**/Rollback/**/*.cs" "**/Adaptation/ImmutableCoreRegistry.cs"
