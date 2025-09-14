#!/bin/bash

# Validation test script for Nexo tool generation integration
echo "🧪 Running Nexo Tool Generation Validation Tests"
echo "================================================"

# Build the test project
echo "📦 Building test project..."
dotnet build src/Nexo.Infrastructure.Tests/Nexo.Infrastructure.Tests.csproj

if [ $? -ne 0 ]; then
    echo "❌ Test project build failed"
    exit 1
fi

echo "✅ Test project built successfully"
echo ""

# Run unit tests
echo "🔍 Running unit tests..."
echo "------------------------"
dotnet test src/Nexo.Infrastructure.Tests/Nexo.Infrastructure.Tests.csproj --verbosity normal

if [ $? -ne 0 ]; then
    echo "❌ Unit tests failed"
    exit 1
fi

echo ""
echo "✅ All unit tests passed!"
echo ""

# Run integration tests
echo "🔍 Running integration tests..."
echo "-------------------------------"
dotnet test src/Nexo.Infrastructure.Tests/Nexo.Infrastructure.Tests.csproj --filter "Category=Integration" --verbosity normal

if [ $? -ne 0 ]; then
    echo "❌ Integration tests failed"
    exit 1
fi

echo ""
echo "✅ All integration tests passed!"
echo ""

# Run end-to-end tests
echo "🔍 Running end-to-end tests..."
echo "------------------------------"
dotnet test src/Nexo.Infrastructure.Tests/Nexo.Infrastructure.Tests.csproj --filter "Category=EndToEnd" --verbosity normal

if [ $? -ne 0 ]; then
    echo "❌ End-to-end tests failed"
    exit 1
fi

echo ""
echo "✅ All end-to-end tests passed!"
echo ""

# Test the actual CLI integration
echo "🔍 Testing CLI integration..."
echo "-----------------------------"
echo "Testing tool command help:"
dotnet run --project src/Nexo.CLI/Nexo.CLI.csproj tool --help

if [ $? -ne 0 ]; then
    echo "❌ CLI integration test failed"
    exit 1
fi

echo ""
echo "✅ CLI integration test passed!"
echo ""

# Summary
echo "🎉 Validation Test Summary"
echo "=========================="
echo "✅ Unit Tests: PASSED"
echo "✅ Integration Tests: PASSED" 
echo "✅ End-to-End Tests: PASSED"
echo "✅ CLI Integration: PASSED"
echo ""
echo "🚀 All validation tests completed successfully!"
echo "The Nexo tool generation integration is working correctly."
