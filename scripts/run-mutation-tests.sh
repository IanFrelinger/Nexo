#!/usr/bin/env bash
# Run Stryker mutation testing on kernel components.
# Install first: dotnet tool install -g dotnet-stryke
# Open ./mutation-reports/*/reports/mutation-report.html to review surviving mutants.
set -e
cd "$(dirname "$0")/.."
mkdir -p mutation-reports

# Use Ashlar.Kernel.sln for a focused kernel graph (Stryker / mutation tooling).
SOLUTION="Ashlar.Kernel.sln"

echo "=== Mutation testing: Ashlar.Policies.Dev (PathAllowlist, MaxWriteSize) ==="
dotnet stryker --solution "$SOLUTION" --project src/Ashlar.Policies.Dev/Ashlar.Policies.Dev.csproj --test-project src/Ashlar.Tests.Infrastructure/Ashlar.Tests.Infrastructure.csproj --output mutation-reports/policies

echo ""
echo "=== Mutation testing: Ashlar.Runtime (AgentHost, PolicyEngine) ==="
dotnet stryker --solution "$SOLUTION" --project src/Ashlar.Runtime/Ashlar.Runtime.csproj --test-project src/Ashlar.Tests.Infrastructure/Ashlar.Tests.Infrastructure.csproj --output mutation-reports/runtime

echo ""
echo "=== Mutation testing: Ashlar.Infrastructure (Rollback, ImmutableCoreRegistry) ==="
dotnet stryker --solution "$SOLUTION" --project src/Ashlar.Infrastructure/Ashlar.Infrastructure.csproj --test-project src/Ashlar.Tests.Infrastructure/Ashlar.Tests.Infrastructure.csproj --mutate "**/Rollback/**/*.cs" "**/Adaptation/ImmutableCoreRegistry.cs" --output mutation-reports/infrastructure

echo ""
echo "Open ./mutation-reports/*/reports/mutation-report.html to review surviving mutants."
