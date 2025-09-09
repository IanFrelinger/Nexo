#!/bin/bash

# Comprehensive test runner for Nexo Iteration Strategy System
# This script runs all tests related to the iteration strategy system

echo "🧪 Running Nexo Iteration Strategy System Tests"
echo "================================================"

# Set error handling
set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Function to print colored output
print_status() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

print_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Function to run tests for a specific project
run_tests() {
    local project_name=$1
    local project_path=$2
    
    print_status "Running tests for $project_name..."
    
    if [ -d "$project_path" ]; then
        cd "$project_path"
        
        # Build the project
        print_status "Building $project_name..."
        dotnet build --configuration Release --verbosity quiet
        
        if [ $? -eq 0 ]; then
            print_success "Build successful for $project_name"
            
            # Run tests
            print_status "Running tests for $project_name..."
            dotnet test --configuration Release --verbosity normal --logger "console;verbosity=normal"
            
            if [ $? -eq 0 ]; then
                print_success "All tests passed for $project_name"
            else
                print_error "Some tests failed for $project_name"
                return 1
            fi
        else
            print_error "Build failed for $project_name"
            return 1
        fi
        
        cd - > /dev/null
    else
        print_warning "Project directory not found: $project_path"
        return 1
    fi
}

# Main execution
main() {
    local start_time=$(date +%s)
    local failed_projects=()
    
    print_status "Starting comprehensive test run..."
    print_status "Test coverage includes:"
    echo "  • Core iteration strategy implementations"
    echo "  • Strategy selector and environment detection"
    echo "  • AI agent integration"
    echo "  • Pipeline command integration"
    echo "  • Configuration system"
    echo "  • CLI command integration"
    echo "  • End-to-end integration tests"
    echo ""
    
    # Test projects in order of dependency
    local test_projects=(
        "Nexo.Core.Application.Tests:src/Nexo.Core.Application.Tests"
        "Nexo.Infrastructure.Tests:src/Nexo.Infrastructure.Tests"
        "Nexo.Feature.AI.Tests:src/Nexo.Feature.AI.Tests"
        "Nexo.Feature.Pipeline.Tests:src/Nexo.Feature.Pipeline.Tests"
        "Nexo.CLI.Tests:src/Nexo.CLI.Tests"
        "Nexo.Integration.Tests:src/Nexo.Integration.Tests"
    )
    
    # Run tests for each project
    for project in "${test_projects[@]}"; do
        IFS=':' read -r name path <<< "$project"
        
        if ! run_tests "$name" "$path"; then
            failed_projects+=("$name")
        fi
        
        echo ""
    done
    
    # Summary
    local end_time=$(date +%s)
    local duration=$((end_time - start_time))
    
    echo "================================================"
    print_status "Test run completed in ${duration} seconds"
    
    if [ ${#failed_projects[@]} -eq 0 ]; then
        print_success "🎉 All tests passed! The iteration strategy system is working correctly."
        echo ""
        print_status "Test Summary:"
        echo "  ✅ Core iteration strategies (ForLoop, Foreach, LINQ, Parallel, Unity)"
        echo "  ✅ Strategy selection and environment detection"
        echo "  ✅ AI agent integration for iteration optimization"
        echo "  ✅ Pipeline command integration"
        echo "  ✅ Configuration system with environment overrides"
        echo "  ✅ CLI command integration"
        echo "  ✅ End-to-end integration workflows"
        echo ""
        print_status "The iteration strategy system is ready for production use!"
        return 0
    else
        print_error "❌ Some test projects failed:"
        for project in "${failed_projects[@]}"; do
            echo "  • $project"
        done
        echo ""
        print_error "Please review the test failures and fix any issues."
        return 1
    fi
}

# Check if we're in the right directory
if [ ! -f "Nexo.sln" ]; then
    print_error "Please run this script from the Nexo project root directory"
    exit 1
fi

# Check if dotnet is available
if ! command -v dotnet &> /dev/null; then
    print_error "dotnet CLI is not installed or not in PATH"
    exit 1
fi

# Run the main function
main "$@"
