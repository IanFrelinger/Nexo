#!/bin/bash

# Unified Feature Factory Test Runner
# Consolidates all test functionality into a single, configurable script
# Replaces multiple redundant test scripts with a unified approach

set -e

# Configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$(dirname "$SCRIPT_DIR")"
FEATURE_DEMO_DIR="$SCRIPT_DIR"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Default configuration
DEFAULT_TARGET_SCORE=95
DEFAULT_MAX_ITERATIONS=15
DEFAULT_ANALYSIS_LIMIT=50
DEFAULT_VERBOSE=false

# Test counters
TOTAL_TESTS=0
PASSED_TESTS=0
FAILED_TESTS=0

# Function to print colored output
print_header() {
    echo -e "${BLUE}$1${NC}"
    echo -e "${BLUE}$(printf '=%.0s' $(seq 1 ${#1}))${NC}"
}

print_success() {
    echo -e "${GREEN}✅ $1${NC}"
}

print_error() {
    echo -e "${RED}❌ $1${NC}"
}

print_info() {
    echo -e "${YELLOW}ℹ️  $1${NC}"
}

# Function to run a test and track results
run_test() {
    local test_name="$1"
    local test_command="$2"
    
    TOTAL_TESTS=$((TOTAL_TESTS + 1))
    echo -e "\n${BLUE}🔄 Running: $test_name${NC}"
    
    if eval "$test_command"; then
        print_success "$test_name - PASSED"
        PASSED_TESTS=$((PASSED_TESTS + 1))
        return 0
    else
        print_error "$test_name - FAILED"
        FAILED_TESTS=$((FAILED_TESTS + 1))
        return 1
    fi
}

# Function to run codebase analysis
run_analysis() {
    local limit="${1:-$DEFAULT_ANALYSIS_LIMIT}"
    local path="${2:-../src}"
    
    print_header "📊 CODEBASE ANALYSIS"
    echo "Analyzing codebase at: $path (limit: $limit)"
    
    cd "$FEATURE_DEMO_DIR"
    dotnet run analyze --path "$path" --limit "$limit"
}

# Function to generate a feature
generate_feature() {
    local description="$1"
    local platform="${2:-DotNet}"
    local target_score="${3:-$DEFAULT_TARGET_SCORE}"
    local max_iterations="${4:-$DEFAULT_MAX_ITERATIONS}"
    
    cd "$FEATURE_DEMO_DIR"
    dotnet run generate --description "$description" --platform "$platform" --target-score "$target_score" --max-iterations "$max_iterations"
}

# Function to run logging tests
run_logging_tests() {
    print_header "🧪 LOGGING SYSTEM TESTS"
    
    cd "$FEATURE_DEMO_DIR"
    
    # Run the integrated logging test suite
    run_test "Logging System Validation" "dotnet run test-logging --verbose"
    
    # Run the comprehensive logging test runner
    if [ -f "./ComprehensiveLoggingTestRunner.sh" ]; then
        run_test "Comprehensive Logging Tests" "./ComprehensiveLoggingTestRunner.sh"
    fi
}

# Function to run E2E tests
run_e2e_tests() {
    print_header "🔄 END-TO-END TESTING"
    
    cd "$FEATURE_DEMO_DIR"
    
    # Generate feature with E2E tests
    run_test "E2E Feature Generation" "dotnet run generate-e2e --description 'Test E2E feature with comprehensive validation' --platform DotNet"
    
    # Run E2E testing demo if available
    if [ -f "./E2ETestingDemo.sh" ]; then
        run_test "E2E Testing Demo" "./E2ETestingDemo.sh"
    fi
}

# Function to run stress tests
run_stress_tests() {
    local complexity="${1:-medium}"
    
    print_header "🚀 STRESS TESTS ($complexity complexity)"
    
    case $complexity in
        "quick")
            run_quick_stress_tests
            ;;
        "medium")
            run_medium_stress_tests
            ;;
        "comprehensive")
            run_comprehensive_stress_tests
            ;;
        *)
            print_error "Unknown stress test complexity: $complexity"
            return 1
            ;;
    esac
}

# Quick stress tests (focused, high-complexity features)
run_quick_stress_tests() {
    cd "$FEATURE_DEMO_DIR"
    
    # Generate complex e-commerce system
    run_test "Complex Product Management" "generate_feature 'Create a sophisticated Product Management System with Product entity (Id, Name, Description, Price, Category, SKU, StockQuantity, IsActive, CreatedAt, UpdatedAt), ProductCategory entity, ProductRepository interface, ProductService with CRUD operations, input validation, business rules, caching, and comprehensive error handling with logging and performance monitoring' DotNet 98 20"
    
    run_test "Advanced Order Processing" "generate_feature 'Create an enterprise-level Order Processing System with Order entity (Id, CustomerId, OrderDate, Status, TotalAmount, ShippingAddress, BillingAddress), OrderItem entity, OrderRepository interface, OrderService with complex business logic, order status management, inventory checking, payment processing integration, fraud detection, and comprehensive audit logging with real-time notifications' DotNet 98 20"
    
    run_test "Intelligent Customer Management" "generate_feature 'Create an advanced Customer Management System with Customer entity (Id, FirstName, LastName, Email, Phone, DateOfBirth, Address, IsActive, CreatedAt, LastLoginAt), CustomerRepository interface, CustomerService with authentication, profile management, order history, loyalty points, recommendation engine integration, and comprehensive validation with security best practices and GDPR compliance' DotNet 98 20"
}

# Medium stress tests (balanced complexity)
run_medium_stress_tests() {
    cd "$FEATURE_DEMO_DIR"
    
    # Generate e-commerce domain
    run_test "Product Management System" "generate_feature 'Create a comprehensive Product Management System with Product entity (Id, Name, Description, Price, Category, SKU, StockQuantity, IsActive, CreatedAt, UpdatedAt), ProductCategory entity, ProductRepository interface, ProductService with CRUD operations, input validation, business rules, and comprehensive error handling' DotNet 95 15"
    
    run_test "Order Management System" "generate_feature 'Create a sophisticated Order Management System with Order entity (Id, CustomerId, OrderDate, Status, TotalAmount, ShippingAddress, BillingAddress), OrderItem entity (OrderId, ProductId, Quantity, UnitPrice, TotalPrice), OrderRepository interface, OrderService with complex business logic, order status management, inventory checking, and payment processing integration' DotNet 95 15"
    
    run_test "Customer Management System" "generate_feature 'Create an advanced Customer Management System with Customer entity (Id, FirstName, LastName, Email, Phone, DateOfBirth, Address, IsActive, CreatedAt, LastLoginAt), CustomerRepository interface, CustomerService with authentication, profile management, order history, loyalty points, and comprehensive validation' DotNet 95 15"
    
    run_test "Payment Processing System" "generate_feature 'Create a robust Payment Processing System with Payment entity (Id, OrderId, Amount, PaymentMethod, Status, TransactionId, ProcessedAt, FailureReason), PaymentRepository interface, PaymentService with multiple payment methods (CreditCard, PayPal, BankTransfer), transaction processing, fraud detection, and comprehensive audit logging' DotNet 95 15"
    
    run_test "Inventory Management System" "generate_feature 'Create an intelligent Inventory Management System with InventoryItem entity (Id, ProductId, WarehouseId, Quantity, ReservedQuantity, ReorderLevel, LastRestockedAt), InventoryRepository interface, InventoryService with stock tracking, low stock alerts, automated reordering, warehouse management, and real-time inventory updates' DotNet 95 15"
}

# Comprehensive stress tests (full system)
run_comprehensive_stress_tests() {
    run_medium_stress_tests
    
    cd "$FEATURE_DEMO_DIR"
    
    # Additional comprehensive tests
    run_test "Advanced API Controller" "generate_feature 'Create a production-ready REST API Controller with full CRUD operations, advanced filtering (by category, price range, availability, date ranges), pagination, sorting, search functionality, bulk operations, image upload support, rate limiting, authentication, authorization, comprehensive Swagger documentation with examples, validation attributes, and comprehensive error handling with proper HTTP status codes' DotNet 98 20"
    
    run_test "Business Logic Engine" "generate_feature 'Create an advanced Business Logic Engine with complex pricing algorithms, discount calculations, tax computations, shipping cost calculations, inventory optimization, demand forecasting, automated reordering, price elasticity analysis, and comprehensive business rule engine with configurable parameters and real-time analytics' DotNet 98 20"
}

# Function to run multi-platform tests
run_multi_platform_tests() {
    local complexity="${1:-quick}"
    
    print_header "🌐 MULTI-PLATFORM TESTS ($complexity)"
    
    local platforms=("DotNet" "Java" "Python" "Node.js" "Go" "React" "Vue.js")
    local test_description="Create a simple User Management System with User entity (Id, Name, Email, IsActive, CreatedAt), UserRepository interface, and UserService with basic CRUD operations and validation"
    
    for platform in "${platforms[@]}"; do
        run_test "Multi-Platform Test - $platform" "generate_feature '$test_description' '$platform' 90 10"
    done
}

# Function to run scaling tests
run_scaling_tests() {
    local complexity="${1:-quick}"
    
    print_header "📈 SCALING TESTS ($complexity)"
    
    case $complexity in
        "quick")
            run_quick_scaling_tests
            ;;
        "comprehensive")
            run_comprehensive_scaling_tests
            ;;
        *)
            print_error "Unknown scaling test complexity: $complexity"
            return 1
            ;;
    esac
}

# Quick scaling tests
run_quick_scaling_tests() {
    cd "$FEATURE_DEMO_DIR"
    
    # Simple to complex progression
    run_test "Simple User Management" "generate_feature 'Create a simple User Management System with User entity (Id, Name, Email, IsActive, CreatedAt), UserRepository interface, and UserService with basic CRUD operations and validation' DotNet 85 5"
    
    run_test "Intermediate Product System" "generate_feature 'Create an intermediate Product Management System with Product entity (Id, Name, Description, Price, Category, SKU, StockQuantity, IsActive, CreatedAt, UpdatedAt), ProductCategory entity, ProductRepository interface, ProductService with CRUD operations, input validation, business rules, and error handling' DotNet 90 10"
    
    run_test "Advanced E-Commerce System" "generate_feature 'Create an advanced E-Commerce System with Product, Order, Customer, and Payment entities, comprehensive repository interfaces, business services with complex logic, API controllers, validation, error handling, logging, and performance monitoring' DotNet 95 15"
}

# Comprehensive scaling tests
run_comprehensive_scaling_tests() {
    run_quick_scaling_tests
    
    cd "$FEATURE_DEMO_DIR"
    
    # Enterprise-level tests
    run_test "Enterprise Microservices" "generate_feature 'Create an enterprise-level microservices architecture with Product Service, Order Service, Customer Service, Payment Service, and Notification Service, each with their own database, API gateway integration, service discovery, circuit breakers, distributed logging, and comprehensive monitoring' DotNet 98 25"
    
    run_test "Cloud-Native Application" "generate_feature 'Create a cloud-native application with containerization support, Kubernetes deployment manifests, health checks, metrics collection, distributed tracing, auto-scaling configuration, and comprehensive observability with Prometheus, Grafana, and Jaeger integration' DotNet 98 25"
}

# Function to run learning and mix-match tests
run_learning_tests() {
    local complexity="${1:-quick}"
    
    print_header "🧠 LEARNING & MIX-MATCH TESTS ($complexity)"
    
    cd "$FEATURE_DEMO_DIR"
    
    # Test command mixing and matching
    run_test "Command Mix-Match Test" "dotnet run analyze . && dotnet run generate --description 'Mixed domain feature combining user management with product catalog' --platform DotNet --target-score 90 --max-iterations 10"
    
    # Test learning from previous commands
    run_test "Learning from History" "dotnet run stats && dotnet run generate --description 'Feature that learns from previous successful patterns' --platform DotNet --target-score 90 --max-iterations 10"
}

# Function to show help
show_help() {
    echo "Unified Feature Factory Test Runner"
    echo "==================================="
    echo ""
    echo "Usage: $0 [OPTIONS] [TEST_TYPES...]"
    echo ""
    echo "OPTIONS:"
    echo "  -h, --help              Show this help message"
    echo "  -v, --verbose           Enable verbose output"
    echo "  --target-score SCORE    Set target score (default: $DEFAULT_TARGET_SCORE)"
    echo "  --max-iterations NUM    Set max iterations (default: $DEFAULT_MAX_ITERATIONS)"
    echo "  --analysis-limit NUM    Set analysis limit (default: $DEFAULT_ANALYSIS_LIMIT)"
    echo ""
    echo "TEST_TYPES:"
    echo "  analysis                Run codebase analysis"
    echo "  logging                 Run logging system tests"
    echo "  e2e                     Run end-to-end tests"
    echo "  stress [complexity]     Run stress tests (quick|medium|comprehensive)"
    echo "  multi-platform [complexity] Run multi-platform tests (quick|comprehensive)"
    echo "  scaling [complexity]    Run scaling tests (quick|comprehensive)"
    echo "  learning [complexity]   Run learning and mix-match tests (quick|comprehensive)"
    echo "  all                     Run all tests"
    echo ""
    echo "EXAMPLES:"
    echo "  $0 analysis                    # Run only analysis"
    echo "  $0 stress quick                # Run quick stress tests"
    echo "  $0 multi-platform comprehensive # Run comprehensive multi-platform tests"
    echo "  $0 all                         # Run all tests"
    echo "  $0 --target-score 98 stress    # Run stress tests with 98% target score"
}

# Function to print final results
print_results() {
    echo ""
    print_header "📊 TEST RESULTS SUMMARY"
    echo "Total Tests: $TOTAL_TESTS"
    echo "Passed: $PASSED_TESTS ✅"
    echo "Failed: $FAILED_TESTS ❌"
    
    if [ $FAILED_TESTS -eq 0 ]; then
        print_success "All tests passed! 🎉"
        return 0
    else
        print_error "Some tests failed. Please review the output above."
        return 1
    fi
}

# Main execution
main() {
    local test_types=()
    local target_score="$DEFAULT_TARGET_SCORE"
    local max_iterations="$DEFAULT_MAX_ITERATIONS"
    local analysis_limit="$DEFAULT_ANALYSIS_LIMIT"
    local verbose="$DEFAULT_VERBOSE"
    
    # Parse command line arguments
    while [[ $# -gt 0 ]]; do
        case $1 in
            -h|--help)
                show_help
                exit 0
                ;;
            -v|--verbose)
                verbose=true
                shift
                ;;
            --target-score)
                target_score="$2"
                shift 2
                ;;
            --max-iterations)
                max_iterations="$2"
                shift 2
                ;;
            --analysis-limit)
                analysis_limit="$2"
                shift 2
                ;;
            analysis|logging|e2e|stress|multi-platform|scaling|learning|all)
                test_types+=("$1")
                shift
                ;;
            quick|medium|comprehensive)
                # This is a complexity parameter for the previous test type
                if [ ${#test_types[@]} -gt 0 ]; then
                    test_types+=("$1")
                fi
                shift
                ;;
            *)
                print_error "Unknown option: $1"
                show_help
                exit 1
                ;;
        esac
    done
    
    # If no test types specified, show help
    if [ ${#test_types[@]} -eq 0 ]; then
        show_help
        exit 0
    fi
    
    # Print configuration
    print_header "🚀 UNIFIED FEATURE FACTORY TEST RUNNER"
    echo "Target Score: $target_score"
    echo "Max Iterations: $max_iterations"
    echo "Analysis Limit: $analysis_limit"
    echo "Verbose: $verbose"
    echo ""
    
    # Execute requested tests
    local i=0
    while [ $i -lt ${#test_types[@]} ]; do
        local test_type="${test_types[$i]}"
        local complexity=""
        
        # Check if next argument is a complexity parameter
        if [ $((i + 1)) -lt ${#test_types[@]} ]; then
            local next_arg="${test_types[$((i + 1))]}"
            case $next_arg in
                quick|medium|comprehensive)
                    complexity="$next_arg"
                    i=$((i + 1))  # Skip the complexity parameter
                    ;;
            esac
        fi
        
        case $test_type in
            analysis)
                run_analysis "$analysis_limit"
                ;;
            logging)
                run_logging_tests
                ;;
            e2e)
                run_e2e_tests
                ;;
            stress)
                run_stress_tests "${complexity:-medium}"
                ;;
            multi-platform)
                run_multi_platform_tests "${complexity:-quick}"
                ;;
            scaling)
                run_scaling_tests "${complexity:-quick}"
                ;;
            learning)
                run_learning_tests "${complexity:-quick}"
                ;;
            all)
                run_analysis "$analysis_limit"
                run_logging_tests
                run_e2e_tests
                run_stress_tests "medium"
                run_multi_platform_tests "quick"
                run_scaling_tests "quick"
                run_learning_tests "quick"
                ;;
        esac
        
        i=$((i + 1))
    done
    
    # Print final results
    print_results
}

# Run main function with all arguments
main "$@"
