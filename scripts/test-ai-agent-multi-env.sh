#!/bin/bash
# Test UniversalTesterAgent on all target platforms
# Validates geospatial CLI and API interactions using AI agent

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
cd "$PROJECT_ROOT"

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Test results directory
RESULTS_DIR="test-results/ai-agent"
mkdir -p "$RESULTS_DIR"

echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo -e "${YELLOW}🧪 Running UniversalTesterAgent Tests on All Platforms${NC}"
echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo ""

# Function to run tests in Docker container
run_docker_test() {
    local env_name=$1
    local dockerfile=$2
    local image_tag="nexo-ai-agent-test:${env_name}-8.0"
    
    echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
    echo -e "${YELLOW}Testing on ${env_name}...${NC}"
    echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
    
    # Build Docker image
    echo "Building Docker image..."
    docker build -f "$dockerfile" \
        --build-arg DOTNET_VERSION=8.0 \
        -t "$image_tag" \
        . > "$RESULTS_DIR/${env_name}-build.log" 2>&1
    
    if [ $? -ne 0 ]; then
        echo -e "${RED}❌ Failed to build ${env_name} image${NC}"
        return 1
    fi
    
    # Run tests
    echo "Running AI agent tests..."
    docker run --rm \
        -v "$(pwd)/$RESULTS_DIR:/workspace/test-results" \
        "$image_tag" \
        bash -c "dotnet test src/Nexo.Tests.GeospatialE2E/Nexo.Tests.GeospatialE2E.csproj \
            --filter 'FullyQualifiedName~GeospatialAIAgentTests' \
            --logger 'console;verbosity=minimal'" \
        > "$RESULTS_DIR/${env_name}-test.log" 2>&1
    
    local exit_code=$?
    
    if [ $exit_code -eq 0 ]; then
        echo -e "${GREEN}✅ ${env_name} tests passed${NC}"
        return 0
    else
        echo -e "${RED}❌ ${env_name} tests failed (exit code: $exit_code)${NC}"
        echo "Last 20 lines of test output:"
        tail -20 "$RESULTS_DIR/${env_name}-test.log"
        return 1
    fi
}

# Function to run native tests (macOS)
run_native_test() {
    local env_name=$1
    
    echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
    echo -e "${YELLOW}Testing on ${env_name} (native)...${NC}"
    echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
    
    dotnet test src/Nexo.Tests.GeospatialE2E/Nexo.Tests.GeospatialE2E.csproj \
        --filter 'FullyQualifiedName~GeospatialAIAgentTests' \
        --logger "console;verbosity=minimal" \
        > "$RESULTS_DIR/${env_name}-test.log" 2>&1
    
    local exit_code=$?
    
    if [ $exit_code -eq 0 ]; then
        echo -e "${GREEN}✅ ${env_name} tests passed${NC}"
        return 0
    else
        echo -e "${RED}❌ ${env_name} tests failed (exit code: $exit_code)${NC}"
        echo "Last 20 lines of test output:"
        tail -20 "$RESULTS_DIR/${env_name}-test.log"
        return 1
    fi
}

# Track results
PASSED=0
FAILED=0
TOTAL=0

# Test Linux platforms
for env in ubuntu alpine debian; do
    TOTAL=$((TOTAL + 1))
    if run_docker_test "$env" ".docker/Dockerfile.test-caching-${env}"; then
        PASSED=$((PASSED + 1))
    else
        FAILED=$((FAILED + 1))
    fi
    echo ""
done

# Test Android
TOTAL=$((TOTAL + 1))
if run_docker_test "android" ".docker/Dockerfile.test-caching-android"; then
    PASSED=$((PASSED + 1))
else
    FAILED=$((FAILED + 1))
fi
echo ""

# Test iOS (native on macOS)
if [[ "$OSTYPE" == "darwin"* ]]; then
    TOTAL=$((TOTAL + 1))
    if run_native_test "ios"; then
        PASSED=$((PASSED + 1))
    else
        FAILED=$((FAILED + 1))
    fi
    echo ""
fi

# Test Unity (native on macOS)
if [[ "$OSTYPE" == "darwin"* ]]; then
    TOTAL=$((TOTAL + 1))
    if run_native_test "unity"; then
        PASSED=$((PASSED + 1))
    else
        FAILED=$((FAILED + 1))
    fi
    echo ""
fi

# Test macOS (native)
if [[ "$OSTYPE" == "darwin"* ]]; then
    TOTAL=$((TOTAL + 1))
    if run_native_test "macos"; then
        PASSED=$((PASSED + 1))
    else
        FAILED=$((FAILED + 1))
    fi
    echo ""
fi

# Summary
echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo -e "${YELLOW}📊 Test Summary${NC}"
echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo -e "Total Platforms: ${TOTAL}"
echo -e "${GREEN}Passed: ${PASSED}${NC}"
echo -e "${RED}Failed: ${FAILED}${NC}"
echo ""
echo -e "Test logs available in: ${RESULTS_DIR}/"

if [ $FAILED -eq 0 ]; then
    echo -e "${GREEN}✅ All AI agent tests passed!${NC}"
    exit 0
else
    echo -e "${RED}❌ Some tests failed${NC}"
    exit 1
fi
