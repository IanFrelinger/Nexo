#!/bin/bash

# .NET Docker Test Runner for Nexo Pipeline
# This script runs .NET-specific tests in a Docker container

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$(dirname "$(dirname "$SCRIPT_DIR")")")"
TEST_RESULTS_DIR="$PROJECT_ROOT/test-results/dotnet"
IMAGE_NAME="nexo-dotnet-tests"

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

# Function to check prerequisites
check_prerequisites() {
    print_status "Checking prerequisites..."
    
    if ! command -v docker &> /dev/null; then
        print_error "Docker is not installed or not in PATH"
        exit 1
    fi
    
    print_success "Prerequisites check passed"
}

# Function to setup test environment
setup_test_environment() {
    print_status "Setting up test environment..."
    
    # Create test results directory
    mkdir -p "$TEST_RESULTS_DIR"
    
    print_success "Test environment setup complete"
}

# Function to build Docker image
build_image() {
    print_status "Building .NET Docker image..."
    
    cd "$SCRIPT_DIR"
    
    # Copy project files to Docker build context
    cp -r "$PROJECT_ROOT/src" .
    cp -r "$PROJECT_ROOT/tests/Nexo.Feature.Pipeline.Tests" .
    cp "$PROJECT_ROOT/global.json" .
    
    # Build image
    docker build -f Dockerfile.dotnet -t "$IMAGE_NAME" .
    
    # Cleanup copied files
    rm -rf src Nexo.Feature.Pipeline.Tests global.json
    
    print_success "Docker image built successfully"
}

# Function to run tests
run_tests() {
    print_status "Running .NET tests in Docker container..."
    
    cd "$SCRIPT_DIR"
    
    # Run container
    docker run --rm \
        -v "$TEST_RESULTS_DIR:/workspace/test-results/dotnet" \
        -e DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1 \
        -e DOTNET_CLI_TELEMETRY_OPTOUT=1 \
        "$IMAGE_NAME"
    
    print_success ".NET tests completed"
}

# Function to show test results
show_results() {
    print_status ".NET Test Results:"
    echo
    
    if [ -d "$TEST_RESULTS_DIR" ]; then
        # Count test results
        if [ -f "$TEST_RESULTS_DIR/TestResults.xml" ]; then
            passed=$(grep -c 'result="Pass"' "$TEST_RESULTS_DIR/TestResults.xml" || echo "0")
            failed=$(grep -c 'result="Fail"' "$TEST_RESULTS_DIR/TestResults.xml" || echo "0")
            skipped=$(grep -c 'result="Skip"' "$TEST_RESULTS_DIR/TestResults.xml" || echo "0")
            
            echo -e "Passed: ${GREEN}$passed${NC}"
            echo -e "Failed: ${RED}$failed${NC}"
            echo -e "Skipped: ${YELLOW}$skipped${NC}"
        else
            print_warning "No test results found"
        fi
        
        # Show coverage if available
        if [ -f "$TEST_RESULTS_DIR/coverage.info" ]; then
            echo
            print_status "Code coverage report available at: $TEST_RESULTS_DIR/coverage.info"
        fi
    else
        print_warning "Test results directory not found"
    fi
}

# Function to cleanup
cleanup() {
    print_status "Cleaning up Docker image..."
    
    docker rmi "$IMAGE_NAME" 2>/dev/null || true
    
    print_success "Cleanup completed"
}

# Function to show help
show_help() {
    echo ".NET Docker Test Runner for Nexo Pipeline"
    echo
    echo "Usage: $0 [OPTIONS]"
    echo
    echo "Options:"
    echo "  -h, --help          Show this help message"
    echo "  -b, --build-only    Only build Docker image"
    echo "  -r, --run-only      Only run tests (skip build)"
    echo "  -c, --cleanup       Clean up Docker image"
    echo "  -s, --summary       Show test results summary"
    echo
    echo "Examples:"
    echo "  $0                  # Run all .NET tests"
    echo "  $0 --build-only     # Only build image"
    echo "  $0 --run-only       # Only run tests"
}

# Main execution
main() {
    local build_only=false
    local run_only=false
    local cleanup_only=false
    local summary_only=false
    
    # Parse command line arguments
    while [[ $# -gt 0 ]]; do
        case $1 in
            -h|--help)
                show_help
                exit 0
                ;;
            -b|--build-only)
                build_only=true
                shift
                ;;
            -r|--run-only)
                run_only=true
                shift
                ;;
            -c|--cleanup)
                cleanup_only=true
                shift
                ;;
            -s|--summary)
                summary_only=true
                shift
                ;;
            *)
                print_error "Unknown option: $1"
                show_help
                exit 1
                ;;
        esac
    done
    
    # Execute based on options
    if [ "$summary_only" = true ]; then
        show_results
        exit 0
    fi
    
    if [ "$cleanup_only" = true ]; then
        cleanup
        exit 0
    fi
    
    check_prerequisites
    setup_test_environment
    
    if [ "$run_only" = false ]; then
        build_image
    fi
    
    if [ "$build_only" = false ]; then
        run_tests
        show_results
        cleanup
    fi
    
    print_success ".NET testing completed successfully!"
}

# Run main function with all arguments
main "$@" 