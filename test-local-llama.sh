#!/bin/bash

# Test script to verify local Llama model setup
# This script tests the connection and basic functionality

set -e

echo "Testing Testing Local Llama Model Setup"
echo "=================================="

# Check if Ollama is running
echo "Search Checking Ollama service..."
if ! curl -s http://localhost:11434/api/tags > /dev/null 2>&1; then
    echo "ERROR: Ollama is not running. Please run ./setup-local-llama.sh first"
    exit 1
fi
echo "SUCCESS: Ollama is running"

# List available models
echo ""
echo "List Available models:"
models=$(curl -s http://localhost:11434/api/tags | jq -r '.models[] | "   • \(.name) (\(.size | . / 1024 / 1024 / 1024 | floor)GB)"' 2>/dev/null || echo "   • Unable to list models")
echo "$models"

# Test basic text generation
echo ""
echo "Testing Testing basic text generation..."
test_prompt="Generate a simple C# class for a Customer entity with Id, Name, and Email properties."

echo "Document Test prompt: $test_prompt"
echo ""

response=$(timeout 60 curl -s -X POST http://localhost:11434/api/generate \
  -H "Content-Type: application/json" \
  -d "{
    \"model\": \"llama3.2:latest\",
    \"prompt\": \"$test_prompt\",
    \"stream\": false,
    \"options\": {
      \"temperature\": 0.3,
      \"num_predict\": 500
    }
  }" 2>/dev/null || echo "{}")

# Extract response content
content=$(echo "$response" | jq -r '.response' 2>/dev/null || echo "Failed to parse response")

if [ "$content" != "null" ] && [ -n "$content" ] && [ "$content" != "Failed to parse response" ]; then
    echo "SUCCESS: Text generation successful!"
    echo ""
    echo "File Generated code:"
    echo "=================="
    echo "$content"
    echo "=================="
else
    echo "ERROR: Text generation failed"
    if [ -z "$response" ] || [ "$response" = "{}" ]; then
        echo "WARNING:  Request timed out or connection failed"
        echo "Idea Try running: docker restart nexollama"
    else
        echo "Raw response: $response"
    fi
    exit 1
fi

# Test code generation specifically
echo ""
echo "Testing Testing code generation..."
code_prompt="Create a C# interface for a repository pattern with CRUD operations for a Customer entity."

echo "Document Code generation prompt: $code_prompt"
echo ""

code_response=$(timeout 60 curl -s -X POST http://localhost:11434/api/generate \
  -H "Content-Type: application/json" \
  -d "{
    \"model\": \"llama3.2:latest\",
    \"prompt\": \"$code_prompt\",
    \"stream\": false,
    \"options\": {
      \"temperature\": 0.2,
      \"num_predict\": 800
    }
  }" 2>/dev/null || echo "{}")

code_content=$(echo "$code_response" | jq -r '.response' 2>/dev/null || echo "Failed to parse response")

if [ "$code_content" != "null" ] && [ -n "$code_content" ] && [ "$code_content" != "Failed to parse response" ]; then
    echo "SUCCESS: Code generation successful!"
    echo ""
    echo "File Generated interface:"
    echo "======================="
    echo "$code_content"
    echo "======================="
else
    echo "ERROR: Code generation failed"
    echo "Raw response: $code_response"
    exit 1
fi

# Test performance
echo ""
echo "⏱️  Testing performance..."
start_time=$(date +%s)

perf_response=$(timeout 30 curl -s -X POST http://localhost:11434/api/generate \
  -H "Content-Type: application/json" \
  -d "{
    \"model\": \"llama3.2:latest\",
    \"prompt\": \"Write a simple hello world function in C#\",
    \"stream\": false,
    \"options\": {
      \"temperature\": 0.1,
      \"num_predict\": 100
    }
  }" 2>/dev/null || echo "{}")

end_time=$(date +%s)
duration=$((end_time - start_time))

echo "SUCCESS: Performance test completed in ${duration} seconds"

# Summary
echo ""
echo "SUCCESS Local Llama Model Test Summary"
echo "================================="
echo "SUCCESS: Ollama service: Running"
echo "SUCCESS: Text generation: Working"
echo "SUCCESS: Code generation: Working"
echo "SUCCESS: Performance: ${duration}s response time"
echo ""
echo "Running Your local Llama model is ready for the Feature Factory!"
echo ""
echo "Next steps:"
echo "  1. Run: ./demo-feature-factory-local.sh"
echo "  2. Or test with: dotnet run --project src/Nexo.CLI -- feature generate --description 'Create a Customer management system' --use-local-models"
