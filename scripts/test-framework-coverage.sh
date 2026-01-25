#!/bin/bash
# Script to run framework test coverage analysis across all core components
# Generates coverage reports and identifies gaps

set -euo pipefail

PROJECT_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$PROJECT_ROOT"

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Coverage threshold (percentage)
COVERAGE_THRESHOLD=${COVERAGE_THRESHOLD:-100}

echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo -e "${YELLOW}Nexo Framework Test Coverage Analysis${NC}"
echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo ""

# Create coverage output directory
COVERAGE_DIR="test-results/coverage"
mkdir -p "$COVERAGE_DIR"

# Framework components to test
FRAMEWORK_COMPONENTS=(
    "Nexo.Core.Domain"
    "Nexo.Core.Application"
    "Nexo.Infrastructure"
    "Nexo.Orchestration"
)

# Test projects
TEST_PROJECTS=(
    "Nexo.Tests.Domain"
    "Nexo.Tests.Application"
    "Nexo.Tests.Infrastructure"
    "Nexo.Tests.Orchestration"
)

FAILED=0
TOTAL_COVERAGE=0
COMPONENT_COUNT=0

# Run coverage for each component
for i in "${!FRAMEWORK_COMPONENTS[@]}"; do
    COMPONENT="${FRAMEWORK_COMPONENTS[$i]}"
    TEST_PROJECT="${TEST_PROJECTS[$i]}"
    
    echo -e "${YELLOW}Testing ${COMPONENT}...${NC}"
    
    # Run tests with coverage
    if dotnet test "src/${TEST_PROJECT}/${TEST_PROJECT}.csproj" \
        --collect:"XPlat Code Coverage" \
        --settings:coverage.runsettings \
        --results-directory "$COVERAGE_DIR" \
        --logger "console;verbosity=minimal" \
        --logger "trx;LogFileName=${COMPONENT}.trx" \
        > "$COVERAGE_DIR/${COMPONENT}-test.log" 2>&1; then
        
        echo -e "${GREEN}✅ ${COMPONENT} tests passed${NC}"
    else
        echo -e "${RED}❌ ${COMPONENT} tests failed${NC}"
        FAILED=1
    fi
done

# Generate combined coverage report
echo ""
echo -e "${YELLOW}Generating coverage report...${NC}"

# Find all coverage files
COVERAGE_FILES=$(find "$COVERAGE_DIR" -name "coverage.cobertura.xml" -o -name "*.cobertura.xml" | tr '\n' ' ')

if [ -n "$COVERAGE_FILES" ]; then
    # Generate HTML report (requires reportgenerator tool)
    if command -v reportgenerator &> /dev/null || dotnet tool list -g | grep -q reportgenerator; then
        dotnet reportgenerator \
            -reports:"$COVERAGE_FILES" \
            -targetdir:"$COVERAGE_DIR/html" \
            -reporttypes:"Html;Badges;JsonSummary" \
            -classfilters:"-*Tests;-*TestBase" \
            > "$COVERAGE_DIR/report.log" 2>&1 || true
        
        echo -e "${GREEN}✅ Coverage report generated at ${COVERAGE_DIR}/html/index.html${NC}"
    else
        echo -e "${YELLOW}⚠️  reportgenerator tool not found. Install with:${NC}"
        echo "   dotnet tool install -g dotnet-reportgenerator-globaltool"
    fi
fi

# Extract coverage percentages from test output
echo ""
echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo -e "${YELLOW}Coverage Summary${NC}"
echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"

for COMPONENT in "${FRAMEWORK_COMPONENTS[@]}"; do
    if [ -f "$COVERAGE_DIR/${COMPONENT}-test.log" ]; then
        # Try to extract coverage from log
        COVERAGE=$(grep -oP 'Line coverage:\s*\K[\d.]+' "$COVERAGE_DIR/${COMPONENT}-test.log" | head -1 || echo "0")
        if [ "$COVERAGE" != "0" ]; then
            echo -e "${GREEN}${COMPONENT}: ${COVERAGE}%${NC}"
            TOTAL_COVERAGE=$(echo "$TOTAL_COVERAGE + $COVERAGE" | bc)
            COMPONENT_COUNT=$((COMPONENT_COUNT + 1))
        fi
    fi
done

if [ $COMPONENT_COUNT -gt 0 ]; then
    AVG_COVERAGE=$(echo "scale=2; $TOTAL_COVERAGE / $COMPONENT_COUNT" | bc)
    echo ""
    echo -e "${BLUE}Average Coverage: ${AVG_COVERAGE}%${NC}"
    echo -e "${BLUE}Target: ${COVERAGE_THRESHOLD}%${NC}"
    
    if (( $(echo "$AVG_COVERAGE < $COVERAGE_THRESHOLD" | bc -l) )); then
        echo -e "${RED}❌ Coverage below threshold!${NC}"
        FAILED=1
    else
        echo -e "${GREEN}✅ Coverage meets threshold!${NC}"
    fi
fi

echo ""
if [ $FAILED -eq 0 ]; then
    echo -e "${GREEN}✅ All framework tests passed${NC}"
    exit 0
else
    echo -e "${RED}❌ Some tests failed or coverage below threshold${NC}"
    exit 1
fi
