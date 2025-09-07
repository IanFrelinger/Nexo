#!/bin/bash

# Demo script for Nexo Feature Factory using local Llama model
# This script demonstrates the Feature Factory with local Ollama/Llama integration

set -e

echo "🏭 Nexo Feature Factory Demo - Local Llama Edition"
echo "=================================================="

# Check if Ollama is running
if ! curl -s http://localhost:11434/api/tags > /dev/null 2>&1; then
    echo "❌ Ollama is not running. Please run ./setup-local-llama.sh first"
    exit 1
fi

echo "✅ Ollama is running"

# Check if Llama model is available
models=$(curl -s http://localhost:11434/api/tags | jq -r '.models[].name' 2>/dev/null || echo "")
if ! echo "$models" | grep -q "llama3.2:latest"; then
    echo "⚠️  Llama 3.2 model not found. Available models:"
    echo "$models" | sed 's/^/   • /'
    echo ""
    echo "Please run: docker exec nexollama ollama pull llama3.2:latest"
    exit 1
fi

echo "✅ Llama 3.2 model is available"

# Set environment variables for local development
export NEXO_AI_PROVIDER=ollama
export NEXO_AI_MODEL=llama3.2:latest
export NEXO_AI_ENDPOINT=http://localhost:11434
export NEXO_CACHE_BACKEND=memory
export NEXO_USE_LOCAL_MODELS=true

echo "🔧 Environment configured for local development"
echo "   • AI Provider: $NEXO_AI_PROVIDER"
echo "   • AI Model: $NEXO_AI_MODEL"
echo "   • AI Endpoint: $NEXO_AI_ENDPOINT"
echo "   • Cache Backend: $NEXO_CACHE_BACKEND"

# Build the project
echo ""
echo "🔨 Building Nexo CLI..."
dotnet build src/Nexo.CLI/Nexo.CLI.csproj --configuration Release --verbosity minimal

if [ $? -ne 0 ]; then
    echo "❌ Build failed. Please fix compilation errors first."
    exit 1
fi

echo "✅ Build successful"

# Test the local model connection
echo ""
echo "🧪 Testing local model connection..."
test_response=$(curl -s -X POST http://localhost:11434/api/generate \
  -H "Content-Type: application/json" \
  -d '{
    "model": "llama3.2:latest",
    "prompt": "Generate a simple C# class for a Customer entity with basic properties.",
    "stream": false,
    "options": {
      "temperature": 0.3,
      "max_tokens": 500
    }
  }' | jq -r '.response' 2>/dev/null || echo "Test failed")

if [ "$test_response" != "Test failed" ] && [ -n "$test_response" ]; then
    echo "✅ Local model test successful!"
    echo "📝 Sample code generation:"
    echo "$test_response" | head -10 | sed 's/^/   /'
    echo "   ..."
else
    echo "⚠️  Local model test failed, but continuing with demo"
fi

# Run the Feature Factory demo
echo ""
echo "🏭 Running Feature Factory Demo with Local Llama..."
echo "=================================================="

# Create a temporary demo configuration
cat > demo-config.json << EOF
{
  "AI": {
    "PreferredProvider": "ollama",
    "PreferredModel": "llama3.2:latest",
    "EnableLocalModels": true,
    "LocalModelEndpoint": "http://localhost:11434"
  },
  "FeatureFactory": {
    "UseLocalModels": true,
    "PreferredLocalModel": "llama3.2:latest",
    "FallbackToCloud": false,
    "EnableCaching": true
  }
}
EOF

# Run the demo with local configuration
echo "🚀 Starting Feature Factory demo..."
echo ""

# Run the feature factory command
dotnet run --project src/Nexo.CLI -- \
  --config demo-config.json \
  feature-factory generate \
  --description "Create a Customer management system with CRUD operations" \
  --entity "Customer" \
  --platforms "web,api" \
  --architecture "clean" \
  --output-dir "./generated-customer-system" \
  --use-local-models \
  --verbose

echo ""
echo "🎉 Feature Factory Demo Complete!"
echo "================================"
echo ""
echo "📁 Generated files should be in: ./generated-customer-system"
echo ""
echo "🔍 To inspect the generated code:"
echo "   ls -la ./generated-customer-system"
echo ""
echo "🧪 To test the generated system:"
echo "   cd ./generated-customer-system && dotnet build"
echo ""
echo "🛑 To stop local services:"
echo "   docker-compose -f docker-compose.local.yml down"
echo ""
echo "🔄 To run again: ./demo-feature-factory-local.sh"

# Clean up temporary config
rm -f demo-config.json
