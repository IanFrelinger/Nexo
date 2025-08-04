#!/bin/bash

# Multi-Platform Test Orchestration Script
# Tests Nexo Framework across different environments

set -e

echo "🚀 Nexo Framework Multi-Platform Testing"
echo "========================================"
echo ""

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Test results tracking
TOTAL_TESTS=0
PASSED_TESTS=0
FAILED_TESTS=0

# Create results directory
mkdir -p ../test-results

# Function to run tests for a platform
run_platform_test() {
    local platform=$1
    local dockerfile=$2
    local container_name=$3
    
    echo -e "${YELLOW}Testing on ${platform}...${NC}"
    
    # Build the test container
    echo -e "${BLUE}Building ${platform} test container...${NC}"
    if docker build -f "$dockerfile" -t "nexo-test-${platform}" .; then
        echo -e "  ${GREEN}✅ Container built successfully${NC}"
    else
        echo -e "  ${RED}❌ Container build failed${NC}"
        ((FAILED_TESTS++))
        return 1
    fi
    
    # Run the tests
    echo -e "${BLUE}Running tests on ${platform}...${NC}"
    if docker run --rm \
        -v "$(pwd)/../test-results:/test/results" \
        -e NEXO_LOG_LEVEL=Information \
        -e DOTNET_RUNNING_IN_CONTAINER=true \
        "nexo-test-${platform}" > "../test-results/${platform}-test.log" 2>&1; then
        
        # Check if tests passed
        if grep -q "Deployment Status: ✅ SUCCESS" "../test-results/${platform}-test.log"; then
            echo -e "  ${GREEN}✅ ${platform} tests PASSED${NC}"
            ((PASSED_TESTS++))
        else
            echo -e "  ${RED}❌ ${platform} tests FAILED${NC}"
            echo "    Check logs: ../test-results/${platform}-test.log"
            ((FAILED_TESTS++))
        fi
    else
        echo -e "  ${RED}❌ ${platform} container execution failed${NC}"
        ((FAILED_TESTS++))
    fi
    
    ((TOTAL_TESTS++))
    echo ""
}

# Function to generate test report
generate_report() {
    echo -e "${YELLOW}Multi-Platform Test Summary${NC}"
    echo "================================"
    echo -e "Total Platforms Tested: ${TOTAL_TESTS}"
    echo -e "Platforms Passed: ${GREEN}${PASSED_TESTS}${NC}"
    echo -e "Platforms Failed: ${RED}${FAILED_TESTS}${NC}"
    echo ""
    
    if [ $FAILED_TESTS -eq 0 ]; then
        echo -e "${GREEN}🎉 All platforms passed! Nexo framework is cross-platform ready.${NC}"
        echo ""
        echo -e "${GREEN}Multi-Platform Status: ✅ SUCCESS${NC}"
        return 0
    else
        echo -e "${RED}❌ Some platforms failed. Check individual test logs.${NC}"
        echo ""
        echo -e "${RED}Multi-Platform Status: ❌ FAILED${NC}"
        return 1
    fi
}

# Function to clean up
cleanup() {
    echo -e "${BLUE}Cleaning up test containers...${NC}"
    docker rmi nexo-test-ubuntu nexo-test-macos nexo-test-alpine 2>/dev/null || true
    echo -e "${GREEN}Cleanup complete${NC}"
}

# Set up cleanup trap
trap cleanup EXIT

# Main test execution
main() {
    echo "Starting multi-platform testing..."
    echo ""
    
    # Test on different platforms
    run_platform_test "Ubuntu" "Dockerfile.ubuntu" "nexo-test-ubuntu"
    run_platform_test "macOS" "Dockerfile.macos" "nexo-test-macos"
    run_platform_test "Alpine" "Dockerfile.alpine" "nexo-test-alpine"
    
    # Note: Windows testing requires Windows containers
    echo -e "${YELLOW}Note: Windows testing requires Windows containers${NC}"
    echo -e "${YELLOW}To test on Windows, run: docker run --rm nexo-test-windows${NC}"
    echo ""
    
    # Generate final report
    generate_report
}

# Run the main function
main "$@" 