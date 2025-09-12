#!/bin/bash

# RAG Functionality Validation Script
# This script validates that the RAG system works end-to-end

set -e

echo "🔍 Validating RAG Functionality"
echo "==============================="

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

# Build the solution
print_status "Building solution..."
dotnet build Nexo.sln --configuration Release --verbosity minimal
if [ $? -ne 0 ]; then
    print_error "Build failed"
    exit 1
fi
print_success "Build completed"

# Create test documentation directory
print_status "Setting up test documentation..."
mkdir -p ./test-docs/rag-documentation/Languages/CSharp/8.0
mkdir -p ./test-docs/rag-documentation/Frameworks/Net5Plus/8.0

# Create test documentation files
cat > ./test-docs/rag-documentation/Languages/CSharp/8.0/async-await.md << 'EOF'
# Async and Await in C# 8.0

## Overview
The async and await keywords in C# provide a way to write asynchronous code that looks like synchronous code.

## Basic Syntax
```csharp
public async Task<string> GetDataAsync()
{
    var data = await SomeAsyncOperation();
    return data;
}
```

## Best Practices
- Use ConfigureAwait(false) in library code
- Don't use async void except for event handlers
- Use Task.Run for CPU-bound work
EOF

cat > ./test-docs/rag-documentation/Frameworks/Net5Plus/8.0/minimal-apis.md << 'EOF'
# Minimal APIs in .NET 8.0

## Overview
Minimal APIs in .NET 8.0 provide a lightweight way to create HTTP APIs with minimal code and configuration.

## Basic Setup
```csharp
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => "Hello World!");
app.Run();
```

## HTTP Methods
- GET: app.MapGet("/users", () => GetUsers())
- POST: app.MapPost("/users", (User user) => CreateUser(user))
- PUT: app.MapPut("/users/{id}", (int id, User user) => UpdateUser(id, user))
- DELETE: app.MapDelete("/users/{id}", (int id) => DeleteUser(id))
EOF

print_success "Test documentation created"

# Run basic functionality tests
print_status "Running basic functionality tests..."

# Test 1: Build and run a simple RAG test
print_status "Test 1: Basic RAG service functionality"
dotnet test tests/Nexo.Feature.AI.Tests/RAG/DocumentationRAGServiceTests.cs \
    --configuration Release \
    --verbosity minimal \
    --logger "console;verbosity=minimal" \
    --filter "QueryDocumentationAsync_WithValidQuery_ReturnsRAGResponse"

if [ $? -eq 0 ]; then
    print_success "Test 1 passed: Basic RAG service functionality"
else
    print_error "Test 1 failed: Basic RAG service functionality"
    exit 1
fi

# Test 2: Vector store functionality
print_status "Test 2: Vector store functionality"
dotnet test tests/Nexo.Feature.AI.Tests/RAG/DocumentationVectorStoreTests.cs \
    --configuration Release \
    --verbosity minimal \
    --logger "console;verbosity=minimal" \
    --filter "StoreAsync_WithValidChunk_StoresSuccessfully"

if [ $? -eq 0 ]; then
    print_success "Test 2 passed: Vector store functionality"
else
    print_error "Test 2 failed: Vector store functionality"
    exit 1
fi

# Test 3: Embedding service functionality
print_status "Test 3: Embedding service functionality"
dotnet test tests/Nexo.Feature.AI.Tests/RAG/DocumentationEmbeddingServiceTests.cs \
    --configuration Release \
    --verbosity minimal \
    --logger "console;verbosity=minimal" \
    --filter "GenerateEmbeddingAsync_WithValidText_ReturnsEmbedding"

if [ $? -eq 0 ]; then
    print_success "Test 3 passed: Embedding service functionality"
else
    print_error "Test 3 failed: Embedding service functionality"
    exit 1
fi

# Test 4: Integration test
print_status "Test 4: RAG integration functionality"
dotnet test tests/Nexo.Feature.AI.Tests/RAG/RAGIntegrationTests.cs \
    --configuration Release \
    --verbosity minimal \
    --logger "console;verbosity=minimal" \
    --filter "RAGSystem_EndToEnd_WorksCorrectly"

if [ $? -eq 0 ]; then
    print_success "Test 4 passed: RAG integration functionality"
else
    print_error "Test 4 failed: RAG integration functionality"
    exit 1
fi

# Test 5: Agent integration
print_status "Test 5: RAG-enhanced agent functionality"
dotnet test tests/Nexo.Feature.Agent.Tests/RAG/RAGEnhancedDeveloperAgentTests.cs \
    --configuration Release \
    --verbosity minimal \
    --logger "console;verbosity=minimal" \
    --filter "ProcessRequestAsync_WithCSharpQuery_ShouldUseRAG"

if [ $? -eq 0 ]; then
    print_success "Test 5 passed: RAG-enhanced agent functionality"
else
    print_error "Test 5 failed: RAG-enhanced agent functionality"
    exit 1
fi

# Test 6: Performance test
print_status "Test 6: RAG performance functionality"
dotnet test tests/Nexo.Feature.AI.Tests/RAG/RAGPerformanceTests.cs \
    --configuration Release \
    --verbosity minimal \
    --logger "console;verbosity=minimal" \
    --filter "RAGSystem_QueryPerformance_PerformsWithinTimeLimit"

if [ $? -eq 0 ]; then
    print_success "Test 6 passed: RAG performance functionality"
else
    print_error "Test 6 failed: RAG performance functionality"
    exit 1
fi

# Test 7: Documentation ingestion
print_status "Test 7: Documentation ingestion functionality"
dotnet test tests/Nexo.Feature.AI.Tests/RAG/DocumentationIngestionServiceTests.cs \
    --configuration Release \
    --verbosity minimal \
    --logger "console;verbosity=minimal" \
    --filter "IngestCustomDocumentationAsync_WithValidFile_ProcessesSuccessfully"

if [ $? -eq 0 ]; then
    print_success "Test 7 passed: Documentation ingestion functionality"
else
    print_error "Test 7 failed: Documentation ingestion functionality"
    exit 1
fi

# Run all RAG tests to ensure everything works together
print_status "Running comprehensive RAG test suite..."
dotnet test tests/Nexo.Feature.AI.Tests/RAG/ \
    --configuration Release \
    --verbosity normal \
    --logger "console;verbosity=normal"

if [ $? -eq 0 ]; then
    print_success "All RAG tests passed"
else
    print_error "Some RAG tests failed"
    exit 1
fi

# Clean up test documentation
print_status "Cleaning up test documentation..."
rm -rf ./test-docs

print_success "RAG functionality validation completed successfully!"
echo ""
echo "📊 Validation Summary:"
echo "- Basic RAG service: ✅ Working"
echo "- Vector store: ✅ Working"
echo "- Embedding service: ✅ Working"
echo "- Integration: ✅ Working"
echo "- Agent enhancement: ✅ Working"
echo "- Performance: ✅ Working"
echo "- Documentation ingestion: ✅ Working"
echo ""
echo "🎉 RAG system is fully functional and ready for use!"
