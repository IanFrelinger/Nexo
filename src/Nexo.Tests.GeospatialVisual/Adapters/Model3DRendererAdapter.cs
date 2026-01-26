using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Nexo.Agents.UniversalTester.Adapters;
using Nexo.Agents.UniversalTester.Configuration;
using Nexo.Agents.UniversalTester.Models;
using System.Diagnostics;
using System.Net;

namespace Nexo.Tests.GeospatialVisual.Adapters;

/// <summary>
/// Adapter for rendering 3D models (OBJ, glTF, GLB) to images for visual validation.
/// Uses a web-based viewer (Three.js) to render models and capture screenshots.
/// </summary>
public class Model3DRendererAdapter : ITargetAdapter, IDisposable
{
    private readonly ILogger<Model3DRendererAdapter>? _logger;
    private Process? _webServer;
    private System.Net.HttpListener? _httpListener;
    private string? _tempWebDir;
    private int _webServerPort = 8080;
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private IPage? _page;

    public TargetType TargetType => TargetType.Game; // Closest match for 3D rendering
    public bool IsConnected => _page != null && _browser != null;

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

        // Create temporary web directory for serving the viewer
        _tempWebDir = Path.Combine(Path.GetTempPath(), $"nexo-viewer-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempWebDir);

        // Copy model files to web directory
        if (File.Exists(target))
        {
            var fileName = Path.GetFileName(target);
            File.Copy(target, Path.Combine(_tempWebDir, fileName), overwrite: true);
        }
        else if (Directory.Exists(target))
        {
            CopyDirectory(target, _tempWebDir);
        }

        // Create HTML viewer with Three.js
        await CreateViewerHtmlAsync(_tempWebDir, target);

        // Start local web server
        await StartWebServerAsync(_tempWebDir, ct);

        // Initialize Playwright for screenshot capture
        _playwright = await Playwright.Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });

        _page = await _browser.NewPageAsync();
        await _page.SetViewportSizeAsync(1920, 1080);

        var url = $"http://localhost:{_webServerPort}/viewer.html";
        await _page.GotoAsync(url, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle,
            Timeout = 30000
        });

        // Wait for model to load and render
        await _page.WaitForTimeoutAsync(3000, ct);
        
        // Wait for Three.js scene to be ready
        try
        {
            await _page.WaitForFunctionAsync("window.scene !== undefined", new PageWaitForFunctionOptions { Timeout = 10000 }, ct);
        }
        catch
        {
            _logger?.LogWarning("Scene not detected, proceeding anyway");
        }

        _logger?.LogInformation("Model3DRendererAdapter connected to {Target}", target);
    }

    public async Task DisconnectAsync(CancellationToken ct = default)
    {
        try
        {
            if (_page != null)
            {
                await _page.CloseAsync();
                _page = null;
            }
            if (_browser != null)
            {
                await _browser.CloseAsync();
                _browser = null;
            }
            _playwright?.Dispose();
            _playwright = null;
            
            if (_webServer != null && !_webServer.HasExited)
            {
                _webServer.Kill();
                _webServer.WaitForExit(5000);
                _webServer = null;
            }
            
            if (_httpListener != null && _httpListener.IsListening)
            {
                _httpListener.Stop();
                _httpListener.Close();
                _httpListener = null;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error during disconnect");
        }
        finally
        {
            if (_tempWebDir != null && Directory.Exists(_tempWebDir))
            {
                try
                {
                    Directory.Delete(_tempWebDir, recursive: true);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }
    }

    public async Task<byte[]?> CaptureScreenshotAsync(CancellationToken ct = default)
    {
        if (_page == null)
            return null;

        return await _page.ScreenshotAsync(new PageScreenshotOptions
        {
            FullPage = false,
            Type = ScreenshotType.Png
        });
    }

    public Task<string?> GetStructureAsync(CancellationToken ct = default)
    {
        // Return HTML content if available
        if (_page != null)
        {
            return _page.ContentAsync();
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
    public Task<string?> GetCurrentUrlAsync(CancellationToken ct = default) => Task.FromResult<string?>($"http://localhost:{_webServerPort}/viewer.html");
    public Task<string?> GetWindowTitleAsync(CancellationToken ct = default) => Task.FromResult<string?>("3D Model Viewer");

    public Task<string?> ExecuteActionAsync(TestAction action, CancellationToken ct = default)
    {
        // Camera controls could be implemented here using JavaScript
        // For now, just return success message
        return Task.FromResult<string?>("Action executed");
    }
    
    public Task<PerformanceMetrics?> GetPerformanceAsync(CancellationToken ct = default)
    {
        // Could extract performance metrics from browser if needed
        return Task.FromResult<PerformanceMetrics?>(null);
    }

    private async Task CreateViewerHtmlAsync(string webDir, string modelPath)
    {
        var modelFile = File.Exists(modelPath) ? Path.GetFileName(modelPath) : FindModelFile(modelPath);
        var modelExtension = Path.GetExtension(modelFile).ToLowerInvariant();

        var html = $@"<!DOCTYPE html>
<html>
<head>
    <title>3D Model Viewer - Visual Validation</title>
    <style>
        body {{ margin: 0; padding: 0; overflow: hidden; background: #1a1a1a; }}
        #canvas-container {{ width: 100vw; height: 100vh; }}
        #info {{ position: absolute; top: 10px; left: 10px; color: white; font-family: monospace; background: rgba(0,0,0,0.7); padding: 10px; border-radius: 5px; }}
    </style>
    <script src=""https://cdnjs.cloudflare.com/ajax/libs/three.js/r128/three.min.js""></script>
    <script src=""https://cdn.jsdelivr.net/npm/three@0.128.0/examples/js/loaders/OBJLoader.js""></script>
    <script src=""https://cdn.jsdelivr.net/npm/three@0.128.0/examples/js/loaders/GLTFLoader.js""></script>
    <script src=""https://cdn.jsdelivr.net/npm/three@0.128.0/examples/js/controls/OrbitControls.js""></script>
</head>
<body>
    <div id=""canvas-container""></div>
    <div id=""info"">Loading model: {modelFile}</div>
    <script>
        let scene, camera, renderer, controls;
        
        function init() {{
            const container = document.getElementById('canvas-container');
            const width = window.innerWidth;
            const height = window.innerHeight;
            
            scene = new THREE.Scene();
            scene.background = new THREE.Color(0x1a1a1a);
            
            camera = new THREE.PerspectiveCamera(75, width / height, 0.1, 1000);
            camera.position.set(0, 50, 100);
            camera.lookAt(0, 0, 0);
            
            renderer = new THREE.WebGLRenderer({{ antialias: true }});
            renderer.setSize(width, height);
            renderer.shadowMap.enabled = true;
            container.appendChild(renderer.domElement);
            
            controls = new THREE.OrbitControls(camera, renderer.domElement);
            controls.enableDamping = true;
            controls.dampingFactor = 0.05;
            
            // Lighting
            const ambientLight = new THREE.AmbientLight(0xffffff, 0.6);
            scene.add(ambientLight);
            
            const directionalLight = new THREE.DirectionalLight(0xffffff, 0.8);
            directionalLight.position.set(50, 100, 50);
            directionalLight.castShadow = true;
            scene.add(directionalLight);
            
            // Grid helper
            const gridHelper = new THREE.GridHelper(200, 20, 0x444444, 0x222222);
            scene.add(gridHelper);
            
            // Load model
            loadModel('{modelFile}');
        }}
        
        function loadModel(filename) {{
            const ext = filename.split('.').pop().toLowerCase();
            
            if (ext === 'obj') {{
                const loader = new THREE.OBJLoader();
                loader.load(filename, (object) => {{
                    object.traverse((child) => {{
                        if (child.isMesh) {{
                            child.material = new THREE.MeshStandardMaterial({{
                                color: 0x888888,
                                metalness: 0.3,
                                roughness: 0.7
                            }});
                            child.castShadow = true;
                            child.receiveShadow = true;
                        }}
                    }});
                    
                    // Center and scale
                    const box = new THREE.Box3().setFromObject(object);
                    const center = box.getCenter(new THREE.Vector3());
                    const size = box.getSize(new THREE.Vector3());
                    const maxDim = Math.max(size.x, size.y, size.z);
                    const scale = 50 / maxDim;
                    
                    object.scale.multiplyScalar(scale);
                    object.position.sub(center.multiplyScalar(scale));
                    
                    scene.add(object);
                    document.getElementById('info').textContent = `Loaded: ${{filename}} (${{object.children.length}} objects)`;
                }});
            }} else if (ext === 'gltf' || ext === 'glb') {{
                const loader = new THREE.GLTFLoader();
                loader.load(filename, (gltf) => {{
                    scene.add(gltf.scene);
                    document.getElementById('info').textContent = `Loaded: ${{filename}}`;
                }});
            }}
        }}
        
        function animate() {{
            requestAnimationFrame(animate);
            controls.update();
            renderer.render(scene, camera);
        }}
        
        init();
        animate();
    </script>
</body>
</html>";

        await File.WriteAllTextAsync(Path.Combine(webDir, "viewer.html"), html);
    }

    private string FindModelFile(string directory)
    {
        var modelExtensions = new[] { ".obj", ".gltf", ".glb" };
        foreach (var ext in modelExtensions)
        {
            var files = Directory.GetFiles(directory, $"*{ext}", SearchOption.TopDirectoryOnly);
            if (files.Length > 0)
                return Path.GetFileName(files[0]);
        }
        throw new FileNotFoundException($"No model file found in {directory}");
    }

    private async Task StartWebServerAsync(string webDir, CancellationToken ct)
    {
        // Use dotnet-serve if available, otherwise use a simple HTTP listener
        try
        {
            // Try dotnet-serve first (if installed as global tool)
            var serveProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = $"serve --directory \"{webDir}\" --port {_webServerPort}",
                    WorkingDirectory = webDir,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            serveProcess.Start();
            await Task.Delay(1000, ct);
            
            // Check if process is still running
            if (!serveProcess.HasExited)
            {
                _webServer = serveProcess;
                _logger?.LogInformation("Web server started using dotnet-serve on port {Port}", _webServerPort);
                return;
            }
        }
        catch
        {
            // Fall through to HTTP listener
        }

        // Fallback: Use HttpListener (built into .NET)
        _httpListener = new System.Net.HttpListener();
        _httpListener.Prefixes.Add($"http://localhost:{_webServerPort}/");
        _httpListener.Start();

        _ = Task.Run(async () =>
        {
            while (!ct.IsCancellationRequested && _httpListener != null && _httpListener.IsListening)
            {
                try
                {
                    var context = await _httpListener.GetContextAsync();
                    var request = context.Request;
                    var response = context.Response;

                    var localPath = request.Url!.LocalPath.TrimStart('/');
                    if (string.IsNullOrEmpty(localPath) || localPath == "/")
                        localPath = "viewer.html";

                    var filePath = Path.Combine(webDir, localPath);
                    
                    if (File.Exists(filePath))
                    {
                        var content = await File.ReadAllBytesAsync(filePath, ct);
                        var contentType = GetContentType(filePath);
                        response.ContentType = contentType;
                        response.ContentLength64 = content.Length;
                        await response.OutputStream.WriteAsync(content, ct);
                    }
                    else
                    {
                        response.StatusCode = 404;
                        var notFound = System.Text.Encoding.UTF8.GetBytes("404 Not Found");
                        response.ContentLength64 = notFound.Length;
                        await response.OutputStream.WriteAsync(notFound, ct);
                    }

                    response.Close();
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error handling HTTP request");
                }
            }
        }, ct);

        await Task.Delay(500, ct); // Wait for server to start
        _logger?.LogInformation("Web server started using HttpListener on port {Port}", _webServerPort);
    }

    private static string GetContentType(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        return ext switch
        {
            ".html" => "text/html",
            ".js" => "application/javascript",
            ".obj" => "text/plain",
            ".gltf" => "model/gltf+json",
            ".glb" => "model/gltf-binary",
            ".png" => "image/png",
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            _ => "application/octet-stream"
        };
    }

    private static void CopyDirectory(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);
        foreach (var file in Directory.GetFiles(sourceDir))
        {
            File.Copy(file, Path.Combine(destDir, Path.GetFileName(file)), overwrite: true);
        }
        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            CopyDirectory(dir, Path.Combine(destDir, Path.GetFileName(dir)));
        }
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
    }
}
