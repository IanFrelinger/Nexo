#!/bin/bash

# Setup script for local Llama model with Nexo Feature Factory
# This script sets up Ollama with Llama models for local development

set -e

echo "🚀 Setting up Local Llama Model for Nexo Feature Factory"
echo "=================================================="

# Check if Docker is running
if ! docker info > /dev/null 2>&1; then
    echo "❌ Docker is not running. Please start Docker and try again."
    exit 1
fi

# Check if Docker Compose is available
if ! command -v docker-compose &> /dev/null; then
    echo "❌ Docker Compose is not installed. Please install Docker Compose and try again."
    exit 1
fi

echo "✅ Docker is running"

# Start Ollama service
echo "🐳 Starting Ollama service..."
docker-compose -f docker-compose.local.yml up -d ollama

# Wait for Ollama to be ready
echo "⏳ Waiting for Ollama to be ready..."
sleep 10

# Check if Ollama is responding
max_attempts=30
attempt=1
while [ $attempt -le $max_attempts ]; do
    if curl -s http://localhost:11434/api/tags > /dev/null 2>&1; then
        echo "✅ Ollama is ready!"
        break
    fi
    echo "⏳ Attempt $attempt/$max_attempts - Waiting for Ollama..."
    sleep 2
    attempt=$((attempt + 1))
done

if [ $attempt -gt $max_attempts ]; then
    echo "❌ Ollama failed to start within expected time"
    exit 1
fi

# Pull Llama model (you can change this to your preferred model)
echo "📥 Pulling Llama 3.2 model..."
docker exec nexollama ollama pull llama3.2:latest

# Verify the model is available
echo "🔍 Verifying model availability..."
models=$(curl -s http://localhost:11434/api/tags | jq -r '.models[].name' 2>/dev/null || echo "")
if echo "$models" | grep -q "llama3.2:latest"; then
    echo "✅ Llama 3.2 model is ready!"
else
    echo "⚠️  Llama 3.2 model may not be fully loaded yet"
fi

# Test the model
echo "🧪 Testing the model..."
test_response=$(curl -s -X POST http://localhost:11434/api/generate \
  -H "Content-Type: application/json" \
  -d '{
    "model": "llama3.2:latest",
    "prompt": "Hello, how are you?",
    "stream": false
  }' | jq -r '.response' 2>/dev/null || echo "Test failed")

if [ "$test_response" != "Test failed" ] && [ -n "$test_response" ]; then
    echo "✅ Model test successful!"
    echo "📝 Sample response: ${test_response:0:100}..."
else
    echo "⚠️  Model test failed, but Ollama is running"
fi

# Start Redis for caching
echo "🔴 Starting Redis for caching..."
docker-compose -f docker-compose.local.yml up -d redis

# Wait for Redis to be ready
echo "⏳ Waiting for Redis to be ready..."
sleep 5

# Test Redis connection
if docker exec nexoredis redis-cli ping | grep -q "PONG"; then
    echo "✅ Redis is ready!"
else
    echo "⚠️  Redis may not be fully ready yet"
fi

echo ""
echo "🎉 Local Llama setup complete!"
echo "================================"
echo "📊 Services Status:"
echo "   • Ollama: http://localhost:11434"
echo "   • Redis:  localhost:6379"
echo ""
echo "🤖 Available Models:"
curl -s http://localhost:11434/api/tags | jq -r '.models[] | "   • \(.name) (\(.size | . / 1024 / 1024 / 1024 | floor)GB)"' 2>/dev/null || echo "   • Unable to list models"
echo ""
echo "🚀 Next Steps:"
echo "   1. Run the Feature Factory demo: ./demo-feature-factory-local.sh"
echo "   2. Or run with local config: dotnet run --project src/Nexo.CLI -- --config appsettings.local.json"
echo ""
echo "🛑 To stop services: docker-compose -f docker-compose.local.yml down"
echo "🔄 To restart: ./setup-local-llama.sh"
