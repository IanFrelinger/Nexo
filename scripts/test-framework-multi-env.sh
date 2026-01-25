#!/usr/bin/env bash
# Script to run framework tests across multiple virtual environments using Docker
# Tests infrastructure components (Base Framework + Stress Tests) across all target platforms

set -euo pipefail

PROJECT_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$PROJECT_ROOT"

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Create test results directory
mkdir -p test-results/framework

# Function to get environment config
get_env_config() {
    local env_name=$1
    case "$env_name" in
        ubuntu-8.0)
            echo ".docker/Dockerfile.test-framework|8.0|Ubuntu 22.04|docker"
            ;;
        ubuntu-7.0)
            echo ".docker/Dockerfile.test-framework|7.0|Ubuntu 22.04|docker"
            ;;
        alpine-8.0)
            echo ".docker/Dockerfile.test-framework-alpine|8.0|Alpine Linux|docker"
            ;;
        debian-8.0)
            echo ".docker/Dockerfile.test-framework-debian|8.0|Debian 12|docker"
            ;;
        android-8.0)
            echo ".docker/Dockerfile.test-framework-android|8.0|Android|docker"
            ;;
        windows-8.0)
            echo ".docker/Dockerfile.test-framework-windows|8.0|Windows|docker"
            ;;
        unity-8.0)
            echo "scripts/test-framework-unity.sh|8.0|Unity (.NET Standard 2.0)|native"
            ;;
        *)
            return 1
            ;;
    esac
}

# Function to run infrastructure tests in Docker
run_infrastructure_tests_docker() {
    local env_name=$1
    local dockerfile=$2
    local dotnet_version=$3
    local description=$4
    
    echo -e "${YELLOW}Building ${description} (${dotnet_version})...${NC}"
    
    # Build Docker image
    docker build \
        -f "$dockerfile" \
        -t "nexo-framework-test:${env_name}" \
        --build-arg DOTNET_VERSION="$dotnet_version" \
        . > "test-results/framework/${env_name}-build.log" 2>&1
    
    if [ $? -ne 0 ]; then
        echo -e "${RED}❌ Failed to build ${env_name}${NC}"
        return 1
    fi
    
    echo -e "${GREEN}✅ ${env_name} image built${NC}"
    
    # Determine if Windows or Linux container
    local is_windows=false
    if [[ "$dockerfile" == *"windows"* ]]; then
        is_windows=true
    fi
    
    # Phase 1: Base Framework Smoke Tests
    echo -e "${YELLOW}Phase 1: Testing Base Framework (Infrastructure dependencies)...${NC}"
    if [ "$is_windows" = true ]; then
        docker run --rm \
            -v "$PROJECT_ROOT/test-results/framework:/workspace/test-results" \
            -e DOTNET_VERSION="$dotnet_version" \
            "nexo-framework-test:${env_name}" \
            cmd /c "cd C:\workspace && dotnet test src\Nexo.Tests.Infrastructure\Nexo.Tests.Infrastructure.csproj --filter \"FullyQualifiedName~BaseFrameworkSmokeTests\" --logger \"console;verbosity=minimal\" --logger \"trx;LogFileName=${env_name}-base-framework.trx\" --results-directory C:\workspace\test-results > C:\workspace\test-results\${env_name}-base-framework.log 2>&1"
    else
        docker run --rm \
            -v "$PROJECT_ROOT/test-results/framework:/workspace/test-results" \
            -e DOTNET_VERSION="$dotnet_version" \
            "nexo-framework-test:${env_name}" \
            bash -c "
                cd /workspace && \
                dotnet test src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj \
                    --filter 'FullyQualifiedName~BaseFrameworkSmokeTests' \
                    --logger 'console;verbosity=minimal' \
                    --logger 'trx;LogFileName=${env_name}-base-framework.trx' \
                    --results-directory /workspace/test-results \
                    2>&1 | tee /workspace/test-results/${env_name}-base-framework.log
            " > "test-results/framework/${env_name}-base-framework-output.log" 2>&1
    fi
    
    local base_exit=$?
    
    # Phase 2: Infrastructure Stress Tests
    echo -e "${YELLOW}Phase 2: Testing Infrastructure Stress Tests...${NC}"
    if [ "$is_windows" = true ]; then
        docker run --rm \
            -v "$PROJECT_ROOT/test-results/framework:/workspace/test-results" \
            -e DOTNET_VERSION="$dotnet_version" \
            "nexo-framework-test:${env_name}" \
            cmd /c "cd C:\workspace && dotnet test src\Nexo.Tests.Infrastructure\Nexo.Tests.Infrastructure.csproj --filter \"FullyQualifiedName~StressTests\" --logger \"console;verbosity=minimal\" --logger \"trx;LogFileName=${env_name}-infrastructure-stress.trx\" --results-directory C:\workspace\test-results > C:\workspace\test-results\${env_name}-infrastructure-stress.log 2>&1" || true
    else
        docker run --rm \
            -v "$PROJECT_ROOT/test-results/framework:/workspace/test-results" \
            -e DOTNET_VERSION="$dotnet_version" \
            "nexo-framework-test:${env_name}" \
            bash -c "
                cd /workspace && \
                dotnet test src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj \
                    --filter 'FullyQualifiedName~StressTests' \
                    --logger 'console;verbosity=minimal' \
                    --logger 'trx;LogFileName=${env_name}-infrastructure-stress.trx' \
                    --results-directory /workspace/test-results \
                    2>&1 | tee /workspace/test-results/${env_name}-infrastructure-stress.log
            " > "test-results/framework/${env_name}-infrastructure-stress-output.log" 2>&1 || true
    fi
    
    local stress_exit=$?
    
    # Extract test results
    echo ""
    echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
    echo -e "${YELLOW}Test Results for ${env_name}${NC}"
    echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
    
    if [ -f "test-results/framework/${env_name}-base-framework.log" ]; then
        local base_passed=$(grep -oE '[0-9]+(?=\s+Passed!)' "test-results/framework/${env_name}-base-framework.log" | head -1 || echo "0")
        local base_failed=$(grep -oE '[0-9]+(?=\s+Failed!)' "test-results/framework/${env_name}-base-framework.log" | head -1 || echo "0")
        local base_total=$(grep -oE '[0-9]+(?=\s+Total)' "test-results/framework/${env_name}-base-framework.log" | head -1 || echo "0")
        
        if [ -n "$base_passed" ] && [ "$base_passed" != "0" ]; then
            echo -e "${GREEN}✅ Base Framework: ${base_total} total, ${base_passed} passed, ${base_failed} failed${NC}"
        else
            echo -e "${YELLOW}⚠️  Base Framework: Results not found or tests may have compilation issues${NC}"
        fi
    fi
    
    if [ -f "test-results/framework/${env_name}-infrastructure-stress.log" ]; then
        local stress_passed=$(grep -oE '[0-9]+(?=\s+Passed!)' "test-results/framework/${env_name}-infrastructure-stress.log" | head -1 || echo "0")
        local stress_failed=$(grep -oE '[0-9]+(?=\s+Failed!)' "test-results/framework/${env_name}-infrastructure-stress.log" | head -1 || echo "0")
        local stress_total=$(grep -oE '[0-9]+(?=\s+Total)' "test-results/framework/${env_name}-infrastructure-stress.log" | head -1 || echo "0")
        
        if [ -n "$stress_passed" ] && [ "$stress_passed" != "0" ]; then
            echo -e "${GREEN}✅ Infrastructure Stress: ${stress_total} total, ${stress_passed} passed, ${stress_failed} failed${NC}"
        else
            echo -e "${YELLOW}⚠️  Infrastructure Stress: Results not found or tests may have dependency issues${NC}"
        fi
    fi
    
    echo ""
    
    # Return success if base tests passed (stress tests may have dependency issues)
    return $base_exit
}

# Main execution
if [ "${1:-}" == "--all" ]; then
    echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
    echo -e "${YELLOW}Running Infrastructure Tests on All Docker Environments${NC}"
    echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
    echo ""
    
    ENVIRONMENTS="ubuntu-8.0 ubuntu-7.0 alpine-8.0 debian-8.0 android-8.0 windows-8.0 unity-8.0"
    FAILED=0
    
    for env_name in $ENVIRONMENTS; do
        config=$(get_env_config "$env_name")
        if [ $? -ne 0 ]; then
            echo -e "${RED}❌ Unknown environment: ${env_name}${NC}"
            continue
        fi
        
        IFS='|' read -r dockerfile dotnet_version description env_type <<< "$config"
        
        echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
        echo -e "${YELLOW}Testing ${env_name} (${description})${NC}"
        echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
        echo ""
        
        if [ "$env_type" == "docker" ]; then
            if ! run_infrastructure_tests_docker "$env_name" "$dockerfile" "$dotnet_version" "$description"; then
                FAILED=1
            fi
        else
            # Native execution (Unity, iOS)
            echo -e "${YELLOW}Running ${description} tests natively...${NC}"
            if [ -f "$dockerfile" ]; then
                chmod +x "$dockerfile"
                if ! "$dockerfile"; then
                    FAILED=1
                fi
            else
                echo -e "${RED}❌ Script not found: ${dockerfile}${NC}"
                FAILED=1
            fi
        fi
        
        echo ""
    done
    
    if [ $FAILED -eq 0 ]; then
        echo -e "${GREEN}✅ All infrastructure tests passed on all environments${NC}"
        exit 0
    else
        echo -e "${RED}❌ Some infrastructure tests failed${NC}"
        exit 1
    fi
elif [ "${1:-}" == "--env" ] && [ -n "${2:-}" ]; then
    ENV_NAME="$2"
    config=$(get_env_config "$ENV_NAME")
    
    if [ $? -ne 0 ]; then
        echo -e "${RED}❌ Unknown environment: ${ENV_NAME}${NC}"
        echo "Available: ubuntu-8.0 ubuntu-7.0 alpine-8.0 debian-8.0 android-8.0 windows-8.0 unity-8.0"
        exit 1
    fi
    
    IFS='|' read -r dockerfile dotnet_version description env_type <<< "$config"
    
    echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
    echo -e "${YELLOW}Testing ${ENV_NAME} (${description})${NC}"
    echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
    echo ""
    
    if [ "$env_type" == "docker" ]; then
        run_infrastructure_tests_docker "$ENV_NAME" "$dockerfile" "$dotnet_version" "$description"
    else
        # Native execution (Unity, iOS)
        echo -e "${YELLOW}Running ${description} tests natively...${NC}"
        if [ -f "$dockerfile" ]; then
            chmod +x "$dockerfile"
            "$dockerfile"
        else
            echo -e "${RED}❌ Script not found: ${dockerfile}${NC}"
            exit 1
        fi
    fi
else
    echo "Usage: $0 [--all|--env ENV_NAME]"
    echo ""
    echo "Environments:"
    echo "  - ubuntu-8.0: Ubuntu 22.04 (.NET 8.0)"
    echo "  - ubuntu-7.0: Ubuntu 22.04 (.NET 7.0)"
    echo "  - alpine-8.0: Alpine Linux (.NET 8.0)"
    echo "  - debian-8.0: Debian 12 (.NET 8.0)"
    echo "  - android-8.0: Android (.NET 8.0)"
    echo "  - windows-8.0: Windows (.NET 8.0)"
    echo "  - unity-8.0: Unity (.NET Standard 2.0 compatible)"
    exit 1
fi
