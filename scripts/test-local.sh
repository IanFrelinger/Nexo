#!/bin/bash
# Script to run Nexo CLI unit tests locally using Docker
# This mirrors the GitHub Actions test matrix

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

cd "$PROJECT_ROOT"

echo "🧪 Running Nexo CLI Unit Tests Locally with Docker"
echo "=================================================="
echo ""

# Create test results directory
mkdir -p test-results

# Colors for output
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Function to run tests in Docker
run_tests() {
    local platform=$1
    local dotnet_version=${2:-8.0}
    
    echo -e "${YELLOW}Testing on $platform with .NET $dotnet_version...${NC}"
    
    # Build Docker image
    docker build -f .docker/Dockerfile.test -t nexo-test:$platform .
    
    # Run tests
    docker run --rm \
        -v "$PROJECT_ROOT/test-results:/workspace/test-results" \
        -e DOTNET_VERSION=$dotnet_version \
        nexo-test:$platform \
        bash -c "
            cd src/Nexo.CLI &&
            dotnet run --project Nexo.CLI.csproj -- test --format-json > /workspace/test-results/${platform}-results.json 2>/workspace/test-results/${platform}-logs.txt || true &&
            python3 << 'PYEOF'
import json
import sys
import re
try:
    with open('/workspace/test-results/${platform}-results.json', 'r') as f:
        content = f.read()
    matches = re.findall(r'\{[^{}]*(?:\{[^{}]*\}[^{}]*)*\}', content, re.DOTALL)
    for match in reversed(matches):
        try:
            data = json.loads(match)
            if 'TotalTests' in data:
                with open('/workspace/test-results/${platform}-results.json', 'w') as out:
                    json.dump(data, out, indent=2)
                total = data.get('TotalTests', 0)
                passed = data.get('PassedTests', 0)
                failed = data.get('FailedTests', 0)
                print(f'✅ ${platform}: {total} total, {passed} passed, {failed} failed')
                sys.exit(0 if failed == 0 else 1)
        except:
            continue
    print('❌ No valid test results found')
    sys.exit(1)
except Exception as e:
    print(f'Error: {e}')
    sys.exit(1)
PYEOF
        "
    
    return $?
}

# Parse arguments
PLATFORMS=("ubuntu")
ALL_PLATFORMS=false
QUICK=false

while [[ $# -gt 0 ]]; do
    case $1 in
        --all)
            ALL_PLATFORMS=true
            PLATFORMS=("ubuntu")
            shift
            ;;
        --quick)
            QUICK=true
            shift
            ;;
        --platform)
            PLATFORMS=("$2")
            shift 2
            ;;
        *)
            echo "Unknown option: $1"
            echo "Usage: $0 [--all] [--quick] [--platform ubuntu]"
            exit 1
            ;;
    esac
done

# Run tests
FAILED_PLATFORMS=()
PASSED_PLATFORMS=()

for platform in "${PLATFORMS[@]}"; do
    if run_tests "$platform" "8.0"; then
        PASSED_PLATFORMS+=("$platform")
    else
        FAILED_PLATFORMS+=("$platform")
    fi
    echo ""
done

# Summary
echo "=================================================="
echo "📊 Test Summary"
echo "=================================================="

if [ ${#PASSED_PLATFORMS[@]} -gt 0 ]; then
    echo -e "${GREEN}✅ Passed platforms:${NC}"
    for platform in "${PASSED_PLATFORMS[@]}"; do
        echo -e "   ${GREEN}✓${NC} $platform"
    done
fi

if [ ${#FAILED_PLATFORMS[@]} -gt 0 ]; then
    echo -e "${RED}❌ Failed platforms:${NC}"
    for platform in "${FAILED_PLATFORMS[@]}"; do
        echo -e "   ${RED}✗${NC} $platform"
        echo "   Check test-results/${platform}-logs.txt for details"
    done
    exit 1
fi

echo ""
echo -e "${GREEN}✅ All tests passed on all platforms!${NC}"
echo ""
echo "Test results are available in: test-results/"

