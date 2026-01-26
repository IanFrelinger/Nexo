using Microsoft.Extensions.Logging;
using Nexo.Agents.UniversalTester.Adapters;
using Nexo.Agents.UniversalTester.Configuration;
using Nexo.Agents.UniversalTester.Models;
using Nexo.Tests.GeospatialVisual.Rendering;

namespace Nexo.Tests.GeospatialVisual.Adapters;

/// <summary>
/// Adapter for rendering 3D models (OBJ, glTF, GLB) to images for visual validation.
/// Uses pure .NET rendering with ImageSharp (no browser dependencies).
/// Works on all platforms including Unity (.NET Standard 2.0 compatible).
/// </summary>
public class Model3DRendererAdapter : ITargetAdapter
{
    private readonly ILogger<Model3DRendererAdapter>? _logger;
    private string? _modelPath;
    private byte[]? _cachedScreenshot;

    public TargetType TargetType => TargetType.Game; // Closest match for 3D rendering
    public bool IsConnected => _modelPath != null && File.Exists(_modelPath);

    public Model3DRendererAdapter(ILogger<Model3DRendererAdapter>? logger = null)
    {
        _logger = logger;
    }

    public async Task ConnectAsync(string target, CancellationToken ct = default)
    {
        // Target is the path to the 3D model file or directory
        if (!Directory.Exists(target) && !File.Exists(target))
        {
            throw new ArgumentException($"Model path does not exist: {target}", nameof(target));
        }

        // Find the model file
        if (File.Exists(target))
        {
            var ext = Path.GetExtension(target).ToLowerInvariant();
            if (ext == ".obj" || ext == ".gltf" || ext == ".glb")
            {
                _modelPath = target;
            }
            else
            {
                throw new ArgumentException($"Unsupported model format: {ext}. Supported: .obj, .gltf, .glb", nameof(target));
            }
        }
        else if (Directory.Exists(target))
        {
            // Find first model file in directory
            var modelExtensions = new[] { ".obj", ".gltf", ".glb" };
            foreach (var ext in modelExtensions)
            {
                var files = Directory.GetFiles(target, $"*{ext}", SearchOption.TopDirectoryOnly);
                if (files.Length > 0)
                {
                    _modelPath = files[0];
                    break;
                }
            }

            if (_modelPath == null)
            {
                throw new FileNotFoundException($"No model file found in {target}");
            }
        }

        // Pre-render the model for faster screenshot capture
        await Task.Run(() =>
        {
            try
            {
                if (Path.GetExtension(_modelPath).ToLowerInvariant() == ".obj")
                {
                    _cachedScreenshot = PureNetModelRenderer.RenderObjToImage(
                        _modelPath,
                        width: 1920,
                        height: 1080,
                        style: RenderStyle.Solid);
                }
                else
                {
                    // For glTF/GLB, we'll need to add support later or convert to OBJ
                    // For now, create a placeholder
                    _logger?.LogWarning("glTF/GLB rendering not yet implemented, using placeholder");
                    _cachedScreenshot = CreatePlaceholderImage();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error rendering model, using placeholder");
                _cachedScreenshot = CreatePlaceholderImage();
            }
        }, ct);

        _logger?.LogInformation("Model3DRendererAdapter connected to {Target}", _modelPath);
    }

    public Task DisconnectAsync(CancellationToken ct = default)
    {
        _modelPath = null;
        _cachedScreenshot = null;
        return Task.CompletedTask;
    }

    public Task<byte[]?> CaptureScreenshotAsync(CancellationToken ct = default)
    {
        if (_cachedScreenshot != null)
        {
            return Task.FromResult<byte[]?>(_cachedScreenshot);
        }

        if (_modelPath != null && File.Exists(_modelPath))
        {
            try
            {
                var ext = Path.GetExtension(_modelPath).ToLowerInvariant();
                if (ext == ".obj")
                {
                    var screenshot = PureNetModelRenderer.RenderObjToImage(
                        _modelPath,
                        width: 1920,
                        height: 1080,
                        style: RenderStyle.Solid);
                    _cachedScreenshot = screenshot;
                    return Task.FromResult<byte[]?>(screenshot);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error capturing screenshot");
            }
        }

        return Task.FromResult<byte[]?>(CreatePlaceholderImage());
    }

    private byte[] CreatePlaceholderImage()
    {
        using var img = new SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32>(1920, 1080);
        img.Mutate(ctx => ctx.Fill(SixLabors.ImageSharp.Color.FromRgb(26, 26, 26)));
        using var ms = new MemoryStream();
        img.SaveAsPng(ms);
        return ms.ToArray();
    }

    public Task<string?> GetStructureAsync(CancellationToken ct = default)
    {
        // Return OBJ file content as structure
        if (_modelPath != null && File.Exists(_modelPath))
        {
            try
            {
                return Task.FromResult<string?>(File.ReadAllText(_modelPath));
            }
            catch
            {
                // Ignore
            }
        }
        return Task.FromResult<string?>(null);
    }

    public Task<string?> GetAccessibilityTreeAsync(CancellationToken ct = default)
    {
        // Not applicable for 3D model viewer
        return Task.FromResult<string?>(null);
    }

    public Task<IReadOnlyList<InteractiveElement>> GetInteractiveElementsAsync(CancellationToken ct = default)
    {
        // 3D viewer has minimal interactivity - camera controls
        return Task.FromResult<IReadOnlyList<InteractiveElement>>(new[]
        {
            new InteractiveElement
            {
                Id = "camera-controls",
                Type = "button",
                Label = "Camera Controls",
                Bounds = new BoundingBox { X = 0, Y = 0, Width = 1920, Height = 1080 }
            }
        });
    }

    public Task<GameState?> GetGameStateAsync(CancellationToken ct = default) => Task.FromResult<GameState?>(null);
    public Task<IReadOnlyList<GameObject>> GetVisibleObjectsAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<GameObject>>(Array.Empty<GameObject>());
    public Task<PlayerState?> GetPlayerStateAsync(CancellationToken ct = default) => Task.FromResult<PlayerState?>(null);
    public Task<ApiResponse?> GetLastApiResponseAsync(CancellationToken ct = default) => Task.FromResult<ApiResponse?>(null);
    public Task<IReadOnlyList<ApiEndpoint>> GetAvailableEndpointsAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<ApiEndpoint>>(Array.Empty<ApiEndpoint>());
    public Task<string?> GetTerminalOutputAsync(CancellationToken ct = default) => Task.FromResult<string?>(null);
    public Task<string?> GetCurrentPromptAsync(CancellationToken ct = default) => Task.FromResult<string?>(null);
    public Task<IReadOnlyList<string>> GetConsoleLogAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
    public Task<IReadOnlyList<string>> GetErrorsAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
    public Task<IReadOnlyList<string>> GetWarningsAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
    public Task<PerformanceMetrics?> GetPerformanceMetricsAsync(CancellationToken ct = default) => Task.FromResult<PerformanceMetrics?>(null);
    public Task<string?> GetCurrentUrlAsync(CancellationToken ct = default) => Task.FromResult<string?>(_modelPath);
    public Task<string?> GetWindowTitleAsync(CancellationToken ct = default) => Task.FromResult<string?>("3D Model Viewer (Pure .NET)");

    public Task<string?> ExecuteActionAsync(TestAction action, CancellationToken ct = default)
    {
        // Actions not supported in pure .NET renderer
        return Task.FromResult<string?>("Action not supported in pure .NET renderer");
    }
    
    public Task<PerformanceMetrics?> GetPerformanceAsync(CancellationToken ct = default)
    {
        return Task.FromResult<PerformanceMetrics?>(null);
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
    }
}
