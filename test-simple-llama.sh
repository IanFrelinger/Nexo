#!/bin/bash

# Simple test script for local Llama model
set -e

echo "Testing Simple Llama Model Test"
echo "=========================="

# Check if Ollama is running
echo "Search Checking Ollama service..."
if ! curl -s http://localhost:11434/api/tags > /dev/null 2>&1; then
    echo "ERROR: Ollama is not running"
    exit 1
fi
echo "SUCCESS: Ollama is running"

# Test 1: Simple hello
echo ""
echo "Testing Test 1: Simple greeting..."
response1=$(timeout 30 curl -s -X POST http://localhost:11434/api/generate \
  -H "Content-Type: application/json" \
  -d '{"model": "llama3.2:latest", "prompt": "Hello", "stream": false, "options": {"num_predict": 20}}' 2>/dev/null || echo "{}")

content1=$(echo "$response1" | jq -r '.response' 2>/dev/null || echo "Failed")
if [ "$content1" != "null" ] && [ -n "$content1" ] && [ "$content1" != "Failed" ]; then
    echo "SUCCESS: Simple greeting: $content1"
else
    echo "ERROR: Simple greeting failed"
    exit 1
fi

# Test 2: Short code request
echo ""
echo "Testing Test 2: Short code generation..."
response2=$(timeout 45 curl -s -X POST http://localhost:11434/api/generate \
  -H "Content-Type: application/json" \
  -d '{"model": "llama3.2:latest", "prompt": "Write a C# class with Name property", "stream": false, "options": {"num_predict": 100}}' 2>/dev/null || echo "{}")

content2=$(echo "$response2" | jq -r '.response' 2>/dev/null || echo "Failed")
if [ "$content2" != "null" ] && [ -n "$content2" ] && [ "$content2" != "Failed" ]; then
    echo "SUCCESS: Code generation successful!"
    echo "File Generated:"
    echo "$content2"
else
    echo "ERROR: Code generation failed"
    exit 1
fi

echo ""
echo "SUCCESS All tests passed! Local Llama model is working correctly."
