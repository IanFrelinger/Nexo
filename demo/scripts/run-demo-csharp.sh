#!/bin/bash

# C#-based demo runner script
# Provides a shell interface to the C# demo utilities

set -euo pipefail

# Colors for output
readonly GREEN='\033[0;32m'
readonly RED='\033[0;31m'
readonly YELLOW='\033[1;33m'
readonly BLUE='\033[0;34m'
readonly NC='\033[0m' # No Color

# Get the directory of this script
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"

# Function to check if .NET is available
check_dotnet() {
    if ! command -v dotnet >/dev/null 2>&1; then
        echo -e "${RED}❌ .NET SDK not found. Please install .NET 8 SDK.${NC}"
        exit 1
    fi
    
    local version
    version=$(dotnet --version)
    if [[ ! "$version" == "8."* ]]; then
        echo -e "${YELLOW}⚠️  Found .NET $version, but .NET 8 is recommended.${NC}"
    else
        echo -e "${GREEN}✅ Found .NET $version${NC}"
    fi
}

# Function to build the C# utilities
build_utilities() {
    echo -e "${BLUE}🔨 Building C# demo utilities...${NC}"
    
    if ! dotnet build "$SCRIPT_DIR/lib/Nexo.Demo.Scripts.csproj" -c Release --verbosity quiet; then
        echo -e "${RED}❌ Failed to build C# utilities${NC}"
        exit 1
    fi
    
    echo -e "${GREEN}✅ C# utilities built successfully${NC}"
}

# Function to run the C# demo
run_demo() {
    local command="${1:-run}"
    shift || true
    
    echo -e "${BLUE}🚀 Running Nexo Feature Lab Demo (C#)${NC}"
    echo "======================================"
    
    # Build utilities first
    build_utilities
    
    # Run the C# program
    dotnet run --project "$SCRIPT_DIR/lib/Nexo.Demo.Scripts.csproj" -- "$command" "$PROJECT_ROOT" "$@"
}

# Function to show usage
show_usage() {
    echo "NEXO FEATURE LAB DEMO RUNNER (C#)"
    echo "=================================="
    echo ""
    echo "Usage: $0 [command] [options]"
    echo ""
    echo "Commands:"
    echo "  validate              Run validation checks"
    echo "  seed                  Seed demo fixtures"
    echo "  build                 Build the solution"
    echo "  run [options]         Run the complete demo (default)"
    echo "  help                  Show this help message"
    echo ""
    echo "Demo Options:"
    echo "  --maui                Use MAUI application (default)"
    echo "  --blazor              Use Blazor application"
    echo "  --skip-validation     Skip validation checks"
    echo "  --no-fixtures         Skip fixture seeding"
    echo "  --port <number>       Specify port for Blazor app"
    echo ""
    echo "Examples:"
    echo "  $0 validate"
    echo "  $0 seed"
    echo "  $0 run --blazor --port 8080"
    echo "  $0 run --maui --skip-validation"
}

# Main execution
main() {
    local command="${1:-run}"
    
    case "$command" in
        "help"|"--help"|"-h")
            show_usage
            exit 0
            ;;
        "validate"|"seed"|"build"|"run")
            check_dotnet
            run_demo "$@"
            ;;
        *)
            echo -e "${RED}❌ Unknown command: $command${NC}"
            echo ""
            show_usage
            exit 1
            ;;
    esac
}

# Run main function with all arguments
main "$@"
