# Local Vision Models for Visual Validation

## Overview

The geospatial visual validation framework supports local vision models to avoid API costs. This guide explains how to set up and use local vision models (primarily Ollama) for analyzing rendered 3D models.

## Supported Local Vision Models

### Ollama

Ollama is a local LLM runtime that supports vision models. It's free, runs entirely locally, and doesn't require API keys.

**Available Vision Models:**
- **llava:7b** - LLaVA 7B (recommended for most use cases)
- **llava:13b** - LLaVA 13B (higher quality, more memory)
- **llava:34b** - LLaVA 34B (best quality, requires significant memory)
- **llama3.2-vision:11b** - Llama 3.2 Vision 11B
- **llama3.2-vision:90b** - Llama 3.2 Vision 90B (requires significant memory)

## Setup Instructions

### 1. Install Ollama

**macOS:**
```bash
brew install ollama
# Or download from https://ollama.com
```

**Linux:**
```bash
curl -fsSL https://ollama.com/install.sh | sh
```

**Windows:**
Download and install from https://ollama.com

### 2. Pull a Vision Model

```bash
# Recommended: LLaVA 7B (good balance of quality and speed)
ollama pull llava:7b

# Or for higher quality (requires more memory)
ollama pull llava:13b
ollama pull llama3.2-vision:11b
```

### 3. Verify Installation

```bash
# Test that Ollama is running
ollama list

# Test vision model
ollama run llava:7b "describe this image" --image test.jpg
```

### 4. Configure Environment Variables (Optional)

The visual validation tests will automatically detect Ollama if it's running on the default port (`http://localhost:11434`). You can customize:

```bash
# Custom Ollama URL (if running on different host/port)
export OLLAMA_BASE_URL=http://localhost:11434

# Custom model name (defaults to llava:7b for vision)
export OLLAMA_MODEL=llava:7b
```

## Usage

### In Visual Validation Tests

The visual validation tests automatically try to use Ollama if available, falling back to mock providers if not:

```csharp
// Tests automatically use Ollama if available
var response = await _providerFactory.ExecuteVisionAsync(
    provider: "ollama",
    systemPrompt: "You are an expert in 3D graphics...",
    userPrompt: prompt,
    imageBytes: screenshot,
    config: new { model = "llava:7b" },
    cancellationToken: CancellationToken.None);
```

### Running Tests

```bash
# Run visual validation tests (will use Ollama if available)
dotnet test src/Nexo.Tests.GeospatialVisual/Nexo.Tests.GeospatialVisual.csproj

# Or with explicit provider
OLLAMA_MODEL=llava:7b dotnet test src/Nexo.Tests.GeospatialVisual/Nexo.Tests.GeospatialVisual.csproj
```

## How It Works

1. **Screenshot Capture**: The `Model3DRendererAdapter` renders 3D models and captures screenshots
2. **Image Encoding**: Screenshots are converted to base64-encoded strings
3. **Vision API Call**: Images are sent to Ollama's `/api/chat` endpoint with the `images` parameter
4. **Analysis**: The vision model analyzes the image and returns JSON validation results
5. **Fallback**: If Ollama is unavailable, tests fall back to mock providers

## Performance Considerations

- **Memory**: Vision models require significant RAM (7B model needs ~8GB, 13B needs ~16GB)
- **Speed**: First inference is slower (model loading), subsequent calls are faster
- **Quality**: Larger models (13B, 34B) provide better analysis but are slower

## Troubleshooting

### Ollama Not Detected

```bash
# Check if Ollama is running
curl http://localhost:11434/api/tags

# Start Ollama if not running
ollama serve
```

### Model Not Found

```bash
# List available models
ollama list

# Pull the model if missing
ollama pull llava:7b
```

### Out of Memory

If you get out-of-memory errors:
- Use a smaller model (`llava:7b` instead of `llava:13b`)
- Close other applications
- Reduce batch size in tests

### Slow Performance

- First inference is always slow (model loading)
- Subsequent calls are faster
- Consider using `llava:7b` for faster responses
- Ensure Ollama is running on the same machine (not remote)

## Alternative Local Vision Models

### LM Studio

LM Studio provides a GUI for running local models, including vision models. It exposes an OpenAI-compatible API:

```bash
# Configure to use LM Studio's API
export OLLAMA_BASE_URL=http://localhost:1234
```

### LocalAI

LocalAI is another option for running local models with OpenAI-compatible APIs:

```bash
# Install and configure LocalAI
# See: https://localai.io/
```

## Image Cleanup (Docker)

When you run visual validation via `scripts/test-visual-validation-all-platforms.sh`, the script removes the **built test images** (`nexo-visual-test:ubuntu-8.0`, `nexo-visual-test:alpine-8.0`, `nexo-visual-test:debian-8.0`) after each platform run so they do not accumulate. The **Ollama image** (`ollama/ollama:latest`) is kept so the next run can reuse it without re-pulling.

## Best Practices

1. **Use Appropriate Model Size**: Start with `llava:7b` for most use cases
2. **Keep Ollama Running**: Start Ollama before running tests to avoid cold starts
3. **Monitor Memory**: Vision models are memory-intensive
4. **Test Fallback**: Ensure tests work with mock providers when Ollama is unavailable
5. **Cache Results**: Consider caching validation results for repeated test runs

## Example Output

When using Ollama, you'll see logs like:

```
[INFO] Attempting to use Ollama vision model for validation
[INFO] Executing vision request with provider ollama
[INFO] Successfully used Ollama vision model for validation
```

If Ollama is unavailable:

```
[WARN] Ollama not available, using mock provider for validation
```

## Next Steps

- For production use with cloud APIs, see `GEOSPATIAL_VISUAL_VALIDATION.md`
- For multi-platform testing, see the Docker test configurations
- For custom vision models, extend `ProviderFactory.ExecuteVisionAsync`
