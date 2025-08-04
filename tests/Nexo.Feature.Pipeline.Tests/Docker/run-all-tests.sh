#!/bin/bash

# Cross-Runtime Docker Test Runner for Nexo Pipeline
# This script runs tests across different runtime environments using Docker containers

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
TEST_RESULTS_DIR="$PROJECT_ROOT/test-results"
DOCKER_COMPOSE_FILE="$SCRIPT_DIR/docker-compose.test.yml"

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
    
    if ! command -v docker-compose &> /dev/null; then
        print_error "Docker Compose is not installed or not in PATH"
        exit 1
    fi
    
    print_success "Prerequisites check passed"
}

# Function to create test results directory
setup_test_environment() {
    print_status "Setting up test environment..."
    
    # Create test results directory
    mkdir -p "$TEST_RESULTS_DIR"
    mkdir -p "$TEST_RESULTS_DIR/dotnet"
    mkdir -p "$TEST_RESULTS_DIR/coreclr"
    mkdir -p "$TEST_RESULTS_DIR/mono"
    mkdir -p "$TEST_RESULTS_DIR/unity"
    mkdir -p "$TEST_RESULTS_DIR/cross-runtime"
    mkdir -p "$TEST_RESULTS_DIR/report"
    
    print_success "Test environment setup complete"
}

# Function to build Docker images
build_images() {
    print_status "Building Docker images..."
    
    cd "$SCRIPT_DIR"
    
    # Build .NET image
    print_status "Building .NET 8.0 image..."
    docker build -f Dockerfile.dotnet -t nexo-dotnet-tests .
    
    # Build Unity image
    print_status "Building Unity image..."
    docker build -f Dockerfile.unity -t nexo-unity-tests .
    
    # Build Mono image
    print_status "Building Mono image..."
    docker build -f Dockerfile.mono -t nexo-mono-tests .
    
    print_success "Docker images built successfully"
}

# Function to run tests in containers
run_tests() {
    print_status "Running cross-runtime tests..."
    
    cd "$SCRIPT_DIR"
    
    # Set Unity credentials if provided
    if [ ! -z "$UNITY_EMAIL" ] && [ ! -z "$UNITY_PASSWORD" ]; then
        export UNITY_EMAIL
        export UNITY_PASSWORD
        print_status "Unity credentials provided"
    else
        print_warning "Unity credentials not provided. Unity tests may fail."
    fi
    
    # Run tests using docker-compose
    docker-compose -f "$DOCKER_COMPOSE_FILE" up --build --abort-on-container-exit
    
    print_success "All tests completed"
}

# Function to generate test report
generate_report() {
    print_status "Generating test report..."
    
    if [ -d "$TEST_RESULTS_DIR" ]; then
        # Check if reportgenerator is available
        if command -v reportgenerator &> /dev/null; then
            reportgenerator \
                -reports:"$TEST_RESULTS_DIR/**/coverage.info" \
                -targetdir:"$TEST_RESULTS_DIR/report" \
                -reporttypes:Html
            print_success "Test report generated at $TEST_RESULTS_DIR/report"
        else
            print_warning "ReportGenerator not found. Install with: dotnet tool install --global dotnet-reportgenerator-globaltool"
        fi
    else
        print_warning "Test results directory not found"
    fi
}

# Function to show test results summary
show_results_summary() {
    print_status "Test Results Summary:"
    echo
    
    if [ -d "$TEST_RESULTS_DIR" ]; then
        for runtime_dir in "$TEST_RESULTS_DIR"/*; do
            if [ -d "$runtime_dir" ]; then
                runtime_name=$(basename "$runtime_dir")
                echo -e "${BLUE}$runtime_name:${NC}"
                
                # Count test results
                if [ -f "$runtime_dir/TestResults.xml" ]; then
                    passed=$(grep -c 'result="Pass"' "$runtime_dir/TestResults.xml" || echo "0")
                    failed=$(grep -c 'result="Fail"' "$runtime_dir/TestResults.xml" || echo "0")
                    skipped=$(grep -c 'result="Skip"' "$runtime_dir/TestResults.xml" || echo "0")
                    
                    echo -e "  Passed: ${GREEN}$passed${NC}"
                    echo -e "  Failed: ${RED}$failed${NC}"
                    echo -e "  Skipped: ${YELLOW}$skipped${NC}"
                else
                    echo -e "  ${YELLOW}No test results found${NC}"
                fi
                echo
            fi
        done
    else
        print_warning "No test results found"
    fi
}

# Function to cleanup
cleanup() {
    print_status "Cleaning up Docker containers..."
    
    cd "$SCRIPT_DIR"
    docker-compose -f "$DOCKER_COMPOSE_FILE" down --volumes --remove-orphans
    
    print_success "Cleanup completed"
}

# Function to show help
show_help() {
    echo "Cross-Runtime Docker Test Runner for Nexo Pipeline"
    echo
    echo "Usage: $0 [OPTIONS]"
    echo
    echo "Options:"
    echo "  -h, --help          Show this help message"
    echo "  -b, --build-only    Only build Docker images"
    echo "  -r, --run-only      Only run tests (skip build)"
    echo "  -c, --cleanup       Clean up Docker containers and volumes"
    echo "  -s, --summary       Show test results summary"
    echo "  --unity-email       Unity email for license activation"
    echo "  --unity-password    Unity password for license activation"
    echo
    echo "Environment Variables:"
    echo "  UNITY_EMAIL         Unity email for license activation"
    echo "  UNITY_PASSWORD      Unity password for license activation"
    echo
    echo "Examples:"
    echo "  $0                                    # Run all tests"
    echo "  $0 --build-only                       # Only build images"
    echo "  $0 --run-only                         # Only run tests"
    echo "  $0 --unity-email user@example.com --unity-password pass  # With Unity credentials"
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
            --unity-email)
                export UNITY_EMAIL="$2"
                shift 2
                ;;
            --unity-password)
                export UNITY_PASSWORD="$2"
                shift 2
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
        show_results_summary
        exit 0
    fi
    
    if [ "$cleanup_only" = true ]; then
        cleanup
        exit 0
    fi
    
    check_prerequisites
    setup_test_environment
    
    if [ "$run_only" = false ]; then
        build_images
    fi
    
    if [ "$build_only" = false ]; then
        run_tests
        generate_report
        show_results_summary
        cleanup
    fi
    
    print_success "Cross-runtime testing completed successfully!"
}

# Run main function with all arguments
main "$@" 