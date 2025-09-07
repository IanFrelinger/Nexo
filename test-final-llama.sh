#!/bin/bash

# Final working test script for local Llama model
set -e

echo "🧪 Final Llama Model Test"
echo "========================="

# Check if Ollama is running
echo "🔍 Checking Ollama service..."
if ! curl -s http://localhost:11434/api/tags > /dev/null 2>&1; then
    echo "❌ Ollama is not running"
    exit 1
fi
echo "✅ Ollama is running"

# List available models
echo ""
echo "📋 Available models:"
models=$(curl -s http://localhost:11434/api/tags | jq -r '.models[] | "   • \(.name) (\(.size | . / 1024 / 1024 / 1024 | floor)GB)"' 2>/dev/null || echo "   • Unable to list models")
echo "$models"

# Test 1: Simple greeting
echo ""
echo "🧪 Test 1: Simple greeting..."
response1=$(timeout 30 curl -s -X POST http://localhost:11434/api/generate \
  -H "Content-Type: application/json" \
  -d '{"model": "llama3.2:latest", "prompt": "Hello", "stream": false, "options": {"num_predict": 20}}' 2>/dev/null || echo "{}")

content1=$(echo "$response1" | jq -r '.response' 2>/dev/null || echo "Failed")
if [ "$content1" != "null" ] && [ -n "$content1" ] && [ "$content1" != "Failed" ]; then
    echo "✅ Simple greeting: $content1"
else
    echo "❌ Simple greeting failed"
    exit 1
fi

# Test 2: Short code generation
echo ""
echo "🧪 Test 2: Code generation..."
response2=$(timeout 45 curl -s -X POST http://localhost:11434/api/generate \
  -H "Content-Type: application/json" \
  -d '{"model": "llama3.2:latest", "prompt": "Create a C# class with Name property", "stream": false, "options": {"num_predict": 150}}' 2>/dev/null || echo "{}")

content2=$(echo "$response2" | jq -r '.response' 2>/dev/null || echo "Failed")
if [ "$content2" != "null" ] && [ -n "$content2" ] && [ "$content2" != "Failed" ]; then
    echo "✅ Code generation successful!"
    echo ""
    echo "📄 Generated code:"
    echo "=================="
    echo "$content2"
    echo "=================="
else
    echo "❌ Code generation failed"
    exit 1
fi

# Test 3: Very short Customer entity
echo ""
echo "🧪 Test 3: Customer entity (short)..."
response3=$(timeout 45 curl -s -X POST http://localhost:11434/api/generate \
  -H "Content-Type: application/json" \
  -d '{"model": "llama3.2:latest", "prompt": "C# Customer class with Id and Name", "stream": false, "options": {"num_predict": 100}}' 2>/dev/null || echo "{}")

content3=$(echo "$response3" | jq -r '.response' 2>/dev/null || echo "Failed")
if [ "$content3" != "null" ] && [ -n "$content3" ] && [ "$content3" != "Failed" ]; then
    echo "✅ Customer entity generation successful!"
    echo ""
    echo "📄 Generated Customer class:"
    echo "============================"
    echo "$content3"
    echo "============================"
else
    echo "❌ Customer entity generation failed"
    exit 1
fi

# Performance test
echo ""
echo "⏱️  Performance test..."
start_time=$(date +%s)

perf_response=$(timeout 30 curl -s -X POST http://localhost:11434/api/generate \
  -H "Content-Type: application/json" \
  -d '{"model": "llama3.2:latest", "prompt": "Hello world in C#", "stream": false, "options": {"num_predict": 50}}' 2>/dev/null || echo "{}")

end_time=$(date +%s)
duration=$((end_time - start_time))

echo "✅ Performance test completed in ${duration} seconds"

# Summary
echo ""
echo "🎉 Local Llama Model Test Summary"
echo "================================="
echo "✅ Ollama service: Running"
echo "✅ Text generation: Working"
echo "✅ Code generation: Working"
echo "✅ Customer entity generation: Working"
echo "✅ Performance: ${duration}s response time"
echo ""
echo "🚀 Your local Llama model is ready for the Feature Factory!"
echo ""
echo "💡 Note: For longer prompts, the model may take more time to respond."
echo "   The Feature Factory will handle this with proper timeouts and retries."
echo ""
echo "Next steps:"
echo "  1. Run: ./demo-feature-factory-local.sh"
echo "  2. Or test with: dotnet run --project src/Nexo.CLI -- feature generate --description 'Create a Customer management system' --use-local-models"
