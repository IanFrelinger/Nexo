#!/bin/bash

# Docker Testing Script for Nexo Feature Factory
# This script runs comprehensive tests in a containerized environment

echo "🐳 Nexo Feature Factory Docker Testing"
echo "======================================="
echo ""

# Check if we're in the right directory
if [ ! -f "Nexo.sln" ]; then
    echo "❌ Error: Please run this script from the Nexo project root directory"
    exit 1
fi

# Check if Docker is available
if ! command -v docker &> /dev/null; then
    echo "❌ Error: Docker is not available"
    exit 1
fi

if ! command -v docker-compose &> /dev/null; then
    echo "❌ Error: Docker Compose is not available"
    exit 1
fi

echo "✅ Docker and Docker Compose are available"
echo ""

# Function to cleanup Docker resources
cleanup_docker() {
    echo "🧹 Cleaning up Docker resources..."
    docker-compose -f docker/docker-compose.testing.yml down --volumes --remove-orphans
    docker system prune -f
    echo "✅ Docker cleanup completed"
}

# Function to run tests
run_tests() {
    echo "🚀 Starting Docker testing environment..."
    echo ""
    
    # Start the testing environment
    docker-compose -f docker/docker-compose.testing.yml up --build --abort-on-container-exit
    local test_result=$?
    
    if [ $test_result -eq 0 ]; then
        echo "✅ Docker tests completed successfully"
    else
        echo "❌ Docker tests failed"
    fi
    
    return $test_result
}

# Function to show test results
show_results() {
    echo ""
    echo "📊 Test Results"
    echo "==============="
    
    # Check if test results exist
    if [ -d "test-results" ]; then
        echo "📁 Test results directory contents:"
        ls -la test-results/
        echo ""
        
        # Find the latest test results
        local latest_test_file=$(find test-results -name "*.trx" -type f 2>/dev/null | sort | tail -1)
        if [ -n "$latest_test_file" ]; then
            echo "📄 Latest test results: $latest_test_file"
        fi
        
        # Find JSON results
        local latest_json_file=$(find test-results -name "test-results-*.json" -type f 2>/dev/null | sort | tail -1)
        if [ -n "$latest_json_file" ]; then
            echo "📄 Latest JSON results: $latest_json_file"
            
            # Show summary if jq is available
            if command -v jq &> /dev/null; then
                echo ""
                echo "📈 Test Summary:"
                jq -r '.Summary // empty | "Success Rate: \(.SuccessRate)% (\(.SuccessfulCommandCount)/\(.TotalCommandCount))"' "$latest_json_file" 2>/dev/null || echo "Could not parse test results"
                jq -r '.Summary // empty | "Total Duration: \(.TotalDuration)"' "$latest_json_file" 2>/dev/null || echo "Could not parse duration"
                jq -r '.Summary // empty | "Overall Status: \(if .IsSuccess then "PASSED" else "FAILED" end)"' "$latest_json_file" 2>/dev/null || echo "Could not parse status"
            fi
        fi
    else
        echo "⚠️ No test results found"
    fi
    
    echo ""
}

# Function to show container logs
show_logs() {
    echo "📋 Container Logs"
    echo "================="
    
    echo "Nexo Testing Container Logs:"
    docker-compose -f docker/docker-compose.testing.yml logs nexo-testing 2>/dev/null || echo "No logs available for nexo-testing"
    echo ""
    
    echo "E2E Test Runner Logs:"
    docker-compose -f docker/docker-compose.testing.yml logs e2e-test-runner 2>/dev/null || echo "No logs available for e2e-test-runner"
    echo ""
    
    echo "Ollama Logs:"
    docker-compose -f docker/docker-compose.testing.yml logs ollama 2>/dev/null || echo "No logs available for ollama"
    echo ""
}

# Main execution
main() {
    local show_logs_flag=false
    local cleanup_flag=false
    
    # Parse command line arguments
    while [[ $# -gt 0 ]]; do
        case $1 in
            --logs)
                show_logs_flag=true
                shift
                ;;
            --cleanup)
                cleanup_flag=true
                shift
                ;;
            --help)
                echo "Usage: $0 [--logs] [--cleanup] [--help]"
                echo ""
                echo "Options:"
                echo "  --logs      Show container logs after tests"
                echo "  --cleanup   Clean up Docker resources after tests"
                echo "  --help      Show this help message"
                exit 0
                ;;
            *)
                echo "Unknown option: $1"
                echo "Use --help for usage information"
                exit 1
                ;;
        esac
    done
    
    # Run the tests
    run_tests
    local test_result=$?
    
    # Show results
    show_results
    
    # Show logs if requested
    if [ "$show_logs_flag" = true ]; then
        show_logs
    fi
    
    # Cleanup if requested
    if [ "$cleanup_flag" = true ]; then
        cleanup_docker
    fi
    
    # Final status
    if [ $test_result -eq 0 ]; then
        echo "🎉 Docker testing completed successfully!"
        echo ""
        echo "✅ All tests passed in containerized environment"
        echo "✅ Feature Factory is ready for production use"
        echo "✅ Docker environment is properly configured"
    else
        echo "❌ Docker testing failed"
        echo ""
        echo "Check the logs above for details on what went wrong"
        echo "Use --logs flag to see detailed container logs"
    fi
    
    echo ""
    echo "Next steps:"
    echo "   • Review test results in test-results/ directory"
    echo "   • Use the generated code in your projects"
    echo "   • Deploy the containerized solution to production"
    echo "   • Run 'docker-compose -f docker/docker-compose.testing.yml down' to stop containers"
    
    exit $test_result
}

# Run main function
main "$@"
