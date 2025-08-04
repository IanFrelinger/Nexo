# AI Configuration

The Nexo Framework provides a comprehensive AI configuration system that allows you to manage AI-specific settings for different workloads and environments. This system supports development, production, and AI-heavy modes with configurable model providers, resource allocation, performance settings, and monitoring.

## Overview

AI configuration manages all aspects of AI operations including:

- **Model Management** - Multiple AI providers (OpenAI, Azure OpenAI, Ollama)
- **Resource Allocation** - Memory, CPU, and GPU resource management
- **Performance Tuning** - Response time, batch processing, and parallel execution
- **Caching Strategies** - Response caching and semantic caching
- **Fallback Mechanisms** - Graceful degradation when AI services are unavailable
- **Monitoring & Alerting** - Performance monitoring and alert thresholds

## Quick Start

### Using CLI Commands

The easiest way to manage AI configuration is through CLI commands:

```bash
# Show current AI configuration
nexo ai-config show

# Set AI mode to production
nexo ai-config set-mode Production

# Validate AI configuration
nexo ai-config validate

# Export configuration to file
nexo ai-config export --output ai-config.json

# Import configuration from file
nexo ai-config import --config ai-config.json

# Reset to default configuration
nexo ai-config reset --mode Development

# List available AI modes
nexo ai-config list-modes

# Show default configuration for a mode
nexo ai-config show-default --mode Production
```

### Using Configuration Files

Create an AI configuration file and use it:

```bash
# Import configuration from file
nexo ai-config import --config my-ai-config.json

# Export current configuration
nexo ai-config export --output current-config.json
```

## Configuration Structure

An AI configuration consists of several key sections:

### Basic Information

```json
{
  "mode": "Development"
}
```

The mode determines the overall behavior and default settings:
- **Development** - Basic AI capabilities with minimal resource usage
- **Production** - Optimized for production workloads with balanced performance
- **AIHeavy** - Maximum AI capabilities with high resource allocation

### Model Configuration

```json
{
  "models": {
    "defaultProvider": "OpenAI",
    "selectionStrategy": "Priority",
    "fallbackChain": ["OpenAI", "AzureOpenAI"],
    "enableCaching": true,
    "cacheTimeoutSeconds": 1800,
    "providers": {
      "OpenAI": {
        "type": "OpenAI",
        "endpoint": "https://api.openai.com/v1",
        "apiKey": "",
        "defaultModel": "gpt-3.5-turbo",
        "models": ["gpt-3.5-turbo", "gpt-4"],
        "timeoutMs": 30000,
        "maxRetries": 3,
        "retryDelayMs": 1000,
        "priority": 1,
        "enabled": true
      }
    }
  }
}
```

#### Model Selection Strategies

- **Priority** - Use providers in priority order
- **CostOptimized** - Select based on cost considerations
- **PerformanceOptimized** - Select based on performance requirements
- **Availability** - Select based on availability

#### Supported Providers

- **OpenAI** - OpenAI API integration
- **AzureOpenAI** - Azure OpenAI Service integration
- **Ollama** - Local Ollama model server

### Resource Configuration

```json
{
  "resources": {
    "maxMemoryBytes": 1073741824,
    "maxCpuCores": 4,
    "maxGpuMemoryBytes": 0,
    "maxConcurrentOperations": 2,
    "allocationStrategy": "Balanced",
    "enableDynamicScaling": false,
    "monitoringIntervalSeconds": 60
  }
}
```

#### Resource Allocation Strategies

- **Balanced** - Equal allocation between CPU and memory
- **CpuOptimized** - Optimize for CPU-intensive operations
- **MemoryOptimized** - Optimize for memory-intensive operations
- **GpuOptimized** - Optimize for GPU operations

### Performance Configuration

```json
{
  "performance": {
    "mode": "Fast",
    "batchSize": 1,
    "maxResponseTimeMs": 15000,
    "enableParallelProcessing": false,
    "parallelProcessingLimit": 4,
    "enableResponseStreaming": true,
    "streamingChunkSize": 512
  }
}
```

#### Performance Modes

- **Fast** - Minimal processing for quick responses
- **Balanced** - Moderate processing for balanced performance
- **Quality** - Maximum processing for highest quality

### Caching Configuration

```json
{
  "caching": {
    "enableResponseCaching": true,
    "cacheDurationSeconds": 1800,
    "maxCacheSizeBytes": 52428800,
    "evictionPolicy": "Lru",
    "enableSemanticCaching": false,
    "semanticSimilarityThreshold": 0.8,
    "enableCacheWarming": false
  }
}
```

#### Cache Eviction Policies

- **Lru** - Least Recently Used
- **Lfu** - Least Frequently Used
- **Fifo** - First In First Out
- **TimeBased** - Time-based expiration

### Fallback Configuration

```json
{
  "fallback": {
    "enableBasicAnalysisFallback": true,
    "enableLocalModelFallback": false,
    "fallbackTimeoutMs": 5000,
    "maxFallbackAttempts": 2,
    "fallbackDelayMs": 500,
    "fallbackErrorMessage": "AI analysis unavailable, using basic analysis"
  }
}
```

### Monitoring Configuration

```json
{
  "monitoring": {
    "enableOperationMonitoring": true,
    "enablePerformanceMonitoring": true,
    "enableErrorMonitoring": true,
    "monitoringIntervalSeconds": 60,
    "maxHistoryEntries": 100,
    "enableAlerting": false,
    "alertThresholds": {
      "errorRateThreshold": 10.0,
      "responseTimeThresholdMs": 15000,
      "memoryUsageThreshold": 70.0,
      "cpuUsageThreshold": 80.0
    }
  }
}
```

## CLI Commands Reference

### `nexo ai-config show`

Shows the current AI configuration.

```bash
nexo ai-config show
```

### `nexo ai-config set-mode <mode>`

Sets the AI mode and updates the configuration.

```bash
nexo ai-config set-mode Production
nexo ai-config set-mode AIHeavy
```

### `nexo ai-config validate`

Validates the current AI configuration and reports any issues.

```bash
nexo ai-config validate
```

### `nexo ai-config export [--output <path>]`

Exports the AI configuration to a file or displays it in the console.

```bash
# Export to file
nexo ai-config export --output ai-config.json

# Display in console
nexo ai-config export
```

### `nexo ai-config import --config <path>`

Imports AI configuration from a file.

```bash
nexo ai-config import --config ai-config.json
```

### `nexo ai-config reset [--mode <mode>]`

Resets the AI configuration to default values for the specified mode.

```bash
# Reset to default development mode
nexo ai-config reset

# Reset to default production mode
nexo ai-config reset --mode Production
```

### `nexo ai-config list-modes`

Lists all available AI modes.

```bash
nexo ai-config list-modes
```

### `nexo ai-config show-default [--mode <mode>]`

Shows the default configuration for a specific mode.

```bash
# Show default development configuration
nexo ai-config show-default

# Show default production configuration
nexo ai-config show-default --mode Production
```

## Mode-Specific Configurations

### Development Mode

Optimized for development with:
- Minimal resource usage (1GB RAM, 2 CPU cores)
- Fast response times (15 seconds max)
- Basic caching (30 minutes)
- Fallback to basic analysis
- No alerting

### Production Mode

Optimized for production with:
- Balanced resource usage (4GB RAM, 8 CPU cores)
- Moderate response times (45 seconds max)
- Enhanced caching (2 hours)
- Multiple provider fallback
- Performance monitoring and alerting

### AI-Heavy Mode

Optimized for intensive AI workloads with:
- High resource usage (16GB RAM, 16 CPU cores, 8GB GPU)
- Extended response times (3 minutes max)
- Advanced caching (4 hours)
- Multiple provider fallback with local models
- Comprehensive monitoring and alerting

## Best Practices

### Security

1. **API Key Management**
   - Never commit API keys to version control
   - Use environment variables or secure key management
   - Rotate API keys regularly

2. **Endpoint Security**
   - Use HTTPS for all API endpoints
   - Validate endpoint URLs
   - Implement proper authentication

### Performance

1. **Resource Allocation**
   - Start with development mode for testing
   - Scale up to production mode for deployment
   - Use AI-heavy mode only for intensive workloads

2. **Caching Strategy**
   - Enable response caching for repeated requests
   - Use semantic caching for similar requests
   - Monitor cache hit rates

3. **Fallback Configuration**
   - Always enable basic analysis fallback
   - Configure multiple providers for redundancy
   - Set appropriate timeouts and retry limits

### Monitoring

1. **Alert Thresholds**
   - Set error rate thresholds based on your requirements
   - Monitor response times and resource usage
   - Configure alerts for critical issues

2. **Performance Tracking**
   - Enable operation monitoring
   - Track performance metrics
   - Review monitoring data regularly

## Troubleshooting

### Common Issues

1. **Configuration Validation Errors**
   ```bash
   nexo ai-config validate
   ```
   Check the validation output for specific issues and fix them.

2. **Provider Connection Issues**
   - Verify API keys are correct
   - Check endpoint URLs
   - Ensure network connectivity

3. **Resource Allocation Problems**
   - Monitor memory and CPU usage
   - Adjust resource limits as needed
   - Consider switching to a different mode

4. **Performance Issues**
   - Check response times
   - Review caching configuration
   - Consider enabling parallel processing

### Debugging

1. **Enable Detailed Logging**
   ```bash
   nexo ai-config show
   ```
   Review the configuration for logging settings.

2. **Check Provider Status**
   ```bash
   nexo ai-config validate
   ```
   Validate provider configurations.

3. **Monitor Resource Usage**
   Use system monitoring tools to track resource consumption.

## Integration with Other Features

### Pipeline Integration

AI configuration integrates with the pipeline system:

```json
{
  "commands": [
    {
      "id": "ai-analyze",
      "name": "AI Code Analysis",
      "description": "Analyze code using AI models",
      "category": "AI",
      "parameters": {
        "model": "gpt-4",
        "provider": "OpenAI"
      }
    }
  ]
}
```

### CLI Integration

AI configuration is available through the CLI:

```bash
# Use AI configuration in pipeline
nexo pipeline create ai-analysis --param model=gpt-4

# Configure AI settings for analysis
nexo ai-config set-mode Production
nexo analyze --path ./src
```

## Examples

### Development Setup

```bash
# Set up development environment
nexo ai-config set-mode Development
nexo ai-config validate

# Configure OpenAI provider
nexo ai-config export --output dev-config.json
# Edit dev-config.json with your API key
nexo ai-config import --config dev-config.json
```

### Production Setup

```bash
# Set up production environment
nexo ai-config set-mode Production
nexo ai-config validate

# Configure multiple providers
nexo ai-config export --output prod-config.json
# Edit prod-config.json with production settings
nexo ai-config import --config prod-config.json
```

### AI-Heavy Workload Setup

```bash
# Set up AI-heavy environment
nexo ai-config set-mode AIHeavy
nexo ai-config validate

# Configure local models
nexo ai-config export --output ai-heavy-config.json
# Edit ai-heavy-config.json with local model settings
nexo ai-config import --config ai-heavy-config.json
```

## Configuration File Examples

See the `examples/ai-config-example.json` file for a complete example of AI configuration with all available options and settings. 