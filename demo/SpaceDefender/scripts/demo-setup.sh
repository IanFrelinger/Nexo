#!/bin/bash

# Nexo "Build a Game in 10 Minutes" Demo Setup Script
# This script prepares the demo environment and ensures everything is ready

set -e

echo "🚀 Setting up Nexo Demo Environment..."

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Function to print colored output
print_status() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

print_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Check prerequisites
check_prerequisites() {
    print_status "Checking prerequisites..."
    
    # Check if .NET 8.0 is installed
    if ! command -v dotnet &> /dev/null; then
        print_error ".NET 8.0 SDK is not installed"
        exit 1
    fi
    
    # Check .NET version
    DOTNET_VERSION=$(dotnet --version)
    if [[ ! $DOTNET_VERSION == 8.* ]]; then
        print_error "Requires .NET 8.0, found $DOTNET_VERSION"
        exit 1
    fi
    
    # Check if Nexo CLI is installed
    if ! command -v nexo &> /dev/null; then
        print_error "Nexo CLI is not installed"
        exit 1
    fi
    
    print_success "Prerequisites check passed"
}

# Clean demo environment
clean_environment() {
    print_status "Cleaning demo environment..."
    
    # Remove existing demo project if it exists
    if [ -d "space-defender" ]; then
        rm -rf space-defender
        print_success "Removed existing demo project"
    fi
    
    # Create fresh demo directory
    mkdir -p demo-workspace
    cd demo-workspace
    
    print_success "Demo environment cleaned"
}

# Initialize Nexo project
init_nexo_project() {
    print_status "Initializing Nexo project..."
    
    # Initialize project with all platforms
    nexo init space-defender \
        --template=game \
        --platforms=web,desktop,mobile,console \
        --ai-enabled \
        --monitoring \
        --verbose
    
    cd space-defender
    
    print_success "Nexo project initialized"
}

# Install dependencies
install_dependencies() {
    print_status "Installing dependencies..."
    
    # Restore NuGet packages
    dotnet restore
    
    # Install Nexo packages
    nexo install --packages=all
    
    print_success "Dependencies installed"
}

# Configure demo settings
configure_demo() {
    print_status "Configuring demo settings..."
    
    # Create demo configuration
    cat > nexo.demo.json << EOF
{
  "demo": {
    "name": "Space Defender",
    "duration": "10 minutes",
    "features": [
      "pipeline-architecture",
      "ai-integration",
      "cross-platform",
      "feature-factory",
      "real-time-monitoring"
    ],
    "platforms": ["web", "desktop", "mobile", "console"],
    "performance": {
      "targetFPS": 60,
      "maxMemory": "100MB",
      "loadTime": "3s"
    }
  }
}
EOF
    
    print_success "Demo configuration created"
}

# Prepare demo assets
prepare_assets() {
    print_status "Preparing demo assets..."
    
    # Create assets directory
    mkdir -p Assets/{Sprites,Audio,UI}
    
    # Create placeholder assets
    echo "Demo sprites will be generated" > Assets/Sprites/README.md
    echo "Demo audio will be generated" > Assets/Audio/README.md
    echo "Demo UI will be generated" > Assets/UI/README.md
    
    print_success "Demo assets prepared"
}

# Test build
test_build() {
    print_status "Testing build..."
    
    # Test build for all platforms
    nexo build --platforms=web --test
    nexo build --platforms=desktop --test
    nexo build --platforms=mobile --test
    nexo build --platforms=console --test
    
    print_success "All platform builds tested successfully"
}

# Start monitoring
start_monitoring() {
    print_status "Starting monitoring services..."
    
    # Start performance monitoring
    nexo monitor --start --real-time &
    MONITOR_PID=$!
    
    # Start error tracking
    nexo errors --start --real-time &
    ERROR_PID=$!
    
    # Save PIDs for cleanup
    echo $MONITOR_PID > .monitor.pid
    echo $ERROR_PID > .error.pid
    
    print_success "Monitoring services started"
}

# Create demo checklist
create_checklist() {
    print_status "Creating demo checklist..."
    
    cat > DEMO_CHECKLIST.md << EOF
# Demo Checklist

## Pre-Demo (5 minutes before)
- [ ] Terminal ready with Nexo CLI
- [ ] Demo project clean and empty
- [ ] All platforms ready (web, desktop, mobile)
- [ ] Audio and video recording tested
- [ ] Backup demo ready

## Demo Execution (10 minutes)
- [ ] Phase 1: Setup & Introduction (1 min)
- [ ] Phase 2: Core Game Logic (3 min)
- [ ] Phase 3: Feature Factory Magic (3 min)
- [ ] Phase 4: Platform Deployment (2 min)
- [ ] Phase 5: Wrap-up & Impact (1 min)

## Post-Demo
- [ ] Share demo recording
- [ ] Provide access to demo code
- [ ] Schedule follow-up meetings
- [ ] Collect feedback

## Success Metrics
- [ ] Build time < 30 seconds
- [ ] All platforms working
- [ ] 60 FPS performance
- [ ] Zero critical errors
- [ ] Complete functionality
EOF
    
    print_success "Demo checklist created"
}

# Main execution
main() {
    print_status "Starting Nexo Demo Setup..."
    
    check_prerequisites
    clean_environment
    init_nexo_project
    install_dependencies
    configure_demo
    prepare_assets
    test_build
    start_monitoring
    create_checklist
    
    print_success "Demo setup completed successfully!"
    print_status "Demo is ready to run. Use 'nexo demo start' to begin."
    
    # Show next steps
    echo ""
    echo "Next steps:"
    echo "1. Run 'nexo demo start' to begin the demo"
    echo "2. Follow the demo script in DemoScript.md"
    echo "3. Use 'nexo demo stop' to end the demo"
    echo "4. Check DEMO_CHECKLIST.md for complete checklist"
}

# Cleanup function
cleanup() {
    print_status "Cleaning up..."
    
    # Stop monitoring services
    if [ -f .monitor.pid ]; then
        kill $(cat .monitor.pid) 2>/dev/null || true
        rm .monitor.pid
    fi
    
    if [ -f .error.pid ]; then
        kill $(cat .error.pid) 2>/dev/null || true
        rm .error.pid
    fi
    
    print_success "Cleanup completed"
}

# Set up signal handlers
trap cleanup EXIT INT TERM

# Run main function
main "$@"
