#!/bin/bash
# Script to run geospatial caching tests in Unity environment
# Note: Requires Unity installation and Unity project setup

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

cd "$PROJECT_ROOT"

echo "🎮 Running Caching Tests in Unity Environment"
echo "==========================================="
echo ""

# Configuration
UNITY_BIN="${UNITY_BIN:-}"
UNITY_PROJECT="${UNITY_PROJECT:-}"

# Try to find Unity automatically
if [[ -z "$UNITY_BIN" ]]; then
    if [[ "$OSTYPE" == "darwin"* ]]; then
        # macOS - check Unity Hub
        HUB_PATH="/Applications/Unity/Hub/Editor"
        if [ -d "$HUB_PATH" ]; then
            LATEST_VERSION=$(ls -1 "$HUB_PATH" | sort -V | tail -1)
            UNITY_BIN="$HUB_PATH/$LATEST_VERSION/Unity.app/Contents/MacOS/Unity"
        fi
    elif [[ "$OSTYPE" == "linux-gnu"* ]]; then
        # Linux - check common locations
        if [ -f "/opt/unity/Editor/Unity" ]; then
            UNITY_BIN="/opt/unity/Editor/Unity"
        fi
    elif [[ "$OSTYPE" == "msys" ]] || [[ "$OSTYPE" == "win32" ]]; then
        # Windows - check Program Files
        if [ -d "/c/Program Files/Unity/Hub/Editor" ]; then
            LATEST_VERSION=$(ls -1 "/c/Program Files/Unity/Hub/Editor" | sort -V | tail -1)
            UNITY_BIN="/c/Program Files/Unity/Hub/Editor/$LATEST_VERSION/Editor/Unity.exe"
        fi
    fi
fi

# Check if Unity is available
if [[ -z "$UNITY_BIN" ]] || [[ ! -f "$UNITY_BIN" ]]; then
    echo "⚠️  Unity not found. Skipping Unity-specific tests."
    echo "   Set UNITY_BIN environment variable to Unity executable path"
    echo "   Example: export UNITY_BIN=/Applications/Unity/Hub/Editor/2022.3.0f1/Unity.app/Contents/MacOS/Unity"
    echo ""
    echo "   Running .NET tests only (Unity integration tests will be skipped)..."
    echo ""
    
    # Fall back to running .NET tests only
    UNITY_AVAILABLE=false
else
    echo "✅ Found Unity at: $UNITY_BIN"
    UNITY_AVAILABLE=true
fi

# Create test results directory
mkdir -p test-results/caching

# Colors for output
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

FAILED=0

# Always run .NET caching tests
echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo -e "${YELLOW}Running .NET Caching Tests (Unity-compatible)...${NC}"
echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo ""

# Run unit tests
echo -e "${YELLOW}Running Unit Tests...${NC}"
if dotnet test src/Nexo.Tests.GeospatialUnit/Nexo.Tests.GeospatialUnit.csproj \
    --filter "FullyQualifiedName~CachingTests" \
    --logger "console;verbosity=minimal" \
    --logger "trx;LogFileName=unity-unit.trx" \
    --results-directory test-results/caching \
    > test-results/caching/unity-unit.log 2>&1; then
    echo -e "${GREEN}✅ Unit tests passed${NC}"
else
    echo -e "${RED}❌ Unit tests failed${NC}"
    FAILED=1
fi

# Run E2E tests
echo -e "${YELLOW}Running E2E Tests...${NC}"
if dotnet test src/Nexo.Tests.GeospatialE2E/Nexo.Tests.GeospatialE2E.csproj \
    --filter "FullyQualifiedName~CachingSmokeTests" \
    --logger "console;verbosity=minimal" \
    --logger "trx;LogFileName=unity-e2e.trx" \
    --results-directory test-results/caching \
    > test-results/caching/unity-e2e.log 2>&1; then
    echo -e "${GREEN}✅ E2E tests passed${NC}"
else
    echo -e "${RED}❌ E2E tests failed${NC}"
    FAILED=1
fi

# Run infrastructure tests
echo -e "${YELLOW}Running Infrastructure Tests...${NC}"
if dotnet test src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj \
    --filter "FullyQualifiedName~Caching" \
    --logger "console;verbosity=minimal" \
    --logger "trx;LogFileName=unity-infra.trx" \
    --results-directory test-results/caching \
    > test-results/caching/unity-infra.log 2>&1; then
    echo -e "${GREEN}✅ Infrastructure tests passed${NC}"
else
    echo -e "${RED}❌ Infrastructure tests failed${NC}"
    FAILED=1
fi

# Run Unity-specific tests if Unity is available
if [ "$UNITY_AVAILABLE" = true ]; then
    echo ""
    echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
    echo -e "${YELLOW}Running Unity Integration Tests...${NC}"
    echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
    echo ""
    
    # Check if Unity project exists
    if [[ -z "$UNITY_PROJECT" ]]; then
        # Try to find Unity project
        if [ -d "DirectorStudioUnity" ]; then
            UNITY_PROJECT="DirectorStudioUnity"
        elif [ -d "unity-project" ]; then
            UNITY_PROJECT="unity-project"
        fi
    fi
    
    if [[ -n "$UNITY_PROJECT" ]] && [[ -d "$UNITY_PROJECT" ]]; then
        echo -e "${YELLOW}Running Unity tests in project: $UNITY_PROJECT${NC}"
        
        UNITY_RESULTS_XML="test-results/caching/unity-playmode-results.xml"
        UNITY_LOG="test-results/caching/unity-playmode.log"
        
        # Run Unity PlayMode tests
        "$UNITY_BIN" -batchmode -nographics \
            -projectPath "$UNITY_PROJECT" \
            -runTests -testPlatform PlayMode \
            -testResults "$UNITY_RESULTS_XML" \
            -logFile "$UNITY_LOG" \
            -quit
        
        UNITY_EXIT=$?
        
        if [ $UNITY_EXIT -eq 0 ] && [ -f "$UNITY_RESULTS_XML" ]; then
            echo -e "${GREEN}✅ Unity PlayMode tests completed${NC}"
        else
            echo -e "${YELLOW}⚠️  Unity PlayMode tests may have issues (check logs)${NC}"
            if [ -f "$UNITY_LOG" ]; then
                echo "   Log: $UNITY_LOG"
            fi
        fi
    else
        echo -e "${YELLOW}⚠️  Unity project not found. Skipping Unity-specific tests.${NC}"
        echo "   Set UNITY_PROJECT environment variable to Unity project path"
    fi
fi

echo ""
echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
if [ $FAILED -eq 0 ]; then
    echo -e "${GREEN}✅ All Unity caching tests passed!${NC}"
else
    echo -e "${RED}❌ Some Unity caching tests failed${NC}"
    echo "   Check test-results/caching/unity-*.log for details"
fi
echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo ""

exit $FAILED
