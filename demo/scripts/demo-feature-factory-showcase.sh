#!/bin/bash

# Script to showcase the Feature Factory functionality

set -euo pipefail

# Colors for output
readonly GREEN='\033[0;32m'
readonly RED='\033[0;31m'
readonly YELLOW='\033[1;33m'
readonly BLUE='\033[0;34m'
readonly PURPLE='\033[0;35m'
readonly NC='\033[0m' # No Color

# Get the directory of this script
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
CONSOLE_APP_PATH="$PROJECT_ROOT/demo/feature-lab/Playground.Console"

echo -e "${PURPLE}🏭 NEXO FEATURE FACTORY SHOWCASE${NC}"
echo "=============================================="
echo
echo -e "${BLUE}This demo showcases the AI-native feature generation system${NC}"
echo -e "${BLUE}that transforms natural language into production-ready code.${NC}"
echo

# Build the console application
echo -e "${BLUE}🔨 Building Playground.Console...${NC}"
if ! dotnet build "$CONSOLE_APP_PATH" -c Release --verbosity quiet; then
    echo -e "${RED}❌ Failed to build Playground.Console${NC}"
    exit 1
fi
echo -e "${GREEN}✅ Playground.Console built successfully${NC}"
echo

# Demo 1: Non-interactive mode (automated demo)
echo -e "${YELLOW}📋 Demo 1: Automated Non-Interactive Demo${NC}"
echo "This shows the console app running in automated mode:"
echo
dotnet run --project "$CONSOLE_APP_PATH" --no-build
echo
echo -e "${GREEN}✅ Non-interactive demo completed successfully${NC}"
echo

# Demo 2: Feature Factory simulation
echo -e "${YELLOW}📋 Demo 2: Feature Factory Simulation${NC}"
echo "This simulates the Feature Factory process:"
echo
echo -e "${BLUE}🤖 Simulating AI Agent Coordination...${NC}"
echo "✅ Agent Coordination: 4 specialized agents coordinated successfully"
sleep 1
echo "✅ Domain Analysis: Identified 3 entities, 2 value objects, 4 business rules"
sleep 1
echo "✅ Decision Engine: Selected Clean Architecture with 87.3% confidence"
sleep 1
echo "✅ Code Generation: Generated 8 files across .NET platform"
sleep 1
echo "✅ Test Generation: Generated 12 unit tests and 3 integration tests"
sleep 1
echo
echo -e "${GREEN}🎉 Feature generation completed successfully!${NC}"
echo
echo -e "${BLUE}Generated files:${NC}"
echo "  • Domain/Entities/User.cs"
echo "  • Application/Interfaces/IUserService.cs"
echo "  • Application/Services/UserService.cs"
echo "  • Infrastructure/Repositories/UserRepository.cs"
echo "  • Web/Controllers/UserController.cs"
echo "  • Mobile/Views/UserView.xaml"
echo "  • Mobile/ViewModels/UserViewModel.cs"
echo "  • Shared/DTOs/UserDto.cs"
echo "  • Tests/Unit/UserServiceTests.cs"
echo "  • Tests/Integration/UserControllerIntegrationTests.cs"
echo

# Demo 3: Show the web interface
echo -e "${YELLOW}📋 Demo 3: Web Interface${NC}"
echo "The Feature Factory is also available in the web interface:"
echo "  🔗 http://localhost:5000/feature-factory"
echo
echo -e "${BLUE}Starting the web server in the background...${NC}"
cd "$PROJECT_ROOT/demo/feature-lab/Playground.Server"
dotnet run --no-build &
SERVER_PID=$!
echo "Server started with PID: $SERVER_PID"
echo
echo -e "${GREEN}✅ Web server is running at http://localhost:5000${NC}"
echo -e "${GREEN}✅ Feature Factory is available at http://localhost:5000/feature-factory${NC}"
echo
echo -e "${YELLOW}Press Ctrl+C to stop the web server and exit the demo${NC}"

# Wait for user to stop the demo
trap "echo; echo -e '${BLUE}Stopping web server...${NC}'; kill $SERVER_PID 2>/dev/null; echo -e '${GREEN}Demo completed successfully!${NC}'; exit 0" INT

# Keep the script running
while true; do
    sleep 1
done
