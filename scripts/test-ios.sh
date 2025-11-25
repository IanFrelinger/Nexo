#!/bin/bash
# Script to run Nexo CLI unit tests on iOS (requires macOS with Xcode)

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

cd "$PROJECT_ROOT"

echo "🍎 Running Nexo CLI Unit Tests on iOS"
echo "======================================="
echo ""

# Check if running on macOS
if [[ "$OSTYPE" != "darwin"* ]]; then
    echo "❌ Error: iOS testing requires macOS with Xcode"
    echo "   This script must be run on a Mac"
    exit 1
fi

# Check for Xcode
if ! command -v xcodebuild &> /dev/null; then
    echo "❌ Error: Xcode is not installed"
    echo "   Please install Xcode from the App Store"
    exit 1
fi

# Check for .NET
if ! command -v dotnet &> /dev/null; then
    echo "❌ Error: .NET SDK is not installed"
    echo "   Please install .NET 8.0 SDK"
    exit 1
fi

echo "✅ Environment checks passed"
echo ""

# Create test results directory
mkdir -p test-results

# Colors for output
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${YELLOW}Running tests on iOS (macOS)...${NC}"

# Run tests
cd src/Nexo.CLI
dotnet run --project Nexo.CLI.csproj -- test --format-json > ../../test-results/ios-results.json 2>../../test-results/ios-logs.txt
TEST_EXIT=$?

# Extract and display results
if [ -f ../../test-results/ios-results.json ] && [ -s ../../test-results/ios-results.json ]; then
    python3 << 'PYEOF'
import json
import sys
import re

try:
    with open('../../test-results/ios-results.json', 'r') as f:
        content = f.read()
    
    # Find JSON object
    matches = re.findall(r'\{[^{}]*(?:\{[^{}]*\}[^{}]*)*\}', content, re.DOTALL)
    for match in reversed(matches):
        try:
            data = json.loads(match)
            if 'TotalTests' in data:
                with open('../../test-results/ios-results.json', 'w') as out:
                    json.dump(data, out, indent=2)
                total = data.get('TotalTests', 0)
                passed = data.get('PassedTests', 0)
                failed = data.get('FailedTests', 0)
                print(f'✅ iOS: {total} total, {passed} passed, {failed} failed')
                sys.exit(0 if failed == 0 else 1)
        except:
            continue
    
    print('❌ No valid JSON found')
    sys.exit(1)
except Exception as e:
    print(f'Error: {e}')
    sys.exit(1)
PYEOF
    EXIT_CODE=$?
else
    echo "⚠️ No test results JSON found, checking logs..."
    tail -20 ../../test-results/ios-logs.txt
    EXIT_CODE=1
fi

cd "$PROJECT_ROOT"

echo ""
echo "======================================="
if [ $EXIT_CODE -eq 0 ]; then
    echo -e "${GREEN}✅ iOS tests passed!${NC}"
else
    echo -e "${RED}❌ iOS tests failed${NC}"
fi
echo ""

exit $EXIT_CODE

