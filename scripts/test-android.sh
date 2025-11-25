#!/bin/bash
# Script to run Nexo CLI unit tests on Android (using Docker)

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

cd "$PROJECT_ROOT"

echo "🤖 Running Nexo CLI Unit Tests on Android"
echo "=========================================="
echo ""

# Create test results directory
mkdir -p test-results

# Colors for output
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${YELLOW}Building Android test Docker image...${NC}"

# Build Docker image
docker build -f .docker/Dockerfile.test-android -t nexo-test:android .

echo -e "${YELLOW}Running tests in Android container...${NC}"

# Run tests
docker run --rm \
    -v "$PROJECT_ROOT/test-results:/workspace/test-results" \
    -e ANDROID_HOME=/opt/android-sdk \
    nexo-test:android \
    bash -c "
        cd src/Nexo.CLI &&
        dotnet run --project Nexo.CLI.csproj -- test --format-json > /workspace/test-results/android-output.txt 2>/workspace/test-results/android-logs.txt || true &&
        python3 << 'PYEOF'
import json
import sys
import re
try:
    with open('/workspace/test-results/android-output.txt', 'r') as f:
        content = f.read()
    matches = re.findall(r'\{[^{}]*(?:\{[^{}]*\}[^{}]*)*\}', content, re.DOTALL)
    for match in reversed(matches):
        try:
            data = json.loads(match)
            if 'TotalTests' in data:
                with open('/workspace/test-results/android-results.json', 'w') as out:
                    json.dump(data, out, indent=2)
                total = data.get('TotalTests', 0)
                passed = data.get('PassedTests', 0)
                failed = data.get('FailedTests', 0)
                print(f'✅ Android: {total} total, {passed} passed, {failed} failed')
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

EXIT_CODE=$?

echo ""
echo "=========================================="
if [ $EXIT_CODE -eq 0 ]; then
    echo -e "${GREEN}✅ Android tests passed!${NC}"
else
    echo -e "${RED}❌ Android tests failed${NC}"
    echo "Check test-results/android-logs.txt for details"
fi
echo ""

exit $EXIT_CODE

