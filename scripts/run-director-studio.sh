#!/bin/bash

# Director Studio (Avalonia) Runner Script
# This script builds and runs the Director Studio Avalonia application

set -e

echo "🎬 Director Studio (Avalonia) - Nexo Companion"
echo "=============================================="

# Check if .NET 8 is installed
if ! command -v dotnet &> /dev/null; then
    echo "❌ .NET 8 SDK is required but not installed."
    echo "Please install .NET 8 SDK from https://dotnet.microsoft.com/download"
    exit 1
fi

# Check .NET version
DOTNET_VERSION=$(dotnet --version | cut -d. -f1)
if [ "$DOTNET_VERSION" -lt 8 ]; then
    echo "❌ .NET 8 or later is required. Current version: $(dotnet --version)"
    exit 1
fi

echo "✅ .NET $(dotnet --version) detected"

# Navigate to project directory
cd "$(dirname "$0")/../tools/Director.Avalonia"

echo "🔨 Building Director Studio..."

# Restore dependencies
dotnet restore

# Build the project
dotnet build --configuration Release

echo "✅ Build completed successfully"

# Check if Unity is running (optional)
if pgrep -f "Unity" > /dev/null; then
    echo "🎮 Unity Editor detected - IPC server should be running"
else
    echo "⚠️  Unity Editor not detected - start Unity Editor first for full functionality"
fi

echo "🚀 Starting Director Studio..."
echo "   - Connect to Unity Editor using the token"
echo "   - Use the UI to run Nexo commands"
echo "   - Monitor logs and validation results"
echo ""

# Run the application
dotnet run --configuration Release
