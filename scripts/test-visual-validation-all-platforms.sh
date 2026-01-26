#!/bin/bash
# Multi-platform visual validation test runner with Ollama
# Runs visual validation tests across all target platforms using real Ollama vision models

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

# Platforms to test with their docker-compose files
# Note: Alpine has known Playwright compatibility issues on arm64 (node binary path)
# Ubuntu and Debian work correctly with Playwright
PLATFORMS=(
    "ubuntu-8.0:docker-compose.visual-validation.yml:visual-test-ubuntu"
    "debian-8.0:docker-compose.visual-validation-debian.yml:visual-test-debian"
    # "alpine-8.0:docker-compose.visual-validation-alpine.yml:visual-test-alpine"  # Disabled: Playwright arm64 compatibility issue
)

# Function to run tests on a platform
run_platform_tests() {
    local platform=$1
    local compose_file=$2
    local service_name=$3
    
    echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
    echo -e "${BLUE}🎨 Testing Visual Validation on ${platform} with Ollama${NC}"
    echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
    
    # Start Ollama service for this platform
    echo -e "${YELLOW}Starting Ollama service...${NC}"
    if ! docker-compose -f "$compose_file" up -d ollama > "$RESULTS_DIR/${platform}-ollama-start.log" 2>&1; then
        echo -e "${RED}❌ Failed to start Ollama for ${platform}${NC}"
        return 1
    fi
    
    # Wait for Ollama to be ready and pull model
    echo -e "${YELLOW}Waiting for Ollama to be ready...${NC}"
    sleep 15
    
    # Get Ollama container name from compose file
    local ollama_container=$(docker-compose -f "$compose_file" ps -q ollama | head -1)
    if [ -n "$ollama_container" ]; then
        local container_name=$(docker inspect --format='{{.Name}}' "$ollama_container" | sed 's/^\///')
        echo -e "${YELLOW}Pulling llava:7b model...${NC}"
        docker exec "$container_name" ollama pull llava:7b > "$RESULTS_DIR/${platform}-model-pull.log" 2>&1 || true
    fi
    
    # Run tests
    echo -e "${YELLOW}Running visual validation tests...${NC}"
    local result_file="$RESULTS_DIR/${platform}-results.txt"
    local exit_code=0
    
    if docker-compose -f "$compose_file" up --build "$service_name" > "$result_file" 2>&1; then
        echo -e "${GREEN}✅ ${platform} tests passed${NC}"
    else
        echo -e "${RED}❌ ${platform} tests failed${NC}"
        exit_code=1
    fi
    
    # Show summary
    if grep -q "✅ Visual validation tests completed" "$result_file"; then
        echo -e "${GREEN}   All visual validation tests passed on ${platform}${NC}"
        if grep -q "Successfully used Ollama vision model" "$result_file"; then
            echo -e "${GREEN}   ✓ Used real Ollama vision model for validation${NC}"
        else
            echo -e "${YELLOW}   ⚠ Used mock provider (Ollama may not have been available)${NC}"
        fi
    else
        echo -e "${RED}   Some tests failed on ${platform}${NC}"
        echo -e "${YELLOW}   See ${result_file} for details${NC}"
    fi
    
    # Cleanup
    echo -e "${YELLOW}Cleaning up...${NC}"
    docker-compose -f "$compose_file" down > /dev/null 2>&1 || true
    
    return $exit_code
}

# Function to run all platforms
run_all_platforms() {
    local failed_platforms=()
    local passed_platforms=()
    
    for platform_config in "${PLATFORMS[@]}"; do
        IFS=':' read -r platform compose_file service_name <<< "$platform_config"
        
        if run_platform_tests "$platform" "$compose_file" "$service_name"; then
            passed_platforms+=("$platform")
        else
            failed_platforms+=("$platform")
        fi
        
        echo ""
    done
    
    # Summary
    echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
    echo -e "${BLUE}📊 Visual Validation Test Summary (All Platforms)${NC}"
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
    
    echo -e "${GREEN}✅ All platforms passed visual validation tests with Ollama!${NC}"
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
        IFS=':' read -r p compose_file service_name <<< "$platform_config"
        if [ "$p" == "$platform" ]; then
            run_platform_tests "$platform" "$compose_file" "$service_name"
            found=true
            break
        fi
    done
    if [ "$found" == false ]; then
        echo -e "${RED}Unknown platform: $platform${NC}"
        echo -e "${YELLOW}Available platforms:${NC}"
        for platform_config in "${PLATFORMS[@]}"; do
            IFS=':' read -r p _ _ <<< "$platform_config"
            echo "  - $p"
        done
        exit 1
    fi
else
    echo "Usage: $0 [--all|--platform <platform>]"
    echo ""
    echo "Options:"
    echo "  --all              Run tests on all platforms with Ollama"
    echo "  --platform <name> Run tests on specific platform"
    echo ""
    echo "Available platforms:"
    for platform_config in "${PLATFORMS[@]}"; do
        IFS=':' read -r p _ _ <<< "$platform_config"
        echo "  - $p"
    done
    exit 1
fi
