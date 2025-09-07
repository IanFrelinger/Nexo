#!/bin/bash

# Nexo Feature Factory Demo Script
# This script demonstrates the AI-native feature factory capabilities

echo "🚀 Nexo Feature Factory Demo"
echo "============================="
echo ""

# Check if we're in the right directory
if [ ! -f "Nexo.sln" ]; then
    echo "❌ Error: Please run this script from the Nexo project root directory"
    exit 1
fi

# Build the project
echo "🔨 Building Nexo project..."
dotnet build src/Nexo.CLI/Nexo.CLI.csproj --configuration Release
if [ $? -ne 0 ]; then
    echo "❌ Build failed"
    exit 1
fi

echo "✅ Build completed successfully"
echo ""

# Create output directory
OUTPUT_DIR="demo-output"
if [ -d "$OUTPUT_DIR" ]; then
    echo "🧹 Cleaning up previous demo output..."
    rm -rf "$OUTPUT_DIR"
fi
mkdir -p "$OUTPUT_DIR"

echo "📁 Created output directory: $OUTPUT_DIR"
echo ""

# Demo 1: Analyze a feature description
echo "🔍 Demo 1: Feature Analysis"
echo "============================"
echo "Analyzing: 'Customer with name, email, phone, billing address. Email must be unique and validated.'"
echo ""

dotnet run --project src/Nexo.CLI/Nexo.CLI.csproj feature analyze \
    --description "Customer with name, email, phone, billing address. Email must be unique and validated." \
    --platform DotNet \
    --output "$OUTPUT_DIR/customer-analysis.json"

if [ $? -eq 0 ]; then
    echo "✅ Analysis completed successfully"
    echo "📄 Results saved to: $OUTPUT_DIR/customer-analysis.json"
else
    echo "❌ Analysis failed"
fi

echo ""

# Demo 2: Generate a complete feature
echo "🏭 Demo 2: Feature Generation"
echo "=============================="
echo "Generating complete Customer feature with CRUD operations..."
echo ""

dotnet run --project src/Nexo.CLI/Nexo.CLI.csproj feature generate \
    --description "Customer with name, email, phone, billing address. Email must be unique and validated." \
    --platform DotNet \
    --output "$OUTPUT_DIR/customer-feature" \
    --verbose

if [ $? -eq 0 ]; then
    echo "✅ Feature generation completed successfully"
    echo "📁 Generated code saved to: $OUTPUT_DIR/customer-feature"
    
    # Show generated files
    echo ""
    echo "📋 Generated Files:"
    find "$OUTPUT_DIR/customer-feature" -type f -name "*.cs" | while read file; do
        echo "   • $(basename "$file")"
    done
else
    echo "❌ Feature generation failed"
fi

echo ""

# Demo 3: Generate a more complex feature
echo "🏗️ Demo 3: Complex Feature Generation"
echo "======================================"
echo "Generating Order Management feature with multiple entities..."
echo ""

dotnet run --project src/Nexo.CLI/Nexo.CLI.csproj feature generate \
    --description "Order management system with Customer, Order, OrderItem, and Product entities. Orders have status, total amount, and date. OrderItems link products to orders with quantity and price. Products have name, description, and price." \
    --platform DotNet \
    --output "$OUTPUT_DIR/order-management" \
    --verbose

if [ $? -eq 0 ]; then
    echo "✅ Complex feature generation completed successfully"
    echo "📁 Generated code saved to: $OUTPUT_DIR/order-management"
    
    # Show generated files
    echo ""
    echo "📋 Generated Files:"
    find "$OUTPUT_DIR/order-management" -type f -name "*.cs" | while read file; do
        echo "   • $(basename "$file")"
    done
else
    echo "❌ Complex feature generation failed"
fi

echo ""

# Demo 4: Show file contents
echo "📖 Demo 4: Generated Code Preview"
echo "=================================="
echo "Preview of generated Customer entity:"
echo ""

if [ -f "$OUTPUT_DIR/customer-feature/src/Domain/Entities/Customer.cs" ]; then
    echo "--- Customer.cs ---"
    head -30 "$OUTPUT_DIR/customer-feature/src/Domain/Entities/Customer.cs"
    echo "..."
    echo ""
fi

if [ -f "$OUTPUT_DIR/customer-feature/src/Application/UseCases/CreateCustomerUseCase.cs" ]; then
    echo "--- CreateCustomerUseCase.cs ---"
    head -20 "$OUTPUT_DIR/customer-feature/src/Application/UseCases/CreateCustomerUseCase.cs"
    echo "..."
    echo ""
fi

# Summary
echo "🎉 Demo Summary"
echo "==============="
echo "✅ Feature Factory Demo completed!"
echo ""
echo "📊 What was demonstrated:"
echo "   • Natural language feature analysis"
echo "   • Complete feature generation with Clean Architecture"
echo "   • Entity, Repository, Use Case, and Test generation"
echo "   • Complex multi-entity feature generation"
echo "   • AI-powered domain analysis and code generation"
echo ""
echo "📁 Generated artifacts are available in:"
echo "   • $OUTPUT_DIR/customer-analysis.json (analysis results)"
echo "   • $OUTPUT_DIR/customer-feature/ (generated Customer feature)"
echo "   • $OUTPUT_DIR/order-management/ (generated Order Management feature)"
echo ""
echo "🚀 The AI-native feature factory is ready for production use!"
echo ""
echo "Next steps:"
echo "   • Review generated code for quality and completeness"
echo "   • Integrate generated features into your projects"
echo "   • Extend the system with custom agents and templates"
echo "   • Deploy to production environments"
