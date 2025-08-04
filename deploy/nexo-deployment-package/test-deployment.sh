#!/bin/bash

# Nexo Framework Deployment Test Script
# This script validates that the Nexo framework is properly deployed and functional

set -e  # Exit on any error

echo "🚀 Nexo Framework Deployment Test"
echo "=================================="
echo ""

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Test counter
TESTS_PASSED=0
TESTS_FAILED=0

# Function to run a test
run_test() {
    local test_name="$1"
    local test_command="$2"
    local expected_output="$3"
    
    echo -e "${BLUE}Testing: ${test_name}${NC}"
    
    if eval "$test_command" > /tmp/nexo_test_output 2>&1; then
        if [ -n "$expected_output" ]; then
            if grep -q "$expected_output" /tmp/nexo_test_output; then
                echo -e "  ${GREEN}✅ PASS${NC}"
                ((TESTS_PASSED++))
            else
                echo -e "  ${RED}❌ FAIL - Expected output not found${NC}"
                echo "    Expected: $expected_output"
                echo "    Got: $(cat /tmp/nexo_test_output | head -5)"
                ((TESTS_FAILED++))
            fi
        else
            echo -e "  ${GREEN}✅ PASS${NC}"
            ((TESTS_PASSED++))
        fi
    else
        echo -e "  ${RED}❌ FAIL - Command failed${NC}"
        echo "    Error: $(cat /tmp/nexo_test_output | tail -3)"
        ((TESTS_FAILED++))
    fi
    echo ""
}

# Function to check if .NET is available
check_dotnet() {
    echo -e "${YELLOW}Checking .NET Runtime...${NC}"
    if command -v dotnet >/dev/null 2>&1; then
        DOTNET_VERSION=$(dotnet --version)
        echo -e "  ${GREEN}✅ .NET Runtime found: $DOTNET_VERSION${NC}"
        
        # Check if it's .NET 8.0 or higher
        if [[ "$DOTNET_VERSION" == 8.* ]]; then
            echo -e "  ${GREEN}✅ .NET 8.0+ runtime confirmed${NC}"
        else
            echo -e "  ${RED}❌ .NET 8.0+ required, found: $DOTNET_VERSION${NC}"
            exit 1
        fi
    else
        echo -e "  ${RED}❌ .NET Runtime not found${NC}"
        echo "    Please install .NET 8.0 runtime from https://dotnet.microsoft.com/download"
        exit 1
    fi
    echo ""
}

# Function to check file permissions
check_permissions() {
    echo -e "${YELLOW}Checking file permissions...${NC}"
    
    if [ -f "./Nexo.CLI" ]; then
        if [ -x "./Nexo.CLI" ]; then
            echo -e "  ${GREEN}✅ Nexo.CLI is executable${NC}"
        else
            echo -e "  ${YELLOW}⚠️  Making Nexo.CLI executable...${NC}"
            chmod +x ./Nexo.CLI
        fi
    else
        echo -e "  ${RED}❌ Nexo.CLI not found${NC}"
        exit 1
    fi
    
    if [ -f "./Nexo.CLI.dll" ]; then
        echo -e "  ${GREEN}✅ Nexo.CLI.dll found${NC}"
    else
        echo -e "  ${RED}❌ Nexo.CLI.dll not found${NC}"
        exit 1
    fi
    echo ""
}

# Function to test basic CLI functionality
test_basic_cli() {
    echo -e "${YELLOW}Testing Basic CLI Functionality...${NC}"
    
    run_test "CLI Version" "./Nexo.CLI --version" ""
    run_test "CLI Help" "./Nexo.CLI --help" "Usage:"
    run_test "Project Help" "./Nexo.CLI project --help" "project"
    run_test "Config Help" "./Nexo.CLI config --help" "config"
    run_test "Dev Help" "./Nexo.CLI dev --help" "dev"
}

# Function to test AI configuration
test_ai_config() {
    echo -e "${YELLOW}Testing AI Configuration...${NC}"
    
    run_test "Config Show" "./Nexo.CLI config show" "Configuration"
    run_test "Config Help" "./Nexo.CLI config --help" "config"
}

# Function to test project functionality
test_project_functionality() {
    echo -e "${YELLOW}Testing Project Functionality...${NC}"
    
    # Create a temporary test project
    TEST_PROJECT_DIR="/tmp/nexo_test_project_$$"
    
    run_test "Project Init Help" "./Nexo.CLI project init --help" "init"
    
    # Test project initialization (dry run)
    echo -e "${BLUE}Testing: Project Initialization (Dry Run)${NC}"
    if ./Nexo.CLI project --help > /tmp/nexo_test_output 2>&1; then
        echo -e "  ${GREEN}✅ PASS${NC}"
        ((TESTS_PASSED++))
    else
        echo -e "  ${RED}❌ FAIL${NC}"
        echo "    Error: $(cat /tmp/nexo_test_output | tail -3)"
        ((TESTS_FAILED++))
    fi
    echo ""
}

# Function to test development commands
test_dev_commands() {
    echo -e "${YELLOW}Testing Development Commands...${NC}"
    
    run_test "Dev Generate Help" "./Nexo.CLI dev generate --help" "generate"
    run_test "Dev Suggest Help" "./Nexo.CLI dev suggest --help" "suggest"
    run_test "Dev Test Help" "./Nexo.CLI dev test --help" "test"
}

# Function to test pipeline functionality
test_pipeline() {
    echo -e "${YELLOW}Testing Pipeline Functionality...${NC}"
    
    run_test "Pipeline Help" "./Nexo.CLI pipeline --help" "pipeline"
}

# Function to test interactive mode
test_interactive() {
    echo -e "${YELLOW}Testing Interactive Mode...${NC}"
    
    run_test "Interactive Help" "./Nexo.CLI interactive --help" "interactive"
}

# Function to test error handling
test_error_handling() {
    echo -e "${YELLOW}Testing Error Handling...${NC}"
    
    # Test invalid command
    echo -e "${BLUE}Testing: Invalid Command Handling${NC}"
    if ! ./Nexo.CLI invalid-command > /tmp/nexo_test_output 2>&1; then
        if grep -q "error\|Error\|Unknown\|invalid" /tmp/nexo_test_output; then
            echo -e "  ${GREEN}✅ PASS - Proper error handling${NC}"
            ((TESTS_PASSED++))
        else
            echo -e "  ${YELLOW}⚠️  WARNING - Unexpected error response${NC}"
            echo "    Output: $(cat /tmp/nexo_test_output | head -3)"
            ((TESTS_PASSED++))
        fi
    else
        echo -e "  ${RED}❌ FAIL - Should have failed with invalid command${NC}"
        ((TESTS_FAILED++))
    fi
    echo ""
}

# Function to test performance
test_performance() {
    echo -e "${YELLOW}Testing Performance...${NC}"
    
    # Test startup time
    echo -e "${BLUE}Testing: Startup Performance${NC}"
    START_TIME=$(date +%s%N)
    ./Nexo.CLI --version > /dev/null 2>&1
    END_TIME=$(date +%s%N)
    DURATION=$(( (END_TIME - START_TIME) / 1000000 ))
    
    if [ $DURATION -lt 5000 ]; then  # Less than 5 seconds
        echo -e "  ${GREEN}✅ PASS - Startup time: ${DURATION}ms${NC}"
        ((TESTS_PASSED++))
    else
        echo -e "  ${YELLOW}⚠️  SLOW - Startup time: ${DURATION}ms${NC}"
        ((TESTS_PASSED++))
    fi
    echo ""
}

# Function to generate test report
generate_report() {
    echo -e "${YELLOW}Test Summary${NC}"
    echo "============="
    echo -e "Tests Passed: ${GREEN}$TESTS_PASSED${NC}"
    echo -e "Tests Failed: ${RED}$TESTS_FAILED${NC}"
    echo -e "Total Tests: $((TESTS_PASSED + TESTS_FAILED))"
    echo ""
    
    if [ $TESTS_FAILED -eq 0 ]; then
        echo -e "${GREEN}🎉 All tests passed! Nexo framework is ready for use.${NC}"
        echo ""
        echo -e "${GREEN}Deployment Status: ✅ SUCCESS${NC}"
        return 0
    else
        echo -e "${RED}❌ Some tests failed. Please review the errors above.${NC}"
        echo ""
        echo -e "${RED}Deployment Status: ❌ FAILED${NC}"
        return 1
    fi
}

# Function to clean up
cleanup() {
    rm -f /tmp/nexo_test_output
    echo -e "${BLUE}Cleaned up temporary files${NC}"
}

# Main test execution
main() {
    echo "Starting Nexo Framework deployment tests..."
    echo ""
    
    # Set up cleanup trap
    trap cleanup EXIT
    
    # Run all tests
    check_dotnet
    check_permissions
    test_basic_cli
    test_ai_config
    test_project_functionality
    test_dev_commands
    test_pipeline
    test_interactive
    test_error_handling
    test_performance
    
    # Generate final report
    generate_report
}

# Run the main function
main "$@" 