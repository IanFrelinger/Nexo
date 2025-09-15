#!/bin/bash

# Validation test script for Nexo tool generation integration
echo "Running Nexo Tool Generation Validation Tests"
echo "================================================"

# Build the test project
echo "Building test project..."
dotnet build src/Nexo.Infrastructure.Tests/Nexo.Infrastructure.Tests.csproj

if [ $? -ne 0 ]; then
    echo "ERROR: Test project build failed"
    exit 1
fi

echo "SUCCESS: Test project built successfully"
echo ""

# Run unit tests
echo "Running unit tests..."
echo "------------------------"
dotnet test src/Nexo.Infrastructure.Tests/Nexo.Infrastructure.Tests.csproj --verbosity normal

if [ $? -ne 0 ]; then
    echo "ERROR: Unit tests failed"
    exit 1
fi

echo ""
echo "SUCCESS: All unit tests passed!"
echo ""

# Run integration tests
echo "Running integration tests..."
echo "-------------------------------"
dotnet test src/Nexo.Infrastructure.Tests/Nexo.Infrastructure.Tests.csproj --filter "Category=Integration" --verbosity normal

if [ $? -ne 0 ]; then
    echo "ERROR: Integration tests failed"
    exit 1
fi

echo ""
echo "SUCCESS: All integration tests passed!"
echo ""

# Run end-to-end tests
echo "Running Running end-to-end tests..."
echo "------------------------------"
dotnet test src/Nexo.Infrastructure.Tests/Nexo.Infrastructure.Tests.csproj --filter "Category=EndToEnd" --verbosity normal

if [ $? -ne 0 ]; then
    echo "ERROR: End-to-end tests failed"
    exit 1
fi

echo ""
echo "SUCCESS: All end-to-end tests passed!"
echo ""

# Test the actual CLI integration
echo "Running Testing CLI integration..."
echo "-----------------------------"
echo "Testing tool command help:"
dotnet run --project src/Nexo.CLI/Nexo.CLI.csproj tool --help

if [ $? -ne 0 ]; then
    echo "ERROR: CLI integration test failed"
    exit 1
fi

echo ""
echo "SUCCESS: CLI integration test passed!"
echo ""

# Summary
echo "SUMMARY Validation Test Summary"
echo "=========================="
echo "SUCCESS: Unit Tests: PASSED"
echo "SUCCESS: Integration Tests: PASSED" 
echo "SUCCESS: End-to-End Tests: PASSED"
echo "SUCCESS: CLI Integration: PASSED"
echo ""
echo "All All validation tests completed successfully!"
echo "The Nexo tool generation integration is working correctly."
