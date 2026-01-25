#!/bin/bash
# Script to run caching tests across multiple virtual environments
# Tests geospatial caching functionality on different OS and .NET versions

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

cd "$PROJECT_ROOT"

echo "🧪 Running Caching Tests Across Multiple Environments"
echo "====================================================="
echo ""

# Create test results directory
mkdir -p test-results/caching

# Colors for output
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Function to run caching tests in Docker
run_caching_tests_docker() {
    local env_name=$1
    local dockerfile=$2
    local dotnet_version=${3:-8.0}
    local os_name=$4
    
    echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
    echo -e "${YELLOW}Testing on ${env_name} (${os_name}) with .NET ${dotnet_version}...${NC}"
    echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
    
    # Build Docker image
    echo -e "${YELLOW}Building Docker image...${NC}"
    docker build -f "$dockerfile" \
        --build-arg DOTNET_VERSION="$dotnet_version" \
        -t "nexo-caching-test:${env_name}" \
        . > "test-results/caching/${env_name}-build.log" 2>&1
    
    if [ $? -ne 0 ]; then
        echo -e "${RED}❌ Failed to build Docker image${NC}"
        return 1
    fi
    
    # Run tests
    echo -e "${YELLOW}Running caching tests...${NC}"
    docker run --rm \
        -v "$PROJECT_ROOT/test-results/caching:/workspace/test-results" \
        -e DOTNET_VERSION="$dotnet_version" \
        -e TEST_FILTER="Caching" \
        "nexo-caching-test:${env_name}" \
        bash -c "
            cd /workspace && \
            dotnet test src/Nexo.Tests.GeospatialUnit/Nexo.Tests.GeospatialUnit.csproj \
                --filter 'FullyQualifiedName~CachingTests' \
                --logger 'console;verbosity=minimal' \
                --logger 'trx;LogFileName=${env_name}-unit.trx' \
                --results-directory /workspace/test-results \
                2>&1 | tee /workspace/test-results/${env_name}-unit.log && \
            dotnet test src/Nexo.Tests.GeospatialE2E/Nexo.Tests.GeospatialE2E.csproj \
                --filter 'FullyQualifiedName~CachingSmokeTests' \
                --logger 'console;verbosity=minimal' \
                --logger 'trx;LogFileName=${env_name}-e2e.trx' \
                --results-directory /workspace/test-results \
                2>&1 | tee /workspace/test-results/${env_name}-e2e.log && \
            dotnet test src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj \
                --filter 'FullyQualifiedName~Caching' \
                --logger 'console;verbosity=minimal' \
                --logger 'trx;LogFileName=${env_name}-infra.trx' \
                --results-directory /workspace/test-results \
                2>&1 | tee /workspace/test-results/${env_name}-infra.log
        " > "test-results/caching/${env_name}-output.log" 2>&1
    
    local exit_code=$?
    
    # Extract test results
    if [ -f "test-results/caching/${env_name}-unit.log" ]; then
        local passed=$(grep -oP '\d+(?=\s+Passed!)' "test-results/caching/${env_name}-unit.log" | head -1 || echo "0")
        local failed=$(grep -oP '\d+(?=\s+Failed!)' "test-results/caching/${env_name}-unit.log" | head -1 || echo "0")
        local total=$(grep -oP '\d+(?=\s+Total)' "test-results/caching/${env_name}-unit.log" | head -1 || echo "0")
        
        if [ -n "$passed" ] && [ -n "$failed" ]; then
            echo -e "${GREEN}✅ ${env_name} Unit Tests: ${total} total, ${passed} passed, ${failed} failed${NC}"
        fi
    fi
    
    if [ $exit_code -eq 0 ]; then
        echo -e "${GREEN}✅ ${env_name} tests completed successfully${NC}"
        return 0
    else
        echo -e "${RED}❌ ${env_name} tests failed (exit code: $exit_code)${NC}"
        echo -e "${YELLOW}   Check test-results/caching/${env_name}-*.log for details${NC}"
        return 1
    fi
}

# Function to run caching tests natively (iOS, Unity)
run_caching_tests_native() {
    local env_name=$1
    local script_path=$2
    local dotnet_version=${3:-8.0}
    local os_name=$4
    
    echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
    echo -e "${YELLOW}Testing on ${env_name} (${os_name}) with .NET ${dotnet_version}...${NC}"
    echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
    
    if [ ! -f "$script_path" ]; then
        echo -e "${RED}❌ Test script not found: $script_path${NC}"
        return 1
    fi
    
    # Run the native test script
    echo -e "${YELLOW}Running native test script...${NC}"
    bash "$script_path" > "test-results/caching/${env_name}-output.log" 2>&1
    
    local exit_code=$?
    
    if [ $exit_code -eq 0 ]; then
        echo -e "${GREEN}✅ ${env_name} tests completed successfully${NC}"
        return 0
    else
        echo -e "${RED}❌ ${env_name} tests failed (exit code: $exit_code)${NC}"
        echo -e "${YELLOW}   Check test-results/caching/${env_name}-output.log for details${NC}"
        return 1
    fi
}

# Parse arguments
ENVIRONMENTS=()
ALL_ENVS=false
QUICK=false

while [[ $# -gt 0 ]]; do
    case $1 in
        --all)
            ALL_ENVS=true
            shift
            ;;
        --quick)
            QUICK=true
            shift
            ;;
        --env)
            ENVIRONMENTS+=("$2")
            shift 2
            ;;
        *)
            echo "Unknown option: $1"
            echo "Usage: $0 [--all] [--quick] [--env ubuntu-8.0|ubuntu-7.0|alpine-8.0|debian-8.0]"
            exit 1
            ;;
    esac
done

# Define test environments
declare -A ENV_CONFIGS
ENV_CONFIGS["ubuntu-8.0"]=".docker/Dockerfile.test-caching|8.0|Ubuntu 22.04|docker"
ENV_CONFIGS["ubuntu-7.0"]=".docker/Dockerfile.test-caching|7.0|Ubuntu 22.04|docker"
ENV_CONFIGS["alpine-8.0"]=".docker/Dockerfile.test-caching-alpine|8.0|Alpine Linux|docker"
ENV_CONFIGS["debian-8.0"]=".docker/Dockerfile.test-caching-debian|8.0|Debian 12|docker"
ENV_CONFIGS["android-8.0"]=".docker/Dockerfile.test-caching-android|8.0|Android|docker"
ENV_CONFIGS["windows-8.0"]=".docker/Dockerfile.test-caching-windows|8.0|Windows|docker"
ENV_CONFIGS["ios-8.0"]="scripts/test-caching-ios.sh|8.0|iOS|native"
ENV_CONFIGS["unity-8.0"]="scripts/test-caching-unity.sh|8.0|Unity|native"

# Default to all environments if none specified
if [ ${#ENVIRONMENTS[@]} -eq 0 ] || [ "$ALL_ENVS" = true ]; then
    ENVIRONMENTS=("ubuntu-8.0" "ubuntu-7.0" "alpine-8.0" "debian-8.0" "android-8.0")
    # Note: windows-8.0 requires Windows containers (not available on Linux/macOS)
    # Note: ios-8.0 and unity-8.0 require native execution (macOS for iOS, Unity installation for Unity)
fi

# Run tests
FAILED_ENVS=()
PASSED_ENVS=()
START_TIME=$(date +%s)

for env in "${ENVIRONMENTS[@]}"; do
    if [ -z "${ENV_CONFIGS[$env]}" ]; then
        echo -e "${RED}❌ Unknown environment: $env${NC}"
        FAILED_ENVS+=("$env")
        continue
    fi
    
    IFS='|' read -r config_path dotnet_version os_name env_type <<< "${ENV_CONFIGS[$env]}"
    
    # Skip Windows on non-Windows hosts (requires Windows containers)
    if [[ "$env" == "windows-8.0" ]] && [[ "$OSTYPE" != "msys" ]] && [[ "$OSTYPE" != "win32" ]] && [[ "$OSTYPE" != "cygwin" ]]; then
        echo -e "${YELLOW}⚠️  Skipping $env (requires Windows containers)${NC}"
        continue
    fi
    
    # Skip iOS on non-macOS hosts
    if [[ "$env" == "ios-8.0" ]] && [[ "$OSTYPE" != "darwin"* ]]; then
        echo -e "${YELLOW}⚠️  Skipping $env (requires macOS)${NC}"
        continue
    fi
    
    if [ "$env_type" = "docker" ]; then
        if run_caching_tests_docker "$env" "$config_path" "$dotnet_version" "$os_name"; then
            PASSED_ENVS+=("$env")
        else
            FAILED_ENVS+=("$env")
        fi
    elif [ "$env_type" = "native" ]; then
        if run_caching_tests_native "$env" "$config_path" "$dotnet_version" "$os_name"; then
            PASSED_ENVS+=("$env")
        else
            FAILED_ENVS+=("$env")
        fi
    else
        echo -e "${RED}❌ Unknown environment type: $env_type${NC}"
        FAILED_ENVS+=("$env")
    fi
    
    echo ""
done

END_TIME=$(date +%s)
DURATION=$((END_TIME - START_TIME))

# Summary
echo ""
echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo -e "${BLUE}📊 Test Summary${NC}"
echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo ""

if [ ${#PASSED_ENVS[@]} -gt 0 ]; then
    echo -e "${GREEN}✅ Passed environments (${#PASSED_ENVS[@]}):${NC}"
    for env in "${PASSED_ENVS[@]}"; do
        echo -e "   ${GREEN}✓${NC} $env"
    done
    echo ""
fi

if [ ${#FAILED_ENVS[@]} -gt 0 ]; then
    echo -e "${RED}❌ Failed environments (${#FAILED_ENVS[@]}):${NC}"
    for env in "${FAILED_ENVS[@]}"; do
        echo -e "   ${RED}✗${NC} $env"
        echo "      Check test-results/caching/${env}-*.log for details"
    done
    echo ""
fi

echo -e "Total duration: ${DURATION}s"
echo -e "Test results: test-results/caching/"
echo ""

if [ ${#FAILED_ENVS[@]} -gt 0 ]; then
    echo -e "${RED}❌ Some tests failed${NC}"
    exit 1
else
    echo -e "${GREEN}✅ All caching tests passed on all environments!${NC}"
    exit 0
fi
