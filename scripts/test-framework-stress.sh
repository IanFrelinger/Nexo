#!/bin/bash
# Script to run stress tests on Nexo framework components
# Tests system under load, concurrent operations, and resource limits

set -euo pipefail

PROJECT_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$PROJECT_ROOT"

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Stress test parameters
CONCURRENT_OPERATIONS=${CONCURRENT_OPERATIONS:-100}
ITERATIONS=${ITERATIONS:-1000}
TIMEOUT_SECONDS=${TIMEOUT_SECONDS:-300}

echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo -e "${YELLOW}Nexo Framework Stress Testing${NC}"
echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo ""
echo -e "Concurrent Operations: ${CONCURRENT_OPERATIONS}"
echo -e "Iterations: ${ITERATIONS}"
echo -e "Timeout: ${TIMEOUT_SECONDS}s"
echo ""

# Create stress test output directory
STRESS_DIR="test-results/stress"
mkdir -p "$STRESS_DIR"

FAILED=0

# Run stress tests
echo -e "${YELLOW}Running stress tests...${NC}"

# Test 1: Orchestration under load
echo -e "${YELLOW}Test 1: Orchestration Concurrent Execution${NC}"
if dotnet test "src/Nexo.Tests.Orchestration/Nexo.Tests.Orchestration.csproj" \
    --filter "FullyQualifiedName~StressTests" \
    --logger "console;verbosity=minimal" \
    --logger "trx;LogFileName=orchestration-stress.trx" \
    --results-directory "$STRESS_DIR" \
    > "$STRESS_DIR/orchestration-stress.log" 2>&1; then
    echo -e "${GREEN}✅ Orchestration stress tests passed${NC}"
else
    echo -e "${RED}❌ Orchestration stress tests failed${NC}"
    FAILED=1
fi

# Test 2: Command execution under load
echo -e "${YELLOW}Test 2: Command Execution Under Load${NC}"
if dotnet test "src/Nexo.Tests.Application/Nexo.Tests.Application.csproj" \
    --filter "FullyQualifiedName~StressTests" \
    --logger "console;verbosity=minimal" \
    --logger "trx;LogFileName=application-stress.trx" \
    --results-directory "$STRESS_DIR" \
    > "$STRESS_DIR/application-stress.log" 2>&1; then
    echo -e "${GREEN}✅ Application stress tests passed${NC}"
else
    echo -e "${RED}❌ Application stress tests failed${NC}"
    FAILED=1
fi

# Test 3: Infrastructure resource limits
echo -e "${YELLOW}Test 3: Infrastructure Resource Limits${NC}"
if dotnet test "src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj" \
    --filter "FullyQualifiedName~StressTests" \
    --logger "console;verbosity=minimal" \
    --logger "trx;LogFileName=infrastructure-stress.trx" \
    --results-directory "$STRESS_DIR" \
    > "$STRESS_DIR/infrastructure-stress.log" 2>&1; then
    echo -e "${GREEN}✅ Infrastructure stress tests passed${NC}"
else
    echo -e "${RED}❌ Infrastructure stress tests failed${NC}"
    FAILED=1
fi

# Test 4: Agent lifecycle under load
echo -e "${YELLOW}Test 4: Agent Lifecycle Under Load${NC}"
if dotnet test "src/Nexo.Tests.Orchestration/Nexo.Tests.Orchestration.csproj" \
    --filter "FullyQualifiedName~AgentStressTests" \
    --logger "console;verbosity=minimal" \
    --logger "trx;LogFileName=agent-stress.trx" \
    --results-directory "$STRESS_DIR" \
    > "$STRESS_DIR/agent-stress.log" 2>&1; then
    echo -e "${GREEN}✅ Agent stress tests passed${NC}"
else
    echo -e "${RED}❌ Agent stress tests failed${NC}"
    FAILED=1
fi

echo ""
if [ $FAILED -eq 0 ]; then
    echo -e "${GREEN}✅ All stress tests passed${NC}"
    exit 0
else
    echo -e "${RED}❌ Some stress tests failed${NC}"
    exit 1
fi
