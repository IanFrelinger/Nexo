#!/usr/bin/env bash
# Run Stryker mutation testing on kernel components.
# Install first: dotnet tool install -g dotnet-stryker
# Open ./mutation-reports/*/reports/mutation-report.html to review surviving mutants.
set -e
cd "$(dirname "$0")/.."
mkdir -p mutation-reports

# Use Nexo.Kernel.sln to avoid building Nexo.Client.Mobile/Nexo.Lite (Android SDK required)
SOLUTION="Nexo.Kernel.sln"

echo "=== Mutation testing: Nexo.Policies.Dev (PathAllowlist, MaxWriteSize) ==="
dotnet stryker --solution "$SOLUTION" --project src/Nexo.Policies.Dev/Nexo.Policies.Dev.csproj --test-project src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj --output mutation-reports/policies

echo ""
echo "=== Mutation testing: Nexo.Runtime (AgentHost, PolicyEngine) ==="
dotnet stryker --solution "$SOLUTION" --project src/Nexo.Runtime/Nexo.Runtime.csproj --test-project src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj --output mutation-reports/runtime

echo ""
echo "=== Mutation testing: Nexo.Infrastructure (Rollback, ImmutableCoreRegistry) ==="
dotnet stryker --solution "$SOLUTION" --project src/Nexo.Infrastructure/Nexo.Infrastructure.csproj --test-project src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj --mutate "**/Rollback/**/*.cs" "**/Adaptation/ImmutableCoreRegistry.cs" --output mutation-reports/infrastructure

echo ""
echo "Open ./mutation-reports/*/reports/mutation-report.html to review surviving mutants."
