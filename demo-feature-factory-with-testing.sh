#!/bin/bash

# Nexo Feature Factory Demo Script with End-to-End Testing
# This script demonstrates the AI-native feature factory capabilities with comprehensive testing

echo "🚀 Nexo Feature Factory Demo with End-to-End Testing"
echo "====================================================="
echo ""

# Check if we're in the right directory
if [ ! -f "Nexo.sln" ]; then
    echo "❌ Error: Please run this script from the Nexo project root directory"
    exit 1
fi

# Function to run tests
run_tests() {
    local test_type=$1
    local test_name=$2
    
    echo "🧪 Running $test_name..."
    echo "=========================="
    
    if [ "$test_type" = "docker" ]; then
        # Run tests in Docker container
        echo "🐳 Starting Docker testing environment..."
        docker-compose -f docker/docker-compose.testing.yml up --build --abort-on-container-exit
        local test_result=$?
        
        if [ $test_result -eq 0 ]; then
            echo "✅ Docker tests completed successfully"
        else
            echo "❌ Docker tests failed"
            return $test_result
        fi
    else
        # Run tests locally
        echo "🏠 Running local tests..."
        dotnet run --project src/Nexo.CLI/Nexo.CLI.csproj test feature-factory --validate-e2e --output ./test-results --verbose
        local test_result=$?
        
        if [ $test_result -eq 0 ]; then
            echo "✅ Local tests completed successfully"
        else
            echo "❌ Local tests failed"
            return $test_result
        fi
    fi
    
    echo ""
    return $test_result
}

# Function to check prerequisites
check_prerequisites() {
    echo "🔍 Checking Prerequisites"
    echo "========================="
    
    # Check if Docker is available
    if command -v docker &> /dev/null; then
        echo "✅ Docker is available"
        DOCKER_AVAILABLE=true
    else
        echo "⚠️ Docker is not available - will run local tests only"
        DOCKER_AVAILABLE=false
    fi
    
    # Check if Ollama is available
    if command -v ollama &> /dev/null; then
        echo "✅ Ollama is available"
        OLLAMA_AVAILABLE=true
    else
        echo "⚠️ Ollama is not available - will use mock responses"
        OLLAMA_AVAILABLE=false
    fi
    
    # Check if .NET is available
    if command -v dotnet &> /dev/null; then
        echo "✅ .NET SDK is available"
        DOTNET_AVAILABLE=true
    else
        echo "❌ .NET SDK is not available"
        exit 1
    fi
    
    echo ""
}

# Function to setup environment
setup_environment() {
    echo "⚙️ Setting up Environment"
    echo "========================="
    
    # Set environment variables
    export AI_PROVIDER=${AI_PROVIDER:-"ollama"}
    export AI_MODEL=${AI_MODEL:-"codellama"}
    export FEATURE_FACTORY_CACHE_ENABLED=true
    export FEATURE_FACTORY_MAX_PARALLEL_AGENTS=2
    
    echo "Environment variables set:"
    echo "  AI_PROVIDER: $AI_PROVIDER"
    echo "  AI_MODEL: $AI_MODEL"
    echo "  FEATURE_FACTORY_CACHE_ENABLED: $FEATURE_FACTORY_CACHE_ENABLED"
    echo ""
    
    # Create output directories
    mkdir -p demo-output
    mkdir -p test-results
    echo "✅ Output directories created"
    echo ""
}

# Function to build the project
build_project() {
    echo "🔨 Building Nexo Project"
    echo "========================"
    
    # Build the project
    echo "Building project..."
    dotnet build src/Nexo.CLI/Nexo.CLI.csproj --configuration Release
    if [ $? -ne 0 ]; then
        echo "❌ Build failed"
        exit 1
    fi
    
    echo "✅ Build completed successfully"
    echo ""
}

# Function to run quick validation tests
run_quick_tests() {
    echo "⚡ Quick Validation Tests"
    echo "========================="
    
    # Run quick tests to validate basic functionality
    echo "Running quick validation tests with C# test runner..."
    dotnet run --project src/Nexo.CLI/Nexo.CLI.csproj test feature-factory --output ./test-results --ai-timeout 15 --domain-timeout 1 --code-timeout 2 --filter critical
    local quick_test_result=$?
    
    if [ $quick_test_result -eq 0 ]; then
        echo "✅ Quick validation tests passed"
    else
        echo "❌ Quick validation tests failed"
        echo "Skipping demo due to test failures"
        exit 1
    fi
    
    echo ""
}

# Function to run comprehensive tests
run_comprehensive_tests() {
    echo "🧪 Comprehensive Testing"
    echo "========================"
    
    if [ "$DOCKER_AVAILABLE" = true ]; then
        echo "Running comprehensive tests in Docker environment..."
        run_tests "docker" "Comprehensive Docker Tests"
        local comprehensive_test_result=$?
    else
        echo "Running comprehensive tests locally..."
        echo "Using extended timeouts for comprehensive testing with C# test runner..."
        dotnet run --project src/Nexo.CLI/Nexo.CLI.csproj test feature-factory --validate-e2e --output ./test-results --verbose --timeout 10 --ai-timeout 60 --domain-timeout 5 --code-timeout 8 --e2e-timeout 10 --perf-timeout 5 --discover
        local comprehensive_test_result=$?
    fi
    
    if [ $comprehensive_test_result -eq 0 ]; then
        echo "✅ Comprehensive tests passed"
    else
        echo "❌ Comprehensive tests failed"
        echo "Continuing with demo despite test failures..."
    fi
    
    echo ""
}

# Function to run the demo
run_demo() {
    echo "🎮 Running Feature Factory Demo"
    echo "==============================="
    
    # Demo 1: Analyze a feature description
    echo "🔍 Demo 1: Feature Analysis"
    echo "============================"
    echo "Analyzing: 'Customer with name, email, phone, billing address. Email must be unique and validated.'"
    echo ""
    
    dotnet run --project src/Nexo.CLI/Nexo.CLI.csproj feature analyze \
        --description "Customer with name, email, phone, billing address. Email must be unique and validated." \
        --platform DotNet \
        --output "demo-output/customer-analysis.json"
    
    if [ $? -eq 0 ]; then
        echo "✅ Analysis completed successfully"
        echo "📄 Results saved to: demo-output/customer-analysis.json"
    else
        echo "❌ Analysis failed"
        return 1
    fi
    
    echo ""
    
    # Demo 2: Generate a complete feature
    echo "🏭 Demo 2: Feature Generation"
    echo "=============================="
    echo "Generating complete Customer feature with CRUD operations..."
    echo ""
    
    dotnet run --project src/Nexo.CLI/Nexo.CLI.csproj feature generate \
        --description "Customer with name, email, phone, billing address. Email must be unique and validated." \
        --platform DotNet \
        --output "demo-output/customer-feature" \
        --verbose
    
    if [ $? -eq 0 ]; then
        echo "✅ Feature generation completed successfully"
        echo "📁 Generated code saved to: demo-output/customer-feature"
        
        # Show generated files
        echo ""
        echo "📋 Generated Files:"
        find "demo-output/customer-feature" -type f -name "*.cs" 2>/dev/null | while read file; do
            echo "   • $(basename "$file")"
        done
    else
        echo "❌ Feature generation failed"
        return 1
    fi
    
    echo ""
    
    # Demo 3: Generate a more complex feature
    echo "🏗️ Demo 3: Complex Feature Generation"
    echo "======================================"
    echo "Generating Order Management feature with multiple entities..."
    echo ""
    
    dotnet run --project src/Nexo.CLI/Nexo.CLI.csproj feature generate \
        --description "Order management system with Customer, Order, OrderItem, and Product entities. Orders have status, total amount, and date. OrderItems link products to orders with quantity and price. Products have name, description, and price." \
        --platform DotNet \
        --output "demo-output/order-management" \
        --verbose
    
    if [ $? -eq 0 ]; then
        echo "✅ Complex feature generation completed successfully"
        echo "📁 Generated code saved to: demo-output/order-management"
        
        # Show generated files
        echo ""
        echo "📋 Generated Files:"
        find "demo-output/order-management" -type f -name "*.cs" 2>/dev/null | while read file; do
            echo "   • $(basename "$file")"
        done
    else
        echo "❌ Complex feature generation failed"
        return 1
    fi
    
    echo ""
    
    # Demo 4: Show file contents
    echo "📖 Demo 4: Generated Code Preview"
    echo "=================================="
    echo "Preview of generated Customer entity:"
    echo ""
    
    if [ -f "demo-output/customer-feature/src/Domain/Entities/Customer.cs" ]; then
        echo "--- Customer.cs ---"
        head -30 "demo-output/customer-feature/src/Domain/Entities/Customer.cs"
        echo "..."
        echo ""
    fi
    
    if [ -f "demo-output/customer-feature/src/Application/UseCases/CreateCustomerUseCase.cs" ]; then
        echo "--- CreateCustomerUseCase.cs ---"
        head -20 "demo-output/customer-feature/src/Application/UseCases/CreateCustomerUseCase.cs"
        echo "..."
        echo ""
    fi
    
    return 0
}

# Function to generate test report
generate_test_report() {
    echo "📊 Generating Test Report"
    echo "========================="
    
    # Find the latest test results
    local latest_test_file=$(find test-results -name "test-results-*.json" -type f 2>/dev/null | sort | tail -1)
    
    if [ -n "$latest_test_file" ]; then
        echo "📄 Latest test results: $latest_test_file"
        
        # Extract key metrics from test results
        if command -v jq &> /dev/null; then
            echo ""
            echo "📈 Test Summary:"
            jq -r '.Summary | "Success Rate: \(.SuccessRate)% (\(.SuccessfulCommandCount)/\(.TotalCommandCount))"' "$latest_test_file" 2>/dev/null || echo "Could not parse test results"
            jq -r '.Summary | "Total Duration: \(.TotalDuration)"' "$latest_test_file" 2>/dev/null || echo "Could not parse duration"
            jq -r '.Summary | "Overall Status: \(if .IsSuccess then "PASSED" else "FAILED" end)"' "$latest_test_file" 2>/dev/null || echo "Could not parse status"
        else
            echo "Install jq for detailed test report parsing"
        fi
    else
        echo "⚠️ No test results found"
    fi
    
    echo ""
}

# Function to cleanup
cleanup() {
    echo "🧹 Cleanup"
    echo "=========="
    
    if [ "$CLEANUP_AFTER_DEMO" = "true" ]; then
        echo "Cleaning up temporary files..."
        rm -rf test-results/temp-*
        echo "✅ Cleanup completed"
    else
        echo "Skipping cleanup (set CLEANUP_AFTER_DEMO=true to enable)"
    fi
    
    echo ""
}

# Main execution
main() {
    echo "Starting Nexo Feature Factory Demo with End-to-End Testing..."
    echo ""
    
    # Parse command line arguments
    RUN_TESTS=true
    RUN_DEMO=true
    USE_DOCKER=false
    
    while [[ $# -gt 0 ]]; do
        case $1 in
            --no-tests)
                RUN_TESTS=false
                shift
                ;;
            --no-demo)
                RUN_DEMO=false
                shift
                ;;
            --docker)
                USE_DOCKER=true
                shift
                ;;
            --help)
                echo "Usage: $0 [--no-tests] [--no-demo] [--docker] [--help]"
                echo ""
                echo "Options:"
                echo "  --no-tests    Skip running tests"
                echo "  --no-demo     Skip running demo"
                echo "  --docker      Use Docker for testing"
                echo "  --help        Show this help message"
                exit 0
                ;;
            *)
                echo "Unknown option: $1"
                echo "Use --help for usage information"
                exit 1
                ;;
        esac
    done
    
    # Execute the demo pipeline
    check_prerequisites
    setup_environment
    build_project
    
    if [ "$RUN_TESTS" = true ]; then
        run_quick_tests
        run_comprehensive_tests
    fi
    
    if [ "$RUN_DEMO" = true ]; then
        run_demo
    fi
    
    generate_test_report
    cleanup
    
    # Final summary
    echo "🎉 Demo Summary"
    echo "==============="
    echo "✅ Nexo Feature Factory Demo with End-to-End Testing completed!"
    echo ""
    echo "📊 What was demonstrated:"
    echo "   • End-to-end testing with command pattern"
    echo "   • Docker containerized testing environment"
    echo "   • Natural language feature analysis"
    echo "   • Complete feature generation with Clean Architecture"
    echo "   • Entity, Repository, Use Case, and Test generation"
    echo "   • Complex multi-entity feature generation"
    echo "   • AI-powered domain analysis and code generation"
    echo ""
    echo "📁 Generated artifacts are available in:"
    echo "   • demo-output/ (demo results)"
    echo "   • test-results/ (test results and reports)"
    echo ""
    echo "🚀 The AI-native feature factory with comprehensive testing is ready for production use!"
    echo ""
    echo "Next steps:"
    echo "   • Review generated code for quality and completeness"
    echo "   • Integrate generated features into your projects"
    echo "   • Extend the system with custom agents and templates"
    echo "   • Deploy to production environments"
    echo "   • Use Docker for consistent testing across environments"
}

# Run main function
main "$@"
