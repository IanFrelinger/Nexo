#!/bin/bash

# Nexo Feature Lab Console Demo Validation Script
# This script validates the environment and builds the console demo

set -e

echo "╔═══════════════════════════════════════════════════════╗"
echo "║           NEXO FEATURE LAB CONSOLE VALIDATION         ║"
echo "╚═══════════════════════════════════════════════════════╝"
echo

# Check if we're in the right directory
if [ ! -f "Nexo.sln" ]; then
    echo "❌ Error: Please run this script from the Nexo project root directory"
    exit 1
fi

echo "🔍 Checking environment..."

# Check .NET 8
echo -n "Checking .NET 8... "
if command -v dotnet >/dev/null 2>&1; then
    DOTNET_VERSION=$(dotnet --version)
    if [[ $DOTNET_VERSION == 8.* ]]; then
        echo "✅ Found $DOTNET_VERSION"
    else
        echo "❌ Found $DOTNET_VERSION, but need .NET 8"
        exit 1
    fi
else
    echo "❌ .NET not found"
    exit 1
fi

# Check Docker (optional)
echo -n "Checking Docker... "
if command -v docker >/dev/null 2>&1; then
    echo "✅ Found $(docker --version)"
else
    echo "⚠️  Docker not found (optional for console demo)"
fi

# Build the console application
echo -n "Building console application... "
if dotnet build demo/feature-lab/Playground.Console/Playground.Console.csproj -c Release --verbosity quiet; then
    echo "✅ Build successful"
else
    echo "❌ Build failed"
    exit 1
fi

# Check if console app exists
echo -n "Checking console executable... "
if [ -f "demo/feature-lab/Playground.Console/bin/Release/net8.0/Playground.Console" ]; then
    echo "✅ Executable found"
else
    echo "❌ Executable not found"
    exit 1
fi

# Run basic tests
echo -n "Running basic tests... "
if dotnet test tests/FeatureLab.Demo.Tests/FeatureLab.Demo.Tests.csproj --verbosity quiet; then
    echo "✅ Tests passed"
else
    echo "❌ Tests failed"
    exit 1
fi

echo
echo "╔═══════════════════════════════════════════════════════╗"
echo "║              VALIDATION COMPLETE                      ║"
echo "╚═══════════════════════════════════════════════════════╝"
echo
echo "✅ Console demo is ready to run!"
echo
echo "To run the console demo:"
echo "  cd demo/feature-lab/Playground.Console"
echo "  dotnet run"
echo
echo "Or use the run script:"
echo "  ./demo/scripts/run-console-demo.sh"