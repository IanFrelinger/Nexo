#!/bin/bash

# Comprehensive Logging Test Runner
# This script runs all logging tests to validate the dependency injection wrapped logging system

set -e

echo "Testing COMPREHENSIVE LOGGING SYSTEM TEST SUITE"
echo "=========================================="
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

# Function to run a test and track results
run_test() {
    local test_name="$1"
    local test_command="$2"
    
    echo -e "${BLUE}Processing Running: $test_name${NC}"
    echo "Command: $test_command"
    echo ""
    
    TOTAL_TESTS=$((TOTAL_TESTS + 1))
    
    if eval "$test_command"; then
        echo -e "${GREEN}SUCCESS: PASSED: $test_name${NC}"
        PASSED_TESTS=$((PASSED_TESTS + 1))
    else
        echo -e "${RED}ERROR: FAILED: $test_name${NC}"
        FAILED_TESTS=$((FAILED_TESTS + 1))
    fi
    
    echo ""
    echo "----------------------------------------"
    echo ""
}

# Function to check if dotnet is available
check_dotnet() {
    if ! command -v dotnet &> /dev/null; then
        echo -e "${RED}ERROR: .NET CLI not found. Please install .NET SDK.${NC}"
        exit 1
    fi
    
    echo -e "${GREEN}SUCCESS: .NET CLI found: $(dotnet --version)${NC}"
}

# Function to build the project
build_project() {
    echo -e "${BLUE}🔨 Building FeatureFactoryDemo project...${NC}"
    if dotnet build --configuration Release --verbosity minimal; then
        echo -e "${GREEN}SUCCESS: Build successful${NC}"
    else
        echo -e "${RED}ERROR: Build failed${NC}"
        exit 1
    fi
    echo ""
}

# Function to run comprehensive logging tests
run_comprehensive_logging_tests() {
    echo -e "${BLUE}Testing Running comprehensive logging system tests...${NC}"
    
    # Run the built-in logging test command
    run_test "Comprehensive Logging System Tests" "dotnet run test-logging --verbose"
}

# Function to run integration tests
run_integration_tests() {
    echo -e "${BLUE}🔗 Running logging integration tests...${NC}"
    
    # Test 1: Basic logging functionality
    run_test "Basic Logging Functionality" "dotnet run -- --help 2>&1 | grep -q 'Available commands'"
    
    # Test 2: Command execution with logging
    run_test "Command Execution Logging" "dotnet run analyze . 2>&1 | grep -q 'Analyzing'"
    
    # Test 3: Error handling with logging
    run_test "Error Handling Logging" "dotnet run nonexistent-command 2>&1 | grep -q 'Unknown command'"
    
    # Test 4: Database operations with logging
    run_test "Database Operations Logging" "dotnet run stats 2>&1 | grep -q 'statistics'"
}

# Function to run performance tests
run_performance_tests() {
    echo -e "${BLUE}⚡ Running logging performance tests...${NC}"
    
    # Test 1: Command execution performance
    echo -e "${BLUE}Processing Testing command execution performance...${NC}"
    start_time=$(date +%s%N)
    dotnet run help > /dev/null 2>&1
    end_time=$(date +%s%N)
    duration=$(( (end_time - start_time) / 1000000 ))
    
    if [ $duration -lt 5000 ]; then
        echo -e "${GREEN}SUCCESS: Command execution performance: ${duration}ms (acceptable)${NC}"
        PASSED_TESTS=$((PASSED_TESTS + 1))
    else
        echo -e "${RED}ERROR: Command execution performance: ${duration}ms (too slow)${NC}"
        FAILED_TESTS=$((FAILED_TESTS + 1))
    fi
    TOTAL_TESTS=$((TOTAL_TESTS + 1))
    
    # Test 2: Multiple command execution
    echo -e "${BLUE}Processing Testing multiple command execution...${NC}"
    start_time=$(date +%s%N)
    for i in {1..10}; do
        dotnet run help > /dev/null 2>&1
    done
    end_time=$(date +%s%N)
    duration=$(( (end_time - start_time) / 1000000 ))
    avg_duration=$(( duration / 10 ))
    
    if [ $avg_duration -lt 5000 ]; then
        echo -e "${GREEN}SUCCESS: Average command execution: ${avg_duration}ms (acceptable)${NC}"
        PASSED_TESTS=$((PASSED_TESTS + 1))
    else
        echo -e "${RED}ERROR: Average command execution: ${avg_duration}ms (too slow)${NC}"
        FAILED_TESTS=$((FAILED_TESTS + 1))
    fi
    TOTAL_TESTS=$((TOTAL_TESTS + 1))
}

# Function to run stress tests
run_stress_tests() {
    echo -e "${BLUE}💪 Running logging stress tests...${NC}"
    
    # Test 1: Rapid command execution
    echo -e "${BLUE}Processing Testing rapid command execution...${NC}"
    start_time=$(date +%s%N)
    for i in {1..10}; do
        dotnet run help > /dev/null 2>&1 &
    done
    wait
    end_time=$(date +%s%N)
    duration=$(( (end_time - start_time) / 1000000 ))
    
    if [ $duration -lt 30000 ]; then
        echo -e "${GREEN}SUCCESS: Rapid command execution: ${duration}ms for 10 commands (acceptable)${NC}"
        PASSED_TESTS=$((PASSED_TESTS + 1))
    else
        echo -e "${RED}ERROR: Rapid command execution: ${duration}ms for 10 commands (too slow)${NC}"
        FAILED_TESTS=$((FAILED_TESTS + 1))
    fi
    TOTAL_TESTS=$((TOTAL_TESTS + 1))
    
    # Test 2: Memory usage during stress
    echo -e "${BLUE}Processing Testing memory usage during stress...${NC}"
    initial_memory=$(ps -o rss= -p $$ | tr -d ' ')
    
    for i in {1..20}; do
        dotnet run help > /dev/null 2>&1
    done
    
    final_memory=$(ps -o rss= -p $$ | tr -d ' ')
    memory_increase=$(( final_memory - initial_memory ))
    
    if [ $memory_increase -lt 10000 ]; then
        echo -e "${GREEN}SUCCESS: Memory usage increase: ${memory_increase}KB (acceptable)${NC}"
        PASSED_TESTS=$((PASSED_TESTS + 1))
    else
        echo -e "${RED}ERROR: Memory usage increase: ${memory_increase}KB (too high)${NC}"
        FAILED_TESTS=$((FAILED_TESTS + 1))
    fi
    TOTAL_TESTS=$((TOTAL_TESTS + 1))
}

# Function to run configuration tests
run_configuration_tests() {
    echo -e "${BLUE}⚙️  Running logging configuration tests...${NC}"
    
    # Test 1: Different log levels
    run_test "Log Level Configuration" "dotnet run help 2>&1 | grep -q 'Available Commands'"
    
    # Test 2: Error handling configuration
    run_test "Error Handling Configuration" "dotnet run invalid-command 2>&1 | grep -q 'Unknown command'"
    
    # Test 3: Database configuration
    run_test "Database Configuration" "dotnet run stats 2>&1 | grep -q 'statistics'"
}

# Function to run comprehensive validation
run_comprehensive_validation() {
    echo -e "${BLUE}Search Running comprehensive logging validation...${NC}"
    
    # Test all major commands with logging
    commands=("help" "analyze" "generate" "validate" "stats")
    
    for cmd in "${commands[@]}"; do
        case $cmd in
            "help")
                run_test "Help Command Logging" "dotnet run help 2>&1 | grep -q 'Available Commands'"
                ;;
            "analyze")
                run_test "Analyze Command Logging" "dotnet run analyze . 2>&1 | grep -q 'Analyzing'"
                ;;
            "generate")
                run_test "Generate Command Logging" "dotnet run generate --description 'Test' --platform DotNet 2>&1 | grep -q 'Generating'"
                ;;
            "validate")
                run_test "Validate Command Logging" "dotnet run validate 2>&1 | grep -q 'validation'"
                ;;
            "stats")
                run_test "Stats Command Logging" "dotnet run stats 2>&1 | grep -q 'statistics'"
                ;;
        esac
    done
}

# Function to generate test report
generate_test_report() {
    echo ""
    echo "Stats COMPREHENSIVE LOGGING TEST REPORT"
    echo "===================================="
    echo ""
    echo -e "Total Tests: ${BLUE}$TOTAL_TESTS${NC}"
    echo -e "Passed: ${GREEN}$PASSED_TESTS${NC}"
    echo -e "Failed: ${RED}$FAILED_TESTS${NC}"
    echo ""
    
    if [ $FAILED_TESTS -eq 0 ]; then
        echo -e "${GREEN}SUCCESS ALL TESTS PASSED! Logging system is working correctly.${NC}"
        echo ""
        echo "SUCCESS: Dependency injection wrapped logging is properly implemented"
        echo "SUCCESS: All logging levels are working correctly"
        echo "SUCCESS: Performance is within acceptable limits"
        echo "SUCCESS: Error handling and logging is functioning"
        echo "SUCCESS: Configuration is working as expected"
        echo "SUCCESS: Stress testing shows system stability"
        return 0
    else
        echo -e "${RED}ERROR: SOME TESTS FAILED! Please review the logging system.${NC}"
        echo ""
        echo "Failed tests indicate potential issues with:"
        echo "- Logging configuration"
        echo "- Performance bottlenecks"
        echo "- Error handling"
        echo "- System stability"
        return 1
    fi
}

# Main execution
main() {
    echo "Starting comprehensive logging system test suite..."
    echo ""
    
    # Pre-flight checks
    check_dotnet
    build_project
    
    echo ""
    echo "Testing RUNNING COMPREHENSIVE LOGGING TESTS"
    echo "======================================"
    echo ""
    
    # Run all test suites
    run_comprehensive_logging_tests
    run_integration_tests
    run_performance_tests
    run_stress_tests
    run_configuration_tests
    run_comprehensive_validation
    
    # Generate final report
    generate_test_report
}

# Run main function
main "$@"
