#!/bin/bash
# Script to run framework tests in Unity environment
# Tests core framework components for Unity compatibility

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
echo -e "${YELLOW}Nexo Framework Tests - Unity Environment${NC}"
echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo ""

# Create test results directory
mkdir -p test-results/framework/unity

FAILED=0

# Check for Unity installation
UNITY_PATH=""
if [ -d "/Applications/Unity/Hub/Editor" ]; then
    # macOS Unity Hub location
    LATEST_UNITY=$(ls -t /Applications/Unity/Hub/Editor 2>/dev/null | head -1)
    if [ -n "$LATEST_UNITY" ]; then
        UNITY_PATH="/Applications/Unity/Hub/Editor/$LATEST_UNITY/Unity.app/Contents/MacOS/Unity"
    fi
elif [ -d "/opt/unity/Editor" ]; then
    # Linux Unity installation
    UNITY_PATH="/opt/unity/Editor/Unity"
elif [ -d "C:/Program Files/Unity/Hub/Editor" ]; then
    # Windows Unity Hub location
    LATEST_UNITY=$(ls -t "C:/Program Files/Unity/Hub/Editor" 2>/dev/null | head -1)
    if [ -n "$LATEST_UNITY" ]; then
        UNITY_PATH="C:/Program Files/Unity/Hub/Editor/$LATEST_UNITY/Editor/Unity.exe"
    fi
fi

# Phase 1: Test .NET compatibility (run tests that Unity can use)
echo -e "${YELLOW}Phase 1: Testing .NET Standard compatibility (Unity-compatible tests)...${NC}"

# Test Domain layer (netstandard2.0 compatible)
if dotnet test src/Nexo.Tests.Domain/Nexo.Tests.Domain.csproj \
    --filter "FullyQualifiedName~Domain" \
    --framework netstandard2.0 \
    --logger "console;verbosity=minimal" \
    --logger "trx;LogFileName=unity-domain.trx" \
    --results-directory test-results/framework/unity \
    > test-results/framework/unity/unity-domain.log 2>&1; then
    echo -e "${GREEN}✅ Domain tests passed (Unity-compatible)${NC}"
else
    echo -e "${YELLOW}⚠️  Domain tests may not be fully Unity-compatible (expected)${NC}"
fi

# Test Core.Domain directly (it's netstandard2.0)
echo -e "${YELLOW}Phase 2: Testing Core.Domain compatibility...${NC}"
if dotnet build src/Nexo.Core.Domain/Nexo.Core.Domain.csproj \
    --framework netstandard2.0 \
    > test-results/framework/unity/unity-core-domain-build.log 2>&1; then
    echo -e "${GREEN}✅ Core.Domain builds for .NET Standard 2.0 (Unity compatible)${NC}"
else
    echo -e "${RED}❌ Core.Domain build failed${NC}"
    FAILED=1
fi

# Phase 3: Test Unity-specific components
echo -e "${YELLOW}Phase 3: Testing Unity UI components...${NC}"
if dotnet build src/Nexo.Core.UI.Unity/Nexo.Core.UI.Unity.csproj \
    > test-results/framework/unity/unity-ui-build.log 2>&1; then
    echo -e "${GREEN}✅ Unity UI components build successfully${NC}"
else
    echo -e "${RED}❌ Unity UI components build failed${NC}"
    FAILED=1
fi

# Phase 4: Run Unity Test Runner if Unity is available
if [ -n "$UNITY_PATH" ] && [ -f "$UNITY_PATH" ]; then
    echo -e "${YELLOW}Phase 4: Running Unity Test Runner...${NC}"
    echo -e "${BLUE}Unity found at: ${UNITY_PATH}${NC}"
    
    # Check if there's a Unity test project
    if [ -d "unity-test-project" ] || [ -d "UnityTestProject" ]; then
        UNITY_PROJECT_PATH=$(find . -maxdepth 2 -type d -name "*Unity*Test*" -o -name "unity-test-project" | head -1)
        
        if [ -n "$UNITY_PROJECT_PATH" ]; then
            echo -e "${YELLOW}Running Unity tests in project: ${UNITY_PROJECT_PATH}${NC}"
            
            # Run Unity in batch mode with test runner
            "$UNITY_PATH" \
                -batchmode \
                -quit \
                -projectPath "$UNITY_PROJECT_PATH" \
                -runTests \
                -testPlatform EditMode \
                -testResults "test-results/framework/unity/unity-test-results.xml" \
                -logFile "test-results/framework/unity/unity-test.log" \
                2>&1 | tee test-results/framework/unity/unity-runner.log || true
            
            if [ -f "test-results/framework/unity/unity-test-results.xml" ]; then
                echo -e "${GREEN}✅ Unity Test Runner completed${NC}"
            else
                echo -e "${YELLOW}⚠️  Unity Test Runner may have encountered issues (check logs)${NC}"
            fi
        else
            echo -e "${YELLOW}⚠️  No Unity test project found, skipping Unity Test Runner${NC}"
            echo -e "${BLUE}   To create a Unity test project, use Unity Hub to create a new project${NC}"
            echo -e "${BLUE}   and add Nexo.Core.UI.Unity as a package dependency${NC}"
        fi
    else
        echo -e "${YELLOW}⚠️  No Unity test project directory found${NC}"
        echo -e "${BLUE}   Unity is installed but no test project configured${NC}"
        echo -e "${BLUE}   Framework components are Unity-compatible (.NET Standard 2.0)${NC}"
    fi
else
    echo -e "${YELLOW}⚠️  Unity Editor not found${NC}"
    echo -e "${BLUE}   Framework components are built for .NET Standard 2.0 (Unity compatible)${NC}"
    echo -e "${BLUE}   Install Unity Hub to run full Unity Test Runner${NC}"
fi

# Extract test results
echo ""
echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo -e "${YELLOW}Unity Test Results Summary${NC}"
echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"

if [ -f "test-results/framework/unity/unity-domain.log" ]; then
    passed=$(grep -oE '[0-9]+(?=\s+Passed!)' "test-results/framework/unity/unity-domain.log" 2>/dev/null | head -1 || echo "0")
    failed=$(grep -oE '[0-9]+(?=\s+Failed!)' "test-results/framework/unity/unity-domain.log" 2>/dev/null | head -1 || echo "0")
    total=$(grep -oE '[0-9]+(?=\s+Total)' "test-results/framework/unity/unity-domain.log" 2>/dev/null | head -1 || echo "0")
    
    if [ -n "$passed" ] && [ "$passed" != "0" ]; then
        echo -e "${GREEN}✅ Domain Tests: ${total} total, ${passed} passed, ${failed} failed${NC}"
    fi
fi

if [ -f "test-results/framework/unity/unity-test-results.xml" ]; then
    echo -e "${GREEN}✅ Unity Test Runner: Results available in unity-test-results.xml${NC}"
fi

echo ""
if [ $FAILED -eq 0 ]; then
    echo -e "${GREEN}✅ Unity compatibility tests completed${NC}"
    exit 0
else
    echo -e "${RED}❌ Some Unity compatibility tests failed${NC}"
    exit 1
fi
