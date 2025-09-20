#!/bin/bash

# Nexo Feature Lab Console Demo Run Script
# This script runs the console demo after validation

set -e

echo "╔═══════════════════════════════════════════════════════╗"
echo "║            NEXO FEATURE LAB CONSOLE DEMO              ║"
echo "╚═══════════════════════════════════════════════════════╝"
echo

# Check if we're in the right directory
if [ ! -f "Nexo.sln" ]; then
    echo "❌ Error: Please run this script from the Nexo project root directory"
    exit 1
fi

# Run validation first
echo "🔍 Running validation..."
if ! ./demo/scripts/validate-console-demo.sh; then
    echo "❌ Validation failed. Please fix the issues and try again."
    exit 1
fi

echo
echo "🚀 Starting Nexo Feature Lab Console Demo..."
echo

# Run the console application
cd demo/feature-lab/Playground.Console
dotnet run
