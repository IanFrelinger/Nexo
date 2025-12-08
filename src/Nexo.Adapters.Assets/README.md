# Nexo Asset Generation Adapters

This project contains adapters for real-world asset generation APIs, including images, audio, and 3D models.

## Configuration

### Cloud API Keys

For cloud-based APIs, add the following to your `appsettings.json` or environment variables:

```json
{
  "OpenAI": {
    "ApiKey": "your-openai-api-key"
  },
  "ElevenLabs": {
    "ApiKey": "your-elevenlabs-api-key"
  },
  "Suno": {
    "ApiKey": "your-suno-api-key"  // Optional
  },
  "Bark": {
    "ApiKey": "your-bark-api-key"  // Optional
  },
  "Meshy": {
    "ApiKey": "your-meshy-api-key"
  },
  "Tripo": {
    "ApiKey": "your-tripo-api-key"
  }
}
```

### Local/Self-Hosted Configuration

For local or self-hosted models, configure endpoints:

```json
{
  "LocalImageGenerator": {
    "BaseUrl": "http://localhost:7860",  // Stable Diffusion Automatic1111
    "Provider": "StableDiffusion",  // or "Ollama", "LocalAI", "Custom"
    "ApiKey": ""  // Optional, if your local server requires auth
  },
  "LocalAudioGenerator": {
    "BaseUrl": "http://localhost:8000",  // Bark local or Piper TTS
    "Provider": "Bark",  // or "Piper", "Coqui", "Custom"
    "ApiKey": ""  // Optional
  },
  "LocalModel3DGenerator": {
    "BaseUrl": "http://localhost:8080",  // Your custom 3D generation endpoint
    "ApiKey": ""  // Optional
  }
}
```

### Service Registration

#### Option 1: Use Cloud APIs

```csharp
using Nexo.Adapters.Assets;

services.AddNexoOrchestration();
services.AddAssetGenerators(options =>
{
    options.ImageGenerator = ImageGeneratorType.Dalle;  // DALL-E 3
    options.AudioGenerator = AudioGeneratorType.Suno;  // or Bark, ElevenLabs
    options.Model3DGenerator = Model3DGeneratorType.Meshy;  // or Tripo
});
```

#### Option 2: Use Local/Self-Hosted Models

```csharp
using Nexo.Adapters.Assets;

services.AddNexoOrchestration();
services.AddAssetGenerators(options =>
{
    options.ImageGenerator = ImageGeneratorType.Local;  // Stable Diffusion, Ollama, LocalAI
    options.AudioGenerator = AudioGeneratorType.Local;  // Bark local, Piper, Coqui
    options.Model3DGenerator = Model3DGeneratorType.Local;  // Custom endpoints
});
```

#### Option 3: Use Echo Placeholders (Testing)

```csharp
services.AddAssetGenerators(options =>
{
    // Uses Echo generators (no API calls, for testing)
});
```

## Available Generators

### Image Generation

**Cloud APIs:**
- **DALL-E 3** (`DalleImageGenerator`): OpenAI's DALL-E 3 for high-quality image generation

**Local/Self-Hosted:**
- **Stable Diffusion** (`LocalImageGenerator`): Automatic1111 or ComfyUI (default: `http://localhost:7860`)
- **Ollama** (`LocalImageGenerator`): Local Ollama instance with image generation models
- **LocalAI** (`LocalImageGenerator`): OpenAI-compatible local API server
- **Custom** (`LocalImageGenerator`): Any custom endpoint with OpenAI-compatible format

**Testing:**
- **Echo** (`EchoImageGenerator`): Placeholder for testing (no API calls)

### Audio Generation

**Cloud APIs:**
- **Suno** (`SunoAudioGenerator`): Music and sound effect generation
- **Bark** (`BarkAudioGenerator`): Music, sound effects, and speech synthesis
- **ElevenLabs** (`ElevenLabsAudioGenerator`): High-quality speech synthesis

**Local/Self-Hosted:**
- **Bark Local** (`LocalAudioGenerator`): Self-hosted Bark instance
- **Piper TTS** (`LocalAudioGenerator`): Fast, local text-to-speech
- **Coqui TTS** (`LocalAudioGenerator`): Open-source TTS with multiple voices
- **Custom** (`LocalAudioGenerator`): Custom audio generation endpoint

**Testing:**
- **Echo** (`EchoAudioGenerator`): Placeholder for testing

### 3D Model Generation

**Cloud APIs:**
- **Meshy** (`MeshyModelGenerator`): Text-to-3D and image-to-3D generation
- **Tripo** (`TripoModelGenerator`): Text-to-3D and image-to-3D generation

**Local/Self-Hosted:**
- **Custom** (`LocalModel3DGenerator`): Any custom 3D generation endpoint

**Testing:**
- **Echo** (`EchoModel3DGenerator`): Placeholder for testing

## Usage Examples

### Image Generation

```csharp
var imageGenerator = serviceProvider.GetRequiredService<IImageGenerator>();
var result = await imageGenerator.GenerateAsync(new ImageGenerationRequest
{
    Prompt = "A futuristic cityscape at sunset",
    Size = ImageSize.Large,
    Style = "photorealistic"
});
```

### Audio Generation

```csharp
var audioGenerator = serviceProvider.GetRequiredService<IAudioGenerator>();

// Music
var music = await audioGenerator.GenerateAsync(new AudioGenerationRequest
{
    Prompt = "Epic orchestral battle music",
    Type = AudioType.Music,
    DurationSeconds = 30,
    Genre = "orchestral",
    Bpm = 120
});

// Speech
var speech = await audioGenerator.GenerateSpeechAsync(new SpeechGenerationRequest
{
    Text = "Welcome to the game!",
    VoiceId = "default",
    Speed = 1.0,
    Pitch = 1.0
});
```

### 3D Model Generation

```csharp
var modelGenerator = serviceProvider.GetRequiredService<IModel3DGenerator>();

// Text-to-3D
var model = await modelGenerator.GenerateFromTextAsync(new Model3DGenerationRequest
{
    Prompt = "A medieval sword with ornate hilt",
    Quality = ModelQuality.High,
    OutputFormat = Model3DFormat.GLB,
    GenerateTextures = true
});

// Image-to-3D
var modelFromImage = await modelGenerator.GenerateFromImageAsync(
    imagePath: "reference.png",
    request: new Model3DGenerationRequest
    {
        Prompt = "Convert this image to a 3D model",
        Quality = ModelQuality.Medium,
        OutputFormat = Model3DFormat.GLB
    }
);
```

## API Notes

### Cloud APIs

#### ElevenLabs
- Optimized for speech synthesis
- Supports multiple voices
- High-quality multilingual speech
- API endpoint: `https://api.elevenlabs.io/v1/`

#### Suno
- Specialized for music generation
- Supports genre and BPM control
- Does not support speech synthesis
- API endpoint: `https://api.suno.ai/v1/` (verify actual endpoint)

#### Bark
- Supports music, sound effects, and speech
- Versatile audio generation
- API endpoint: `https://api.bark.ai/v1/` (verify actual endpoint)

#### Meshy
- Text-to-3D and image-to-3D
- Supports multiple formats (GLB, GLTF, FBX, OBJ, USD)
- Texture generation support
- API endpoint: `https://api.meshy.ai/v2/`

#### Tripo
- Text-to-3D and image-to-3D
- High-quality model generation
- API endpoint: `https://api.tripo3d.ai/v1/` (verify actual endpoint)

### Local/Self-Hosted APIs

#### Stable Diffusion (Automatic1111)
- **Setup**: Install Automatic1111 WebUI or ComfyUI
- **Default URL**: `http://localhost:7860`
- **API Endpoint**: `/sdapi/v1/txt2img`
- **Features**: Text-to-image, image-to-image, inpainting, controlnet
- **Configuration**:
  ```json
  {
    "LocalImageGenerator": {
      "BaseUrl": "http://localhost:7860",
      "Provider": "StableDiffusion"
    }
  }
  ```

#### Stable Diffusion (ComfyUI)
- **Setup**: Install ComfyUI with API enabled
- **Default URL**: `http://localhost:8188`
- **API Endpoint**: `/api/v1/predict`
- **Note**: ComfyUI uses a different API format (workflows)

#### Ollama
- **Setup**: Install Ollama and pull image generation models
- **Default URL**: `http://localhost:11434`
- **API Endpoint**: `/api/generate`
- **Note**: Requires specific image generation models (e.g., `llava`)

#### LocalAI
- **Setup**: Install LocalAI and configure image generation backend
- **Default URL**: `http://localhost:8080`
- **API Endpoint**: `/v1/images/generations` (OpenAI-compatible)
- **Features**: OpenAI-compatible API, supports multiple backends

#### Bark Local
- **Setup**: Run Bark server locally (Docker or Python)
- **Default URL**: `http://localhost:8000`
- **API Endpoint**: `/generate` (audio) or `/tts` (speech)
- **Features**: Music, sound effects, and speech synthesis

#### Piper TTS
- **Setup**: Install Piper TTS server
- **Default URL**: `http://localhost:5000`
- **API Endpoint**: `/api/tts`
- **Features**: Fast, lightweight TTS with multiple voices

#### Coqui TTS
- **Setup**: Install Coqui TTS server
- **Default URL**: `http://localhost:5002`
- **API Endpoint**: `/api/tts`
- **Features**: High-quality TTS with emotion and style control

#### Custom Endpoints
- **Format**: OpenAI-compatible or custom JSON
- **Response Formats Supported**:
  - `image_url`: URL to download image
  - `image_base64`: Base64-encoded image
  - `audio_url`: URL to download audio
  - `audio_base64`: Base64-encoded audio
  - `model_url`: URL to download 3D model
  - `model_base64`: Base64-encoded 3D model
  - `task_id`: For async operations (poll `/tasks/{task_id}`)

## Error Handling

All generators include comprehensive error handling:
- API key validation
- Network error retries
- Rate limit handling
- Timeout management
- Invalid response detection

## Testing

Use Echo generators for unit testing without making real API calls:

```csharp
services.AddAssetGenerators(); // Defaults to Echo generators
```

## Local Setup Examples

### Setting Up Stable Diffusion (Automatic1111)

1. Install Automatic1111 WebUI:
   ```bash
   git clone https://github.com/AUTOMATIC1111/stable-diffusion-webui
   cd stable-diffusion-webui
   ./webui.sh --api
   ```

2. Configure Nexo:
   ```json
   {
     "LocalImageGenerator": {
       "BaseUrl": "http://localhost:7860",
       "Provider": "StableDiffusion"
     }
   }
   ```

3. Register in code:
   ```csharp
   services.AddAssetGenerators(options =>
   {
       options.ImageGenerator = ImageGeneratorType.Local;
   });
   ```

### Setting Up Bark Local

1. Run Bark server:
   ```bash
   docker run -p 8000:8000 bark-server
   # or
   python -m bark.server --port 8000
   ```

2. Configure Nexo:
   ```json
   {
     "LocalAudioGenerator": {
       "BaseUrl": "http://localhost:8000",
       "Provider": "Bark"
     }
   }
   ```

3. Register in code:
   ```csharp
   services.AddAssetGenerators(options =>
   {
       options.AudioGenerator = AudioGeneratorType.Local;
   });
   ```

### Setting Up Custom 3D Generation Endpoint

1. Create your 3D generation API (e.g., Flask/FastAPI):
   ```python
   @app.post("/text-to-3d")
   async def text_to_3d(request: GenerationRequest):
       # Your generation logic
       return {
           "task_id": "12345",
           "status": "processing"
       }
   
   @app.get("/tasks/{task_id}")
   async def get_task(task_id: str):
       # Return status or completed model
       return {
           "status": "completed",
           "model_url": "http://localhost:8080/models/model.glb"
       }
   ```

2. Configure Nexo:
   ```json
   {
     "LocalModel3DGenerator": {
       "BaseUrl": "http://localhost:8080"
     }
   }
   ```

3. Register in code:
   ```csharp
   services.AddAssetGenerators(options =>
   {
       options.Model3DGenerator = Model3DGeneratorType.Local;
   });
   ```

## Storage

Generated assets are saved to temporary files by default. Use `IAssetStorage` to persist them:

```csharp
services.AddSingleton<IAssetStorage, LocalAssetStorage>();
```

Configure storage path in `appsettings.json`:

```json
{
  "AssetStorage": {
    "BasePath": "/path/to/assets"
  }
}
```

