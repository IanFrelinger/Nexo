#!/bin/bash
# Script to run framework tests across multiple virtual environments
# Tests core framework components (Domain, Application, Infrastructure, Orchestration)
# across all target platforms before testing applications

set -euo pipefail

PROJECT_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$PROJECT_ROOT"

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Environment configurations: "dockerfile|dotnet_version|description|type"
declare -A ENV_CONFIGS
ENV_CONFIGS["ubuntu-8.0"]=".docker/Dockerfile.test-framework|8.0|Ubuntu 22.04|docker"
ENV_CONFIGS["ubuntu-7.0"]=".docker/Dockerfile.test-framework|7.0|Ubuntu 22.04|docker"
ENV_CONFIGS["alpine-8.0"]=".docker/Dockerfile.test-framework-alpine|8.0|Alpine Linux|docker"
ENV_CONFIGS["debian-8.0"]=".docker/Dockerfile.test-framework-debian|8.0|Debian 12|docker"
ENV_CONFIGS["android-8.0"]=".docker/Dockerfile.test-framework-android|8.0|Android|docker"
ENV_CONFIGS["windows-8.0"]=".docker/Dockerfile.test-framework-windows|8.0|Windows|docker"
ENV_CONFIGS["ios-8.0"]="scripts/test-framework-ios.sh|8.0|iOS|native"
ENV_CONFIGS["unity-8.0"]="scripts/test-framework-unity.sh|8.0|Unity|native"

# Create test results directory
mkdir -p test-results/framework

# Function to run tests in Docker
run_framework_tests_docker() {
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
    
    # Run framework tests in phases
    echo -e "${YELLOW}Phase 1: Testing Domain layer...${NC}"
    docker run --rm \
        -v "$PROJECT_ROOT/test-results/framework:/workspace/test-results" \
        -e DOTNET_VERSION="$dotnet_version" \
        "nexo-framework-test:${env_name}" \
        bash -c "
            cd /workspace && \
            dotnet test src/Nexo.Tests.Domain/Nexo.Tests.Domain.csproj \
                --logger 'console;verbosity=minimal' \
                --logger 'trx;LogFileName=${env_name}-domain.trx' \
                --results-directory /workspace/test-results \
                2>&1 | tee /workspace/test-results/${env_name}-domain.log
        " > "test-results/framework/${env_name}-domain-output.log" 2>&1
    
    local domain_exit=$?
    
    echo -e "${YELLOW}Phase 2: Testing Application layer...${NC}"
    docker run --rm \
        -v "$PROJECT_ROOT/test-results/framework:/workspace/test-results" \
        -e DOTNET_VERSION="$dotnet_version" \
        "nexo-framework-test:${env_name}" \
        bash -c "
            cd /workspace && \
            dotnet test src/Nexo.Tests.Application/Nexo.Tests.Application.csproj \
                --logger 'console;verbosity=minimal' \
                --logger 'trx;LogFileName=${env_name}-application.trx' \
                --results-directory /workspace/test-results \
                2>&1 | tee /workspace/test-results/${env_name}-application.log
        " > "test-results/framework/${env_name}-application-output.log" 2>&1
    
    local app_exit=$?
    
    echo -e "${YELLOW}Phase 3: Testing Infrastructure layer...${NC}"
    docker run --rm \
        -v "$PROJECT_ROOT/test-results/framework:/workspace/test-results" \
        -e DOTNET_VERSION="$dotnet_version" \
        "nexo-framework-test:${env_name}" \
        bash -c "
            cd /workspace && \
            dotnet test src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj \
                --logger 'console;verbosity=minimal' \
                --logger 'trx;LogFileName=${env_name}-infrastructure.trx' \
                --results-directory /workspace/test-results \
                2>&1 | tee /workspace/test-results/${env_name}-infrastructure.log
        " > "test-results/framework/${env_name}-infrastructure-output.log" 2>&1
    
    local infra_exit=$?
    
    echo -e "${YELLOW}Phase 4: Testing Orchestration layer...${NC}"
    docker run --rm \
        -v "$PROJECT_ROOT/test-results/framework:/workspace/test-results" \
        -e DOTNET_VERSION="$dotnet_version" \
        "nexo-framework-test:${env_name}" \
        bash -c "
            cd /workspace && \
            dotnet test src/Nexo.Tests.Orchestration/Nexo.Tests.Orchestration.csproj \
                --logger 'console;verbosity=minimal' \
                --logger 'trx;LogFileName=${env_name}-orchestration.trx' \
                --results-directory /workspace/test-results \
                2>&1 | tee /workspace/test-results/${env_name}-orchestration.log
        " > "test-results/framework/${env_name}-orchestration-output.log" 2>&1
    
    local orch_exit=$?
    
    local exit_code=$((domain_exit + app_exit + infra_exit + orch_exit))
    
    # Extract test results
    for phase in domain application infrastructure orchestration; do
        if [ -f "test-results/framework/${env_name}-${phase}.log" ]; then
            local passed=$(grep -oP '\d+(?=\s+Passed!)' "test-results/framework/${env_name}-${phase}.log" | head -1 || echo "0")
            local failed=$(grep -oP '\d+(?=\s+Failed!)' "test-results/framework/${env_name}-${phase}.log" | head -1 || echo "0")
            local total=$(grep -oP '\d+(?=\s+Total)' "test-results/framework/${env_name}-${phase}.log" | head -1 || echo "0")
            
            if [ -n "$passed" ] && [ -n "$failed" ]; then
                echo -e "${GREEN}✅ ${env_name} ${phase}: ${total} total, ${passed} passed, ${failed} failed${NC}"
            fi
        fi
    done
    
    return $exit_code
}

# Function to run tests natively
run_framework_tests_native() {
    local env_name=$1
    local script_path=$2
    
    echo -e "${YELLOW}Running ${env_name} tests natively...${NC}"
    
    if [ -f "$script_path" ]; then
        chmod +x "$script_path"
        "$script_path"
        return $?
    else
        echo -e "${RED}❌ Script not found: ${script_path}${NC}"
        return 1
    fi
}

# Main execution
if [ "${1:-}" == "--all" ]; then
    echo -e "${BLUE}Running framework tests on all environments...${NC}"
    echo ""
    
    FAILED=0
    for env_name in "${!ENV_CONFIGS[@]}"; do
        IFS='|' read -r dockerfile dotnet_version description env_type <<< "${ENV_CONFIGS[$env_name]}"
        
        echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
        echo -e "${YELLOW}Testing ${env_name} (${description})${NC}"
        echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
        echo ""
        
        if [ "$env_type" == "docker" ]; then
            if ! run_framework_tests_docker "$env_name" "$dockerfile" "$dotnet_version" "$description"; then
                FAILED=1
            fi
        else
            if ! run_framework_tests_native "$env_name" "$dockerfile"; then
                FAILED=1
            fi
        fi
        
        echo ""
    done
    
    if [ $FAILED -eq 0 ]; then
        echo -e "${GREEN}✅ All framework tests passed on all environments${NC}"
        exit 0
    else
        echo -e "${RED}❌ Some framework tests failed${NC}"
        exit 1
    fi
elif [ "${1:-}" == "--env" ] && [ -n "${2:-}" ]; then
    ENV_NAME="$2"
    if [ -z "${ENV_CONFIGS[$ENV_NAME]:-}" ]; then
        echo -e "${RED}❌ Unknown environment: ${ENV_NAME}${NC}"
        echo "Available: ${!ENV_CONFIGS[*]}"
        exit 1
    fi
    
    IFS='|' read -r dockerfile dotnet_version description env_type <<< "${ENV_CONFIGS[$ENV_NAME]}"
    
    if [ "$env_type" == "docker" ]; then
        run_framework_tests_docker "$ENV_NAME" "$dockerfile" "$dotnet_version" "$description"
    else
        run_framework_tests_native "$ENV_NAME" "$dockerfile"
    fi
else
    echo "Usage: $0 [--all|--env ENV_NAME]"
    echo ""
    echo "Environments:"
    for env_name in "${!ENV_CONFIGS[@]}"; do
        IFS='|' read -r _ _ description _ <<< "${ENV_CONFIGS[$env_name]}"
        echo "  - $env_name: $description"
    done
    exit 1
fi
