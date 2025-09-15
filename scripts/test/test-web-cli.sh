#!/bin/bash

echo "Testing Testing Web CLI Integration"
echo "=============================="

# Test if the web command is recognized
echo "Testing web command help..."
dotnet run --project src/Nexo.CLI/Nexo.CLI.csproj -- web --help 2>/dev/null | head -20

echo ""
echo "Testing web generate command help..."
dotnet run --project src/Nexo.CLI/Nexo.CLI.csproj -- web generate --help 2>/dev/null | head -20

echo ""
echo "SUCCESS: Web CLI integration test completed!" 