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

# Run unit tests
echo -e "${YELLOW}Running Unit Tests...${NC}"
if dotnet test src/Nexo.Tests.GeospatialUnit/Nexo.Tests.GeospatialUnit.csproj \
    --filter "FullyQualifiedName~CachingTests" \
    --logger "console;verbosity=minimal" \
    --logger "trx;LogFileName=ios-unit.trx" \
    --results-directory test-results/caching \
    > test-results/caching/ios-unit.log 2>&1; then
    echo -e "${GREEN}✅ Unit tests passed${NC}"
else
    echo -e "${RED}❌ Unit tests failed${NC}"
    FAILED=1
fi

# Run E2E tests
echo -e "${YELLOW}Running E2E Tests...${NC}"
if dotnet test src/Nexo.Tests.GeospatialE2E/Nexo.Tests.GeospatialE2E.csproj \
    --filter "FullyQualifiedName~CachingSmokeTests" \
    --logger "console;verbosity=minimal" \
    --logger "trx;LogFileName=ios-e2e.trx" \
    --results-directory test-results/caching \
    > test-results/caching/ios-e2e.log 2>&1; then
    echo -e "${GREEN}✅ E2E tests passed${NC}"
else
    echo -e "${RED}❌ E2E tests failed${NC}"
    FAILED=1
fi

# Run infrastructure tests
echo -e "${YELLOW}Running Infrastructure Tests...${NC}"
if dotnet test src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj \
    --filter "FullyQualifiedName~Caching" \
    --logger "console;verbosity=minimal" \
    --logger "trx;LogFileName=ios-infra.trx" \
    --results-directory test-results/caching \
    > test-results/caching/ios-infra.log 2>&1; then
    echo -e "${GREEN}✅ Infrastructure tests passed${NC}"
else
    echo -e "${RED}❌ Infrastructure tests failed${NC}"
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
