#!/bin/bash

# Script to run the C# demo runner

set -euo pipefail

# Colors for output
readonly GREEN='\033[0;32m'
readonly RED='\033[0;31m'
readonly YELLOW='\033[1;33m'
readonly BLUE='\033[0;34m'
readonly PURPLE='\033[0;35m'
readonly NC='\033[0m' # No Color

# Get the directory of this script
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
DEMO_SCRIPTS_PATH="$PROJECT_ROOT/demo/scripts"

echo -e "${PURPLE}🚀 NEXO FEATURE LAB C# DEMO RUNNER${NC}"
echo "======================================"
echo

# Build the demo scripts
echo -e "${BLUE}🔨 Building Demo Scripts...${NC}"
if ! dotnet build "$DEMO_SCRIPTS_PATH" -c Release --verbosity quiet; then
    echo -e "${RED}❌ Failed to build demo scripts${NC}"
    exit 1
fi
echo -e "${GREEN}✅ Demo scripts built successfully${NC}"
echo

# Run the demo
echo -e "${BLUE}🎮 Running C# Demo Runner...${NC}"
echo

if [ $# -eq 0 ]; then
    # Run full showcase
    dotnet run --project "$DEMO_SCRIPTS_PATH" --no-build
else
    # Run specific demo
    dotnet run --project "$DEMO_SCRIPTS_PATH" --no-build -- "$@"
fi

echo
echo -e "${GREEN}✅ Demo completed successfully!${NC}"