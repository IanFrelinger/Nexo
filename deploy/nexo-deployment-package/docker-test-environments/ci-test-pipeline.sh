#!/bin/bash

# CI/CD Multi-Platform Test Pipeline
# Designed for GitHub Actions, GitLab CI, Azure DevOps, etc.

set -e

echo "🚀 Nexo Framework CI/CD Multi-Platform Testing"
echo "=============================================="
echo ""

# Environment variables
PLATFORM=${PLATFORM:-"all"}
TEST_TIMEOUT=${TEST_TIMEOUT:-"300"}
PARALLEL_TESTS=${PARALLEL_TESTS:-"false"}

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Test results
declare -A TEST_RESULTS
TOTAL_TESTS=0
PASSED_TESTS=0
FAILED_TESTS=0

# Function to run single platform test
run_single_platform_test() {
    local platform=$1
    local dockerfile="Dockerfile.${platform,,}"
    local log_file="../test-results/${platform}-ci.log"
    
    echo -e "${YELLOW}Testing ${platform}...${NC}"
    
    # Build with timeout
    if timeout $TEST_TIMEOUT docker build -f "$dockerfile" -t "nexo-ci-${platform,,}" .; then
        echo -e "  ${GREEN}✅ ${platform} build successful${NC}"
    else
        echo -e "  ${RED}❌ ${platform} build failed${NC}"
        TEST_RESULTS[$platform]="FAILED"
        ((FAILED_TESTS++))
        return 1
    fi
    
    # Run tests with timeout
    if timeout $TEST_TIMEOUT docker run --rm \
        -v "$(pwd)/../test-results:/test/results" \
        -e NEXO_LOG_LEVEL=Information \
        -e DOTNET_RUNNING_IN_CONTAINER=true \
        "nexo-ci-${platform,,}" > "$log_file" 2>&1; then
        
        if grep -q "Deployment Status: ✅ SUCCESS" "$log_file"; then
            echo -e "  ${GREEN}✅ ${platform} tests PASSED${NC}"
            TEST_RESULTS[$platform]="PASSED"
            ((PASSED_TESTS++))
        else
            echo -e "  ${RED}❌ ${platform} tests FAILED${NC}"
            echo "    Log: $log_file"
            TEST_RESULTS[$platform]="FAILED"
            ((FAILED_TESTS++))
        fi
    else
        echo -e "  ${RED}❌ ${platform} execution failed${NC}"
        TEST_RESULTS[$platform]="FAILED"
        ((FAILED_TESTS++))
    fi
    
    ((TOTAL_TESTS++))
}

# Function to run parallel tests
run_parallel_tests() {
    echo -e "${BLUE}Running tests in parallel...${NC}"
    
    # Start all test containers in background
    run_single_platform_test "Ubuntu" &
    run_single_platform_test "macOS" &
    run_single_platform_test "Alpine" &
    
    # Wait for all tests to complete
    wait
    
    echo -e "${BLUE}All parallel tests completed${NC}"
}

# Function to run sequential tests
run_sequential_tests() {
    echo -e "${BLUE}Running tests sequentially...${NC}"
    
    run_single_platform_test "Ubuntu"
    run_single_platform_test "macOS"
    run_single_platform_test "Alpine"
}

# Function to generate CI report
generate_ci_report() {
    echo ""
    echo -e "${YELLOW}CI/CD Test Report${NC}"
    echo "=================="
    echo -e "Total Platforms: ${TOTAL_TESTS}"
    echo -e "Passed: ${GREEN}${PASSED_TESTS}${NC}"
    echo -e "Failed: ${RED}${FAILED_TESTS}${NC}"
    echo ""
    
    # Platform-specific results
    for platform in "${!TEST_RESULTS[@]}"; do
        if [ "${TEST_RESULTS[$platform]}" = "PASSED" ]; then
            echo -e "  ${platform}: ${GREEN}✅ PASSED${NC}"
        else
            echo -e "  ${platform}: ${RED}❌ FAILED${NC}"
        fi
    done
    
    echo ""
    
    # Set exit code for CI
    if [ $FAILED_TESTS -eq 0 ]; then
        echo -e "${GREEN}🎉 All CI tests passed!${NC}"
        echo -e "${GREEN}CI Status: ✅ SUCCESS${NC}"
        exit 0
    else
        echo -e "${RED}❌ Some CI tests failed${NC}"
        echo -e "${RED}CI Status: ❌ FAILED${NC}"
        exit 1
    fi
}

# Function to create test artifacts
create_artifacts() {
    echo -e "${BLUE}Creating test artifacts...${NC}"
    
    # Create summary report
    cat > "../test-results/ci-summary.md" << EOF
# Nexo Framework CI/CD Test Summary

## Test Results
- **Total Platforms**: $TOTAL_TESTS
- **Passed**: $PASSED_TESTS
- **Failed**: $FAILED_TESTS
- **Success Rate**: $((PASSED_TESTS * 100 / TOTAL_TESTS))%

## Platform Results
EOF
    
    for platform in "${!TEST_RESULTS[@]}"; do
        echo "- **${platform}**: ${TEST_RESULTS[$platform]}" >> "../test-results/ci-summary.md"
    done
    
    echo "" >> "../test-results/ci-summary.md"
    echo "## Test Logs" >> "../test-results/ci-summary.md"
    echo "- [Ubuntu Test Log](./ubuntu-ci.log)" >> "../test-results/ci-summary.md"
    echo "- [macOS Test Log](./macos-ci.log)" >> "../test-results/ci-summary.md"
    echo "- [Alpine Test Log](./alpine-ci.log)" >> "../test-results/ci-summary.md"
    
    echo -e "${GREEN}✅ Artifacts created in ../test-results/${NC}"
}

# Function to clean up
cleanup() {
    echo -e "${BLUE}Cleaning up CI containers...${NC}"
    docker rmi nexo-ci-ubuntu nexo-ci-macos nexo-ci-alpine 2>/dev/null || true
    echo -e "${GREEN}Cleanup complete${NC}"
}

# Set up cleanup trap
trap cleanup EXIT

# Main CI execution
main() {
    echo "Starting CI/CD multi-platform testing..."
    echo -e "Platform: ${PLATFORM}"
    echo -e "Timeout: ${TEST_TIMEOUT}s"
    echo -e "Parallel: ${PARALLEL_TESTS}"
    echo ""
    
    # Create results directory
    mkdir -p ../test-results
    
    # Run tests based on configuration
    if [ "$PARALLEL_TESTS" = "true" ]; then
        run_parallel_tests
    else
        run_sequential_tests
    fi
    
    # Create artifacts
    create_artifacts
    
    # Generate final report
    generate_ci_report
}

# Run the main function
main "$@" 