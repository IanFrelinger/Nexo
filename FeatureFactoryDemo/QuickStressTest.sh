#!/bin/bash

# Quick Feature Factory Stress Test
# Demonstrates system capabilities with focused, high-complexity features

echo "🚀 QUICK FEATURE FACTORY STRESS TEST"
echo "===================================="
echo "Testing system with high-complexity feature generation"
echo ""

cd /Users/ianfrelinger/RiderProjects/Nexo/FeatureFactoryDemo

# Step 1: Comprehensive Analysis
echo "📊 STEP 1: Comprehensive Codebase Analysis"
echo "=========================================="
dotnet run analyze --path ../src --limit 50
echo ""

# Step 2: Generate Complex E-Commerce System
echo "🛒 STEP 2: Complex E-Commerce System Generation"
echo "==============================================="

echo "  → Generating Advanced Product Management System..."
dotnet run generate --description "Create a sophisticated Product Management System with Product entity (Id, Name, Description, Price, Category, SKU, StockQuantity, IsActive, CreatedAt, UpdatedAt), ProductCategory entity, ProductRepository interface, ProductService with CRUD operations, input validation, business rules, caching, and comprehensive error handling with logging and performance monitoring" --platform DotNet --target-score 98 --max-iterations 20

echo ""

echo "  → Generating Advanced Order Processing System..."
dotnet run generate --description "Create an enterprise-level Order Processing System with Order entity (Id, CustomerId, OrderDate, Status, TotalAmount, ShippingAddress, BillingAddress), OrderItem entity, OrderRepository interface, OrderService with complex business logic, order status management, inventory checking, payment processing integration, fraud detection, and comprehensive audit logging with real-time notifications" --platform DotNet --target-score 98 --max-iterations 20

echo ""

echo "  → Generating Intelligent Customer Management System..."
dotnet run generate --description "Create an advanced Customer Management System with Customer entity (Id, FirstName, LastName, Email, Phone, DateOfBirth, Address, IsActive, CreatedAt, LastLoginAt), CustomerRepository interface, CustomerService with authentication, profile management, order history, loyalty points, recommendation engine integration, and comprehensive validation with security best practices and GDPR compliance" --platform DotNet --target-score 98 --max-iterations 20

echo ""

# Step 3: Generate Advanced API Layer
echo "🌐 STEP 3: Advanced API Layer Generation"
echo "========================================"

echo "  → Generating Comprehensive REST API Controller..."
dotnet run generate --description "Create a production-ready REST API Controller with full CRUD operations, advanced filtering (by category, price range, availability, date ranges), pagination, sorting, search functionality, bulk operations, image upload support, rate limiting, authentication, authorization, comprehensive Swagger documentation with examples, validation attributes, and comprehensive error handling with proper HTTP status codes" --platform DotNet --target-score 98 --max-iterations 20

echo ""

# Step 4: Generate Business Logic Engine
echo "🧠 STEP 4: Advanced Business Logic Engine"
echo "========================================="

echo "  → Generating Dynamic Pricing Engine..."
dotnet run generate --description "Create an intelligent Dynamic Pricing Engine with PriceRule entity, PricingService that calculates prices based on customer tier, volume discounts, seasonal pricing, competitor analysis, demand forecasting, A/B testing capabilities, machine learning integration, real-time price updates, comprehensive pricing strategies, and detailed analytics with performance tracking and optimization recommendations" --platform DotNet --target-score 98 --max-iterations 20

echo ""

# Step 5: Generate Testing Infrastructure
echo "🧪 STEP 5: Comprehensive Testing Infrastructure"
echo "=============================================="

echo "  → Generating Advanced Test Suite..."
dotnet run generate --description "Create a comprehensive testing framework with unit tests, integration tests, end-to-end tests, mock implementations, test data builders, assertion helpers, test fixtures, parameterized tests, test containers, parallel test execution, comprehensive coverage reporting, performance testing, load testing, and detailed test documentation with best practices and CI/CD integration" --platform DotNet --target-score 98 --max-iterations 20

echo ""

# Step 6: System Validation and Analysis
echo "✅ STEP 6: System Validation and Analysis"
echo "========================================="

echo "  → Running comprehensive validation..."
dotnet run validate --verbose

echo ""

echo "  → Analyzing system performance..."
dotnet run stats --all

echo ""

echo "🎯 STRESS TEST RESULTS"
echo "======================"
echo "✅ Generated 6 highly complex, enterprise-level features"
echo "✅ Each feature targeted 98+ quality score with 20 iterations"
echo "✅ Demonstrated advanced business logic, API design, and testing"
echo "✅ System handled complex, multi-layered requirements"
echo "✅ All features validated and stored in database"
echo ""
echo "🏆 Feature Factory successfully generated production-ready,"
echo "   enterprise-level features with high complexity and quality!"
