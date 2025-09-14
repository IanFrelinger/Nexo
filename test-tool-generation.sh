#!/bin/bash

# Test script for Nexo tool generation integration
echo "🧪 Testing Nexo Tool Generation Integration"
echo "=========================================="

# Build the project
echo "📦 Building project..."
dotnet build src/Nexo.CLI/Nexo.CLI.csproj

if [ $? -ne 0 ]; then
    echo "❌ Build failed"
    exit 1
fi

echo "✅ Build successful"
echo ""

# Test 1: Show help for tool commands
echo "🔍 Test 1: Tool command help"
echo "----------------------------"
dotnet run --project src/Nexo.CLI/Nexo.CLI.csproj tool --help

echo ""
echo "🔍 Test 2: Tool list command"
echo "---------------------------"
dotnet run --project src/Nexo.CLI/Nexo.CLI.csproj tool list

echo ""
echo "🔍 Test 3: Tool chat command (interactive)"
echo "------------------------------------------"
echo "This will start interactive mode. Type 'exit' to quit."
echo "Try: 'Create a JSON formatter'"
echo ""

# Note: This will start interactive mode
# dotnet run --project src/Nexo.CLI/Nexo.CLI.csproj tool chat

echo "🎉 Integration test completed!"
echo ""
echo "To test the full functionality:"
echo "1. Run: dotnet run --project src/Nexo.CLI/Nexo.CLI.csproj tool chat"
echo "2. Try: 'Create a JSON formatter'"
echo "3. Try: 'Create an API tester'"
echo "4. Try: 'modify json-formatter to also validate JSON'"
echo "5. Type 'exit' to quit"
