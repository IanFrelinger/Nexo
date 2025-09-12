#!/bin/bash

# RAG System Test Runner
# This script runs comprehensive tests for the RAG system

set -e

echo "🧪 Running RAG System Tests"
echo "=========================="

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

# Check if we're in the right directory
if [ ! -f "Nexo.sln" ]; then
    print_error "Please run this script from the Nexo project root directory"
    exit 1
fi

# Build the solution first
print_status "Building solution..."
dotnet build Nexo.sln --configuration Release --verbosity minimal
if [ $? -ne 0 ]; then
    print_error "Build failed"
    exit 1
fi
print_success "Build completed"

# Run unit tests
print_status "Running RAG unit tests..."
dotnet test tests/Nexo.Feature.AI.Tests/RAG/Nexo.Feature.AI.RAG.Tests.csproj \
    --configuration Release \
    --verbosity normal \
    --logger "console;verbosity=normal" \
    --collect:"XPlat Code Coverage" \
    --results-directory ./test-results/rag-unit

if [ $? -eq 0 ]; then
    print_success "RAG unit tests passed"
else
    print_error "RAG unit tests failed"
    exit 1
fi

# Run integration tests
print_status "Running RAG integration tests..."
dotnet test tests/Nexo.Feature.AI.Tests/RAG/RAGIntegrationTests.cs \
    --configuration Release \
    --verbosity normal \
    --logger "console;verbosity=normal" \
    --collect:"XPlat Code Coverage" \
    --results-directory ./test-results/rag-integration

if [ $? -eq 0 ]; then
    print_success "RAG integration tests passed"
else
    print_error "RAG integration tests failed"
    exit 1
fi

# Run performance tests
print_status "Running RAG performance tests..."
dotnet test tests/Nexo.Feature.AI.Tests/RAG/RAGPerformanceTests.cs \
    --configuration Release \
    --verbosity normal \
    --logger "console;verbosity=normal" \
    --collect:"XPlat Code Coverage" \
    --results-directory ./test-results/rag-performance

if [ $? -eq 0 ]; then
    print_success "RAG performance tests passed"
else
    print_error "RAG performance tests failed"
    exit 1
fi

# Run agent tests
print_status "Running RAG agent tests..."
dotnet test tests/Nexo.Feature.Agent.Tests/RAG/ \
    --configuration Release \
    --verbosity normal \
    --logger "console;verbosity=normal" \
    --collect:"XPlat Code Coverage" \
    --results-directory ./test-results/rag-agents

if [ $? -eq 0 ]; then
    print_success "RAG agent tests passed"
else
    print_error "RAG agent tests failed"
    exit 1
fi

# Run all RAG-related tests together
print_status "Running all RAG tests..."
dotnet test Nexo.sln \
    --configuration Release \
    --verbosity normal \
    --logger "console;verbosity=normal" \
    --filter "Category=RAG" \
    --collect:"XPlat Code Coverage" \
    --results-directory ./test-results/rag-all

if [ $? -eq 0 ]; then
    print_success "All RAG tests passed"
else
    print_error "Some RAG tests failed"
    exit 1
fi

# Generate test report
print_status "Generating test report..."
if [ -d "./test-results" ]; then
    echo "Test results saved to ./test-results/"
    echo "Coverage reports available in subdirectories"
    
    # Count test results
    total_tests=0
    passed_tests=0
    failed_tests=0
    
    for result_dir in ./test-results/*/; do
        if [ -d "$result_dir" ]; then
            echo "Results in $result_dir:"
            ls -la "$result_dir"
        fi
    done
fi

# Run specific test categories
print_status "Running specific test categories..."

# Unit tests only
print_status "Running unit tests only..."
dotnet test tests/Nexo.Feature.AI.Tests/RAG/ \
    --configuration Release \
    --verbosity minimal \
    --filter "Category=Unit" \
    --logger "console;verbosity=minimal"

# Integration tests only
print_status "Running integration tests only..."
dotnet test tests/Nexo.Feature.AI.Tests/RAG/ \
    --configuration Release \
    --verbosity minimal \
    --filter "Category=Integration" \
    --logger "console;verbosity=minimal"

# Performance tests only
print_status "Running performance tests only..."
dotnet test tests/Nexo.Feature.AI.Tests/RAG/ \
    --configuration Release \
    --verbosity minimal \
    --filter "Category=Performance" \
    --logger "console;verbosity=minimal"

print_success "All RAG tests completed successfully!"
echo ""
echo "📊 Test Summary:"
echo "- Unit tests: ✅ Passed"
echo "- Integration tests: ✅ Passed"
echo "- Performance tests: ✅ Passed"
echo "- Agent tests: ✅ Passed"
echo ""
echo "🎉 RAG system is working correctly!"
