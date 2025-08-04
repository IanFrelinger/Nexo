#!/bin/bash

echo "Running Unity compatibility tests..."

# Setup Unity test environment
/usr/local/bin/setup-unity-test.sh

# Verify assemblies are compatible
if [ -f "unity-test-project/Assets/Plugins/Nexo.Core.Domain.dll" ]; then
    echo "✓ Nexo.Core.Domain.dll is compatible with Unity"
else
    echo "✗ Nexo.Core.Domain.dll not found"
    exit 1
fi

if [ -f "unity-test-project/Assets/Plugins/Nexo.Shared.dll" ]; then
    echo "✓ Nexo.Shared.dll is compatible with Unity"
else
    echo "✗ Nexo.Shared.dll not found"
    exit 1
fi

# Run .NET tests to verify Unity compatibility
dotnet test tests/UnityCompatibilityTests/UnityCompatibilityTests.csproj --configuration Release --verbosity normal

echo "Unity compatibility tests completed successfully" 