#!/bin/bash
# Script to run geospatial caching tests on iOS (requires macOS with Xcode)

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

cd "$PROJECT_ROOT"

echo "🍎 Running Caching Tests on iOS"
echo "================================="
echo ""

# Check if running on macOS
if [[ "$OSTYPE" != "darwin"* ]]; then
    echo "❌ Error: iOS testing requires macOS with Xcode"
    echo "   This script must be run on a Mac"
    exit 1
fi

# Check for .NET
if ! command -v dotnet &> /dev/null; then
    echo "❌ Error: .NET SDK is not installed"
    echo "   Please install .NET 8.0 SDK"
    exit 1
fi

echo "✅ Environment checks passed"
echo ""

# Create test results directory
mkdir -p test-results/caching

# Colors for output
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo -e "${YELLOW}Running caching tests on iOS (macOS)...${NC}"
echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo ""

FAILED=0

# Phase 1: Test base framework dependencies
echo -e "${YELLOW}Phase 1: Testing Base Framework Dependencies...${NC}"
if dotnet test src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj \
    --filter "FullyQualifiedName~BaseFrameworkSmokeTests" \
    --logger "console;verbosity=minimal" \
    --logger "trx;LogFileName=ios-base.trx" \
    --results-directory test-results/caching \
    > test-results/caching/ios-base.log 2>&1; then
    echo -e "${GREEN}✅ Base framework tests passed${NC}"
else
    echo -e "${RED}❌ Base framework tests failed - skipping geo app tests${NC}"
    FAILED=1
    exit $FAILED
fi

# Phase 2: Test geospatial application
echo -e "${YELLOW}Phase 2: Testing Geospatial Application...${NC}"
if dotnet test src/Nexo.Tests.GeospatialE2E/Nexo.Tests.GeospatialE2E.csproj \
    --filter "FullyQualifiedName~GeospatialE2ESmokeTests" \
    --logger "console;verbosity=minimal" \
    --logger "trx;LogFileName=ios-geo.trx" \
    --results-directory test-results/caching \
    > test-results/caching/ios-geo.log 2>&1; then
    echo -e "${GREEN}✅ Geospatial application tests passed${NC}"
else
    echo -e "${RED}❌ Geospatial application tests failed${NC}"
    FAILED=1
fi

echo ""
echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
if [ $FAILED -eq 0 ]; then
    echo -e "${GREEN}✅ All iOS caching tests passed!${NC}"
else
    echo -e "${RED}❌ Some iOS caching tests failed${NC}"
    echo "   Check test-results/caching/ios-*.log for details"
fi
echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo ""

exit $FAILED
