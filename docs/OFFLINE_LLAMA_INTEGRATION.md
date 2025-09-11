# Offline LLama AI Integration for Nexo

This document describes the offline LLama AI integration that has been added to the Nexo development orchestration platform. This integration provides offline AI capabilities using local LLama models through both Docker (Ollama) and native implementations.

## Overview

The offline LLama integration includes:

- **Ollama Provider**: Docker-based LLama models with automatic model management
- **Native Provider**: Direct LLamaSharp integration for platforms without Docker
- **Interactive Chat Interface**: REPL-style chat with syntax highlighting
- **Model Management**: Commands to download, manage, and monitor models
- **Caching Integration**: Response caching using the existing Nexo caching system
- **Graceful Fallback**: Automatic fallback between Docker, native, and remote providers

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Nexo CLI Interface                      │
├─────────────────────────────────────────────────────────────┤
│  ChatCommand  │  ModelCommand  │  Other Commands           │
├─────────────────────────────────────────────────────────────┤
│                    IModelOrchestrator                      │
├─────────────────────────────────────────────────────────────┤
│  OllamaProvider  │  LlamaNativeProvider  │  Other Providers │
├─────────────────────────────────────────────────────────────┤
│              Caching System (Decorator Pattern)            │
├─────────────────────────────────────────────────────────────┤
│  Docker (Ollama)  │  Native (LlamaSharp)  │  Remote APIs   │
└─────────────────────────────────────────────────────────────┘
```

## Features

### 1. Multiple Provider Support

- **Ollama Provider** (Priority: 95)
  - Docker-based LLama models
  - Automatic model downloading and management
  - GPU acceleration support
  - HTTP API communication
  - Models stored in `~/.nexo/models/ollama/`

- **Native Provider** (Priority: 90)
  - Direct LlamaSharp integration
  - No Docker dependency
  - Platform-specific optimizations
  - Models stored in `~/.nexo/models/native/`

### 2. Interactive Chat Interface

The chat interface provides a rich REPL experience with:

- **Syntax Highlighting**: Code blocks are highlighted using Spectre.Console
- **Session Management**: Persistent chat history and context
- **Model Switching**: Switch between different models during conversation
- **Context Awareness**: Include project files and directory context
- **Command Support**: Built-in commands for model management

#### Chat Commands

- `exit` - Exit the chat session
- `help` - Show available commands
- `clear` - Clear chat history
- `/model <name>` - Switch to specific model
- `/context <text>` - Set context for the session
- `/history` - Show chat history
- `/stats` - Show model statistics

### 3. Model Management

Comprehensive model management through CLI commands:

#### List Models
```bash
nexo model list                    # List all models
nexo model list --provider ollama  # List Ollama models only
nexo model list --type code        # List code generation models
nexo model list --installed        # List only installed models
```

#### Download Models
```bash
nexo model pull codellama:7b-instruct    # Download specific model
nexo model pull llama2:7b-chat --provider ollama  # Use specific provider
nexo model pull mistral:7b --force       # Force re-download
```

#### Remove Models
```bash
nexo model remove codellama:7b-instruct  # Remove specific model
nexo model remove llama2:7b-chat --force # Force removal
```

#### Model Information
```bash
nexo model info codellama:7b-instruct    # Show detailed model info
nexo model info llama2:7b-chat --provider ollama
```

#### Health Check
```bash
nexo model health                    # Check all providers
nexo model health --provider ollama  # Check specific provider
```

### 4. Chat Interface Types

#### Interactive Chat
```bash
nexo chat interactive                    # Start interactive session
nexo chat interactive --model auto      # Auto-select best model
nexo chat interactive --context "C# project"  # Set context
```

#### Quick Chat
```bash
nexo chat quick "How do I implement dependency injection?"  # Quick question
nexo chat quick "Explain this code" --model codellama:7b   # Use specific model
```

#### Code Chat
```bash
nexo chat code "How do I optimize this function?" --file Program.cs
nexo chat code "Review this code" --directory src/
```

## Installation and Setup

### Prerequisites

1. **Docker** (for Ollama provider)
2. **.NET 8 SDK**
3. **Spectre.Console** NuGet package
4. **LlamaSharp** NuGet package (for native provider)

### 1. Docker Setup (Ollama)

#### Using Docker Compose
```bash
# Start Ollama service
docker-compose -f docker/docker-compose.ollama.yml up -d

# Check status
docker-compose -f docker/docker-compose.ollama.yml ps
```

#### Using Startup Script
```bash
# Make script executable
chmod +x scripts/start-ollama.sh

# Start Ollama
./scripts/start-ollama.sh
```

#### Manual Docker Setup
```bash
# Build Ollama image
docker build -f docker/Dockerfile.ollama -t nexo-ollama .

# Run container
docker run -d \
  --name nexo-ollama \
  -p 11434:11434 \
  -v ollama_models:/root/.ollama/models \
  nexo-ollama
```

### 2. Native Setup (LlamaSharp)

The native provider will automatically detect and use available models in the `~/.nexo/models/native/` directory.

### 3. Model Download

Download models using the CLI:

```bash
# Download popular models
nexo model pull codellama:7b-instruct
nexo model pull llama2:7b-chat
nexo model pull mistral:7b-instruct

# List available models
nexo model list
```

## Configuration

### Provider Priority

The system automatically selects the best available provider based on priority:

1. **Ollama Provider** (Priority: 95) - Highest priority for offline operation
2. **Native Provider** (Priority: 90) - Fallback when Docker is not available
3. **Remote Providers** (Priority: < 90) - Fallback for online operation

### Model Selection

When using `--model auto`, the system:

1. Checks for Ollama provider availability
2. Falls back to native provider if Ollama is not available
3. Prefers code generation models for code-related tasks
4. Uses the highest priority available provider

### Caching Configuration

The integration uses the existing Nexo caching system:

- **Ollama responses**: Cached for 1 hour
- **Native responses**: Cached for 2 hours (more expensive to generate)
- **Model lists**: Cached for 5-10 minutes
- **Health checks**: Not cached

## Usage Examples

### Basic Chat
```bash
# Start interactive chat
nexo chat interactive

# Quick question
nexo chat quick "How do I implement a repository pattern in C#?"

# Code-specific chat
nexo chat code "How can I optimize this LINQ query?" --file DataService.cs
```

### Model Management
```bash
# List all models
nexo model list

# Download a model
nexo model pull codellama:7b-instruct

# Check model info
nexo model info codellama:7b-instruct

# Check provider health
nexo model health
```

### Advanced Usage
```bash
# Chat with specific model and context
nexo chat interactive --model codellama:7b-instruct --context "ASP.NET Core project"

# Code review with file context
nexo chat code "Review this controller" --file Controllers/UserController.cs

# Architecture discussion with project context
nexo chat interactive --context "Microservices architecture" --model llama2:7b-chat
```

## Troubleshooting

### Common Issues

#### Ollama Not Starting
```bash
# Check Docker status
docker ps

# Check Ollama logs
docker logs nexo-ollama

# Restart Ollama
docker restart nexo-ollama
```

#### No Models Available
```bash
# Check available models for download
nexo model list --available

# Download a model
nexo model pull llama2:7b-chat

# Check model installation
nexo model list --installed
```

#### Provider Health Issues
```bash
# Check all providers
nexo model health

# Check specific provider
nexo model health --provider ollama

# Check provider logs
docker logs nexo-ollama  # For Ollama
# Check application logs for native provider
```

### Performance Optimization

#### GPU Acceleration
- Ensure NVIDIA drivers are installed
- Uncomment GPU configuration in `docker-compose.ollama.yml`
- Verify GPU support: `nvidia-smi`

#### Memory Management
- Monitor model memory usage: `nexo model info <model>`
- Unload unused models: Use `/model` command in chat
- Adjust Docker memory limits in compose file

#### Caching
- Check cache hit rates in application logs
- Adjust cache TTL in provider implementations
- Monitor cache storage usage

## Development

### Adding New Providers

1. Implement `ILlamaProvider` interface
2. Register provider in DI container
3. Add provider-specific commands if needed
4. Update model selection logic

### Extending Chat Commands

1. Add new command to `ProcessChatCommandAsync` method
2. Implement command logic
3. Update help text
4. Add command validation

### Custom Model Support

1. Add model detection logic to provider
2. Implement model-specific optimizations
3. Add model to supported models list
4. Update documentation

## Security Considerations

- Models are stored locally in user directories
- No data is sent to external services in offline mode
- Docker containers run with limited privileges
- Model files are not automatically shared between users

## Performance Characteristics

### Ollama Provider
- **Startup Time**: ~30-60 seconds (first time)
- **Model Loading**: ~5-15 seconds per model
- **Response Time**: ~1-5 seconds (depending on model size)
- **Memory Usage**: ~2-8GB per model (depending on size)

### Native Provider
- **Startup Time**: ~5-10 seconds
- **Model Loading**: ~10-30 seconds per model
- **Response Time**: ~2-10 seconds (depending on hardware)
- **Memory Usage**: ~1-4GB per model (depending on size)

## Future Enhancements

- [ ] Streaming response support
- [ ] Model fine-tuning capabilities
- [ ] Multi-model inference
- [ ] Advanced caching strategies
- [ ] Model performance benchmarking
- [ ] Integration with cloud model repositories
- [ ] Custom model format support
- [ ] Distributed inference support

## Contributing

When contributing to the offline LLama integration:

1. Follow existing code patterns and architecture
2. Add comprehensive tests for new features
3. Update documentation for any changes
4. Consider performance implications
5. Test with both Ollama and native providers
6. Validate caching behavior
7. Ensure graceful fallback behavior

## Support

For issues related to the offline LLama integration:

1. Check the troubleshooting section above
2. Review application logs
3. Verify provider health status
4. Test with different models
5. Check system resources (memory, disk space)
6. Verify Docker/container status (for Ollama)

## License

This integration follows the same license as the main Nexo project.
