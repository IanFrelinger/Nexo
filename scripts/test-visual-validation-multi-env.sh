#!/bin/bash
# Multi-platform visual validation test runner
# Runs visual validation tests across all target platforms using synthetic data

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
cd "$PROJECT_ROOT"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Test results directory
RESULTS_DIR="$PROJECT_ROOT/test-results/visual-validation"
mkdir -p "$RESULTS_DIR"

# Platforms to test
PLATFORMS=(
    "ubuntu-8.0:.docker/Dockerfile.test-visual-validation"
    "alpine-8.0:.docker/Dockerfile.test-visual-validation-alpine"
    "debian-8.0:.docker/Dockerfile.test-visual-validation-debian"
)

# Function to run tests on a platform
run_platform_tests() {
    local platform=$1
    local dockerfile=$2
    local image_name="nexo-visual-test:${platform}"
    
    echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
    echo -e "${BLUE}🎨 Testing Visual Validation on ${platform}${NC}"
    echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
    
    # Build Docker image
    echo -e "${YELLOW}Building Docker image for ${platform}...${NC}"
    if ! docker build -f "$dockerfile" \
        --build-arg DOTNET_VERSION=8.0 \
        -t "$image_name" \
        . > "$RESULTS_DIR/${platform}-build.log" 2>&1; then
        echo -e "${RED}❌ Failed to build image for ${platform}${NC}"
        return 1
    fi
    
    # Run tests
    echo -e "${YELLOW}Running visual validation tests...${NC}"
    local result_file="$RESULTS_DIR/${platform}-results.txt"
    local exit_code=0
    
    if docker run --rm \
        -v "$RESULTS_DIR:/workspace/test-results" \
        --shm-size=2gb \
        "$image_name" > "$result_file" 2>&1; then
        echo -e "${GREEN}✅ ${platform} tests passed${NC}"
    else
        echo -e "${RED}❌ ${platform} tests failed${NC}"
        exit_code=1
    fi
    
    # Show summary
    if grep -q "✅ Visual validation tests completed" "$result_file"; then
        echo -e "${GREEN}   All visual validation tests passed on ${platform}${NC}"
    else
        echo -e "${RED}   Some tests failed on ${platform}${NC}"
        echo -e "${YELLOW}   See ${result_file} for details${NC}"
    fi
    
    return $exit_code
}

# Function to run all platforms
run_all_platforms() {
    local failed_platforms=()
    local passed_platforms=()
    
    for platform_config in "${PLATFORMS[@]}"; do
        IFS=':' read -r platform dockerfile <<< "$platform_config"
        
        if run_platform_tests "$platform" "$dockerfile"; then
            passed_platforms+=("$platform")
        else
            failed_platforms+=("$platform")
        fi
        
        echo ""
    done
    
    # Summary
    echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
    echo -e "${BLUE}📊 Visual Validation Test Summary${NC}"
    echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
    
    if [ ${#passed_platforms[@]} -gt 0 ]; then
        echo -e "${GREEN}✅ Passed (${#passed_platforms[@]}):${NC}"
        for p in "${passed_platforms[@]}"; do
            echo -e "   ${GREEN}✓${NC} $p"
        done
    fi
    
    if [ ${#failed_platforms[@]} -gt 0 ]; then
        echo -e "${RED}❌ Failed (${#failed_platforms[@]}):${NC}"
        for p in "${failed_platforms[@]}"; do
            echo -e "   ${RED}✗${NC} $p"
        done
        return 1
    fi
    
    echo -e "${GREEN}✅ All platforms passed visual validation tests!${NC}"
    return 0
}

# Main execution
if [ "$1" == "--all" ]; then
    run_all_platforms
elif [ "$1" == "--platform" ] && [ -n "$2" ]; then
    # Run specific platform
    platform=$2
    found=false
    for platform_config in "${PLATFORMS[@]}"; do
        IFS=':' read -r p dockerfile <<< "$platform_config"
        if [ "$p" == "$platform" ]; then
            run_platform_tests "$platform" "$dockerfile"
            found=true
            break
        fi
    done
    if [ "$found" == false ]; then
        echo -e "${RED}Unknown platform: $platform${NC}"
        echo -e "${YELLOW}Available platforms:${NC}"
        for platform_config in "${PLATFORMS[@]}"; do
            IFS=':' read -r p _ <<< "$platform_config"
            echo "  - $p"
        done
        exit 1
    fi
else
    echo "Usage: $0 [--all|--platform <platform>]"
    echo ""
    echo "Options:"
    echo "  --all              Run tests on all platforms"
    echo "  --platform <name>  Run tests on specific platform"
    echo ""
    echo "Available platforms:"
    for platform_config in "${PLATFORMS[@]}"; do
        IFS=':' read -r p _ <<< "$platform_config"
        echo "  - $p"
    done
    exit 1
fi
