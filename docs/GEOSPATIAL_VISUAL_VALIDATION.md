# Geospatial Visual Validation Guide

## Overview

This guide explains how to use the UniversalTesterAgent with visual reasoning models to validate the rendering quality of geospatial application outputs (3D meshes, terrain, buildings, etc.).

## Architecture

The visual validation system consists of:

1. **Model3DRendererAdapter** - Renders 3D models (OBJ, glTF, GLB) to images using a web-based viewer
2. **Visual Validation Tests** - Test cases that generate geo outputs and validate their visual appearance
3. **Visual Reasoning Integration** - Uses vision-capable LLMs (GPT-4o, Claude 3 Opus) to analyze rendered images

## How It Works

### Step 1: Generate Geo Outputs

The tests generate 3D models using the geo application:
- Terrain meshes (OBJ format)
- Building meshes (OBJ format)
- Complete world bundles (multiple OBJ files)

### Step 2: Render to Images

The `Model3DRendererAdapter`:
- Creates a temporary web server
- Serves a Three.js-based 3D viewer
- Loads the 3D model in the viewer
- Captures screenshots using Playwright

### Step 3: Visual Reasoning Validation

The validation process:
- Captures a screenshot of the rendered model
- Sends the image to a vision-capable LLM (GPT-4o, Claude 3 Opus)
- The model analyzes:
  - Geometry correctness (no missing faces, holes, corruption)
  - Lighting and shadows
  - Proportions and scale
  - Visual artifacts (flickering, z-fighting, texture issues)
  - Overall visual quality

### Step 4: Report Results

The validation returns:
- Pass/fail status
- Detailed reasoning
- List of detected issues with severity
- Confidence score

## Usage

### Running Visual Validation Tests

```bash
# Run all visual validation tests
dotnet test src/Nexo.Tests.GeospatialVisual/Nexo.Tests.GeospatialVisual.csproj

# Run specific test
dotnet test src/Nexo.Tests.GeospatialVisual/Nexo.Tests.GeospatialVisual.csproj \
  --filter "FullyQualifiedName~VisualValidation_TerrainMesh"
```

### Test Examples

#### Terrain Mesh Validation

```csharp
[Fact]
public async Task VisualValidation_TerrainMesh_ShouldRenderCorrectly()
{
    // Generate terrain
    var outputFile = Path.Combine(_testOutputDir, "terrain.obj");
    await command.BoundsToObjAsync(...);
    
    // Render and validate
    var adapter = new Model3DRendererAdapter();
    await adapter.ConnectAsync(outputFile);
    var screenshot = await adapter.CaptureScreenshotAsync();
    var result = await ValidateVisualRenderingAsync(screenshot, ...);
    
    result.Passed.Should().BeTrue();
}
```

#### Building Mesh Validation

```csharp
[Fact]
public async Task VisualValidation_BuildingMesh_ShouldRenderCorrectly()
{
    // Similar pattern for building meshes
}
```

#### Complete World Bundle Validation

```csharp
[Fact]
public async Task VisualValidation_WorldBundle_ShouldRenderCompleteScene()
{
    // Validates complete scenes with terrain + buildings
}
```

## Visual Reasoning Models

### Supported Models

The system works with vision-capable models:
- **GPT-4o** (OpenAI) - Recommended, excellent visual reasoning
- **GPT-4 Vision** (OpenAI) - Good visual analysis
- **Claude 3 Opus** (Anthropic) - Excellent for detailed analysis
- **Claude 3 Sonnet** (Anthropic) - Good balance of speed and quality

### Configuration

Set environment variables:

```bash
# For OpenAI
export OPENAI_API_KEY="your-key"
export OPENAI_MODEL="gpt-4o"  # or "gpt-4-vision-preview"

# For Azure OpenAI
export AZURE_OPENAI_ENDPOINT="https://your-endpoint.openai.azure.com"
export AZURE_OPENAI_API_KEY="your-key"
export AZURE_OPENAI_DEPLOYMENT="gpt-4o"
```

## What Gets Validated

### Geometry Quality
- ✅ No missing faces or holes
- ✅ Proper mesh topology
- ✅ No degenerate triangles
- ✅ Correct vertex positions

### Visual Appearance
- ✅ Realistic proportions
- ✅ Proper scale
- ✅ Correct lighting
- ✅ Appropriate shadows

### Artifacts Detection
- ❌ Z-fighting (overlapping faces)
- ❌ Texture stretching or distortion
- ❌ Flickering or rendering glitches
- ❌ Incorrect normals (dark/light faces)

### Scene Composition
- ✅ Terrain and buildings properly aligned
- ✅ Realistic world appearance
- ✅ Appropriate detail level
- ✅ No obvious errors or corruption

## Integration with UniversalTesterAgent

The visual validation can also be used with the full UniversalTesterAgent workflow:

```csharp
var config = new UniversalTesterConfig
{
    Target = "path/to/model.obj",
    Goal = "Validate that the terrain mesh renders correctly with proper geometry and lighting",
    Depth = TestingDepth.Standard,
    MaxDuration = TimeSpan.FromMinutes(5)
};

var agent = new UniversalTesterAgent(providerFactory, logger);
var report = await agent.ExecuteAsync(config, context, ct);
```

## Future Enhancements

### Vision API Integration

Currently, the system uses text-based prompts. Future enhancements will:
- Directly pass image bytes to vision APIs
- Use OpenAI's Chat API with `image_url` content
- Support Claude's vision API
- Add support for local vision models (LLaVA, etc.)

### Advanced Validation

- **Comparison Testing** - Compare rendered output to reference images
- **Regression Detection** - Detect visual regressions between versions
- **Quality Metrics** - Quantitative metrics (triangle quality, texture resolution)
- **Multi-Angle Validation** - Validate from multiple camera angles

### Performance Optimization

- **Caching** - Cache rendered screenshots
- **Parallel Validation** - Validate multiple models simultaneously
- **Incremental Testing** - Only validate changed outputs

## Troubleshooting

### Web Server Not Starting

If the web server fails to start:
- Ensure port 8080 is available
- Check firewall settings
- Try a different port by modifying `_webServerPort`

### Playwright Issues

If Playwright fails:
- Run `playwright install chromium` to install browser
- Check that Playwright is properly installed

### Model Not Loading

If models don't load in the viewer:
- Check that OBJ/glTF files are valid
- Verify file paths are correct
- Check browser console for errors (in non-headless mode)

### Vision API Errors

If vision API calls fail:
- Verify API keys are set correctly
- Check that the model supports vision
- Ensure image data is properly encoded

## Best Practices

1. **Use Deterministic Providers** - Use "echo" provider for consistent test results
2. **Set Appropriate Timeouts** - Model loading and rendering can take time
3. **Validate Key Scenarios** - Focus on critical rendering paths
4. **Store Reference Images** - Keep reference screenshots for comparison
5. **Use Appropriate Models** - GPT-4o or Claude 3 Opus for best results

## Related Documentation

- [UniversalTesterAgent README](../src/Nexo.Agents.UniversalTester/README.md)
- [Geospatial E2E Tests](./GEOSPATIAL_E2E_TESTS.md)
- [Geospatial Unit Tests](./GEOSPATIAL_UNIT_TESTS.md)
- [Multi-Environment Testing](./MULTI_ENV_TESTING.md)
