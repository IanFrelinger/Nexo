# Local Llama Model Integration with Nexo Feature Factory

This guide shows you how to integrate your local hosted Llama model running in a container with the Nexo Feature Factory for AI-native code generation.

## 🚀 Quick Start

### 1. Setup Local Llama Model

```bash
# Start Ollama with Llama model
./setup-local-llama.sh
```

This script will:
- Start Ollama in a Docker container
- Pull the Llama 3.2 model
- Start Redis for caching
- Verify everything is working

### 2. Test Your Setup

```bash
# Test the local model
./test-local-llama.sh
```

### 3. Run Feature Factory Demo

```bash
# Run the demo with local Llama
./demo-feature-factory-local.sh
```

## 🐳 Docker Services

The setup includes two Docker services:

### Ollama Service
- **Image**: `ollama/ollama:latest`
- **Port**: `11434`
- **Volume**: `ollama_data` (persists models)
- **Models**: Automatically pulls `llama3.2:latest`

### Redis Service
- **Image**: `redis:7-alpine`
- **Port**: `6379`
- **Volume**: `redis_data` (persists cache)
- **Purpose**: Caching for improved performance

## 🔧 Configuration

### Environment Variables

The system automatically sets these when using local models:

```bash
NEXO_AI_PROVIDER=ollama
NEXO_AI_MODEL=llama3.2:latest
NEXO_AI_ENDPOINT=http://localhost:11434
NEXO_USE_LOCAL_MODELS=true
NEXO_CACHE_BACKEND=memory
```

### Configuration Files

- `appsettings.local.json` - Local development configuration
- `docker-compose.local.yml` - Docker services configuration

## 🎯 Usage Examples

### Basic Feature Generation

```bash
dotnet run --project src/Nexo.CLI -- \
  feature generate \
  --description "Create a Customer management system with CRUD operations" \
  --use-local-models \
  --local-model "llama3.2:latest" \
  --verbose
```

### Advanced Configuration

```bash
dotnet run --project src/Nexo.CLI -- \
  feature generate \
  --description "Build a REST API for product catalog" \
  --platform DotNet \
  --output "./generated-api" \
  --use-local-models \
  --local-model "llama3.2:latest" \
  --local-endpoint "http://localhost:11434" \
  --verbose
```

## 🧪 Testing

### Test Local Model Connection

```bash
curl http://localhost:11434/api/tags
```

### Test Code Generation

```bash
curl -X POST http://localhost:11434/api/generate \
  -H "Content-Type: application/json" \
  -d '{
    "model": "llama3.2:latest",
    "prompt": "Generate a C# class for a Customer entity",
    "stream": false
  }'
```

## 📊 Available Models

To see all available models in your Ollama instance:

```bash
curl -s http://localhost:11434/api/tags | jq '.models[].name'
```

Common models you can use:
- `llama3.2:latest` - Latest Llama 3.2 model
- `llama3.1:latest` - Llama 3.1 model
- `codellama:latest` - Code-specific Llama model
- `mistral:latest` - Mistral model

## 🔄 Management Commands

### Start Services
```bash
docker-compose -f docker-compose.local.yml up -d
```

### Stop Services
```bash
docker-compose -f docker-compose.local.yml down
```

### View Logs
```bash
docker-compose -f docker-compose.local.yml logs ollama
docker-compose -f docker-compose.local.yml logs redis
```

### Pull Additional Models
```bash
docker exec nexollama ollama pull codellama:latest
docker exec nexollama ollama pull mistral:latest
```

## 🛠️ Troubleshooting

### Ollama Not Starting
```bash
# Check Docker status
docker info

# Restart Ollama
docker-compose -f docker-compose.local.yml restart ollama

# Check logs
docker-compose -f docker-compose.local.yml logs ollama
```

### Model Not Found
```bash
# List available models
docker exec nexollama ollama list

# Pull specific model
docker exec nexollama ollama pull llama3.2:latest
```

### Connection Issues
```bash
# Test connection
curl http://localhost:11434/api/tags

# Check if port is in use
lsof -i :11434
```

### Performance Issues
```bash
# Check system resources
docker stats

# Monitor Ollama logs
docker-compose -f docker-compose.local.yml logs -f ollama
```

## 🎨 Customization

### Using Different Models

Edit `appsettings.local.json`:
```json
{
  "AI": {
    "PreferredModel": "codellama:latest"
  }
}
```

### Custom Ollama Endpoint

```bash
export NEXO_AI_ENDPOINT=http://your-ollama-server:11434
```

### Custom Model Parameters

The system uses these default parameters:
- Temperature: 0.3 (for code generation)
- Max Tokens: 2000-4000 (depending on task)
- Stream: false (for Feature Factory)

## 📈 Performance Tips

1. **Use SSD Storage**: Store Ollama data on SSD for faster model loading
2. **Allocate More RAM**: Llama models work better with more RAM
3. **Enable Caching**: Redis caching improves repeated requests
4. **Use Smaller Models**: For faster responses, use smaller model variants

## 🔒 Security Notes

- Ollama runs on localhost by default (secure)
- No external API keys required for local models
- All data stays on your machine
- Redis is also local-only

## 📚 Next Steps

1. **Explore Generated Code**: Check the `./generated-*` directories
2. **Customize Prompts**: Modify the Feature Factory prompts for your needs
3. **Add More Models**: Pull additional models for different use cases
4. **Integrate with IDE**: Use the generated code in your development workflow

## 🤝 Contributing

To improve the local Llama integration:

1. Test with different models
2. Optimize prompt templates
3. Add support for streaming responses
4. Improve error handling

## 📞 Support

If you encounter issues:

1. Check the troubleshooting section above
2. Review Docker and Ollama logs
3. Test with the provided test scripts
4. Verify your system meets the requirements

---

**Happy coding with your local Llama model! 🦙✨**
