#!/bin/bash
# Script to test code analysis functionality in Docker Unity environment
# This validates that code analysis works in .NET Standard 2.0 (Unity-compatible) environment

set -euo pipefail

PROJECT_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$PROJECT_ROOT"

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo -e "${YELLOW}Code Analysis Tests - Docker Unity Environment${NC}"
echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo ""

# Check if Docker is running
if ! docker info > /dev/null 2>&1; then
    echo -e "${RED}❌ Docker is not running${NC}"
    echo "   Please start Docker Desktop and try again"
    exit 1
fi

echo -e "${YELLOW}Building Unity Docker test image...${NC}"
docker build -f .docker/Dockerfile.test-framework-unity -t nexo-unity-code-analysis-test:latest . || {
    echo -e "${RED}❌ Failed to build Docker image${NC}"
    exit 1
}

echo ""
echo -e "${GREEN}✅ Docker image built successfully${NC}"
echo ""

# Create test results directory
mkdir -p test-results/unity-code-analysis

echo -e "${YELLOW}Running code analysis tests in Unity Docker environment...${NC}"
echo ""

# Run tests in container
docker run --rm \
    -v "$PROJECT_ROOT/test-results/unity-code-analysis:/workspace/test-results" \
    nexo-unity-code-analysis-test:latest \
    bash -c "
        echo '🧪 Testing Code Analysis in Unity Environment (.NET Standard 2.0)'
        echo ''
        echo '📋 Platform Information:'
        dotnet --version
        echo ''
        echo '🧪 Running Code Analysis Platform Compatibility Tests...'
        dotnet test src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj \
            --filter 'FullyQualifiedName~CodeAnalysisPlatformCompatibilityTests' \
            --logger 'console;verbosity=normal' \
            --logger 'trx;LogFileName=unity-code-analysis-platform.trx' \
            --results-directory test-results || true
        echo ''
        echo '🧪 Running Code Analysis Portability Tests...'
        dotnet test src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj \
            --filter 'FullyQualifiedName~CodeAnalysisPortabilityTests' \
            --logger 'console;verbosity=normal' \
            --logger 'trx;LogFileName=unity-code-analysis-portability.trx' \
            --results-directory test-results || true
        echo ''
        echo '🧪 Running Code Analysis Smoke Tests...'
        dotnet test src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj \
            --filter 'FullyQualifiedName~CodeAnalysisSmokeTests' \
            --logger 'console;verbosity=normal' \
            --logger 'trx;LogFileName=unity-code-analysis-smoke.trx' \
            --results-directory test-results || true
        echo ''
        echo '✅ Code analysis tests completed in Unity environment'
    " 2>&1 | tee test-results/unity-code-analysis/unity-docker-test.log

echo ""
echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo -e "${YELLOW}Test Results Summary${NC}"
echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"

# Check for test result files
if [ -f "test-results/unity-code-analysis/unity-code-analysis-platform.trx" ]; then
    echo -e "${GREEN}✅ Platform Compatibility Tests: Results available${NC}"
fi

if [ -f "test-results/unity-code-analysis/unity-code-analysis-portability.trx" ]; then
    echo -e "${GREEN}✅ Portability Tests: Results available${NC}"
fi

if [ -f "test-results/unity-code-analysis/unity-code-analysis-smoke.trx" ]; then
    echo -e "${GREEN}✅ Smoke Tests: Results available${NC}"
fi

# Extract pass/fail counts from log
if [ -f "test-results/unity-code-analysis/unity-docker-test.log" ]; then
    passed=$(grep -oE '[0-9]+(?=\s+Passed!)' "test-results/unity-code-analysis/unity-docker-test.log" 2>/dev/null | tail -1 || echo "0")
    failed=$(grep -oE '[0-9]+(?=\s+Failed!)' "test-results/unity-code-analysis/unity-docker-test.log" 2>/dev/null | tail -1 || echo "0")
    total=$(grep -oE '[0-9]+(?=\s+Total)' "test-results/unity-code-analysis/unity-docker-test.log" 2>/dev/null | tail -1 || echo "0")
    
    if [ -n "$total" ] && [ "$total" != "0" ]; then
        echo ""
        echo -e "${GREEN}Test Results: ${total} total, ${passed} passed, ${failed} failed${NC}"
    fi
fi

echo ""
echo -e "${BLUE}Test logs available in: test-results/unity-code-analysis/${NC}"
echo -e "${GREEN}✅ Code analysis Unity Docker tests completed${NC}"
