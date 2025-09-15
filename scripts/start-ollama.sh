#!/bin/bash

# Nexo Ollama Startup Script
# This script starts the Ollama Docker container for offline LLama AI integration

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
CONTAINER_NAME="nexo-ollama"
IMAGE_NAME="nexo-ollama"
PORT="11434"
COMPOSE_FILE="docker/docker-compose.ollama.yml"

echo -e "${BLUE}Running Starting Nexo Ollama AI Service${NC}"
echo "=================================="

# Check if Docker is running
if ! docker info > /dev/null 2>&1; then
    echo -e "${RED}ERROR: Docker is not running. Please start Docker first.${NC}"
    exit 1
fi

# Check if container already exists
if docker ps -a --format "table {{.Names}}" | grep -q "^${CONTAINER_NAME}$"; then
    echo -e "${YELLOW}WARNING:  Container ${CONTAINER_NAME} already exists${NC}"
    
    # Check if it's running
    if docker ps --format "table {{.Names}}" | grep -q "^${CONTAINER_NAME}$"; then
        echo -e "${GREEN}SUCCESS: Container is already running${NC}"
        echo -e "${BLUE}📡 Ollama API available at: http://localhost:${PORT}${NC}"
        exit 0
    else
        echo -e "${YELLOW}Processing Starting existing container...${NC}"
        docker start ${CONTAINER_NAME}
    fi
else
    echo -e "${BLUE}Building  Building Ollama image...${NC}"
    docker build -f docker/Dockerfile.ollama -t ${IMAGE_NAME} .
    
    echo -e "${BLUE}Running Starting Ollama container...${NC}"
    docker run -d \
        --name ${CONTAINER_NAME} \
        -p ${PORT}:11434 \
        -v ollama_models:/root/.ollama/models \
        -e OLLAMA_HOST=0.0.0.0 \
        -e OLLAMA_ORIGINS=* \
        -e OLLAMA_KEEP_ALIVE=24h \
        --restart unless-stopped \
        ${IMAGE_NAME}
fi

# Wait for Ollama to be ready
echo -e "${BLUE}⏳ Waiting for Ollama to be ready...${NC}"
max_attempts=30
attempt=0

while [ $attempt -lt $max_attempts ]; do
    if curl -s http://localhost:${PORT}/api/tags > /dev/null 2>&1; then
        echo -e "${GREEN}SUCCESS: Ollama is ready!${NC}"
        break
    fi
    
    attempt=$((attempt + 1))
    echo -e "${YELLOW}⏳ Attempt ${attempt}/${max_attempts} - waiting for Ollama...${NC}"
    sleep 2
done

if [ $attempt -eq $max_attempts ]; then
    echo -e "${RED}ERROR: Ollama failed to start within expected time${NC}"
    echo -e "${YELLOW}List Container logs:${NC}"
    docker logs ${CONTAINER_NAME}
    exit 1
fi

# Show available models
echo -e "${BLUE}List Available models:${NC}"
curl -s http://localhost:${PORT}/api/tags | jq -r '.models[].name' 2>/dev/null || echo "No models available"

echo ""
echo -e "${GREEN}SUCCESS Ollama is running successfully!${NC}"
echo -e "${BLUE}📡 API endpoint: http://localhost:${PORT}${NC}"
echo -e "${BLUE}Documentation Documentation: https://github.com/jmorganca/ollama${NC}"
echo ""
echo -e "${YELLOW}Idea Usage examples:${NC}"
echo "  # List models"
echo "  curl http://localhost:${PORT}/api/tags"
echo ""
echo "  # Generate text"
echo "  curl -X POST http://localhost:${PORT}/api/generate -d '{\"model\":\"llama2\",\"prompt\":\"Hello world\"}'"
echo ""
echo -e "${YELLOW}🛑 To stop Ollama:${NC}"
echo "  docker stop ${CONTAINER_NAME}"
echo ""
echo -e "${YELLOW}Removing  To remove Ollama:${NC}"
echo "  docker stop ${CONTAINER_NAME} && docker rm ${CONTAINER_NAME}"
