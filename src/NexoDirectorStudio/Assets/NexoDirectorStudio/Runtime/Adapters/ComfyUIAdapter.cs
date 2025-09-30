using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using System.Net.Http;

namespace NexoDirectorStudio.Adapters
{
    /// <summary>
    /// Implementation of ITextureGenAdapter for offline image generation.
    /// Provides texture generation capabilities using ComfyUI.
    /// </summary>
    public sealed class ComfyUIAdapter : ITextureGenAdapter, IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly string _outputPath;
        private bool _disposed;
        
        public string Id => "comfyui";
        public string Name => "ComfyUI Texture Generation Adapter";
        public string Description => "Offline image generation adapter using ComfyUI for game asset creation";
        public bool IsAvailable { get; private set; }
        public DateTime LastHealthCheck { get; private set; }
        
        public ComfyUIAdapter(string baseUrl = "http://localhost:8188", string outputPath = "Assets/Generated/Textures")
        {
            _baseUrl = baseUrl ?? throw new ArgumentNullException(nameof(baseUrl));
            _outputPath = outputPath ?? throw new ArgumentNullException(nameof(outputPath));
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri(_baseUrl);
        }
        
        public async Task<HealthCheckResult> HealthCheckAsync(CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.UtcNow;
            
            try
            {
                var response = await _httpClient.GetAsync("/system_stats", cancellationToken);
                var responseTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
                
                if (response.IsSuccessStatusCode)
                {
                    IsAvailable = true;
                    LastHealthCheck = DateTime.UtcNow;
                    
                    return new HealthCheckResult
                    {
                        IsHealthy = true,
                        Message = "ComfyUI service is available",
                        ResponseTimeMs = (long)responseTime,
                        Details = $"Base URL: {_baseUrl}, Output Path: {_outputPath}"
                    };
                }
                else
                {
                    IsAvailable = false;
                    return new HealthCheckResult
                    {
                        IsHealthy = false,
                        Message = $"ComfyUI service returned status {response.StatusCode}",
                        ResponseTimeMs = (long)responseTime,
                        Details = $"Base URL: {_baseUrl}"
                    };
                }
            }
            catch (HttpRequestException ex)
            {
                IsAvailable = false;
                var responseTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
                
                return new HealthCheckResult
                {
                    IsHealthy = false,
                    Message = $"ComfyUI service is unreachable: {ex.Message}",
                    ResponseTimeMs = (long)responseTime,
                    Details = $"Base URL: {_baseUrl}, Error: {ex.Message}"
                };
            }
            catch (TaskCanceledException)
            {
                IsAvailable = false;
                return new HealthCheckResult
                {
                    IsHealthy = false,
                    Message = "Health check timed out",
                    ResponseTimeMs = (long)(DateTime.UtcNow - startTime).TotalMilliseconds,
                    Details = $"Base URL: {_baseUrl}"
                };
            }
        }
        
        public async Task<InitializationResult> InitializeAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                // Check if ComfyUI is running and accessible
                var response = await _httpClient.GetAsync("/system_stats", cancellationToken);
                
                if (response.IsSuccessStatusCode)
                {
                    // Ensure output directory exists
                    Directory.CreateDirectory(_outputPath);
                    
                    return new InitializationResult
                    {
                        IsSuccessful = true,
                        Message = "ComfyUI adapter initialized successfully",
                        Details = $"Base URL: {_baseUrl}, Output Path: {_outputPath}"
                    };
                }
                else
                {
                    return new InitializationResult
                    {
                        IsSuccessful = false,
                        Message = $"Failed to connect to ComfyUI: {response.StatusCode}",
                        Details = $"Base URL: {_baseUrl}"
                    };
                }
            }
            catch (Exception ex)
            {
                return new InitializationResult
                {
                    IsSuccessful = false,
                    Message = $"Initialization failed: {ex.Message}",
                    Details = $"Base URL: {_baseUrl}, Error: {ex.Message}"
                };
            }
        }
        
        public async Task<TextureSet> GeneratePackAsync(string prompt, string style, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(prompt))
                throw new ArgumentException("Prompt cannot be null or empty", nameof(prompt));
            
            if (!IsAvailable)
            {
                throw new InvalidOperationException("ComfyUI adapter is not available");
            }
            
            try
            {
                var textures = new List<Texture>();
                
                // Generate different types of textures for the pack
                var textureTypes = new[] { "diffuse", "normal", "specular", "roughness" };
                
                foreach (var textureType in textureTypes)
                {
                    var texture = await GenerateSingleTextureAsync(prompt, textureType, style, 512, 512, cancellationToken);
                    textures.Add(texture);
                }
                
                return new TextureSet
                {
                    Name = $"Texture Pack: {prompt}",
                    Description = $"Generated texture pack for: {prompt}",
                    Textures = textures,
                    Style = style,
                    Prompt = prompt
                };
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to generate texture pack: {ex.Message}", ex);
            }
        }
        
        public async Task<Texture> GenerateTextureAsync(string prompt, int width, int height, string style, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(prompt))
                throw new ArgumentException("Prompt cannot be null or empty", nameof(prompt));
            
            if (width <= 0 || height <= 0)
                throw new ArgumentException("Width and height must be positive", nameof(width));
            
            if (!IsAvailable)
            {
                throw new InvalidOperationException("ComfyUI adapter is not available");
            }
            
            try
            {
                return await GenerateSingleTextureAsync(prompt, "diffuse", style, width, height, cancellationToken);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to generate texture: {ex.Message}", ex);
            }
        }
        
        public async Task<TextureCollection> GenerateForAssetsAsync(IReadOnlyList<AssetRequirement> requirements, CancellationToken cancellationToken = default)
        {
            if (requirements == null)
                throw new ArgumentNullException(nameof(requirements));
            
            if (!IsAvailable)
            {
                throw new InvalidOperationException("ComfyUI adapter is not available");
            }
            
            try
            {
                var textureSets = new List<TextureSet>();
                
                foreach (var requirement in requirements)
                {
                    var prompt = GeneratePromptForAsset(requirement);
                    var style = DetermineStyleForAsset(requirement);
                    
                    var textureSet = await GeneratePackAsync(prompt, style, cancellationToken);
                    textureSets.Add(textureSet);
                }
                
                var totalTextures = textureSets.Sum(ts => ts.Textures.Count);
                var totalSize = textureSets.Sum(ts => ts.Textures.Sum(t => t.FileSizeBytes));
                
                return new TextureCollection
                {
                    Name = "Game Asset Textures",
                    Description = $"Generated textures for {requirements.Count} asset requirements",
                    TextureSets = textureSets,
                    TotalTextures = totalTextures,
                    TotalSizeBytes = totalSize
                };
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to generate textures for assets: {ex.Message}", ex);
            }
        }
        
        private async Task<Texture> GenerateSingleTextureAsync(string prompt, string textureType, string style, int width, int height, CancellationToken cancellationToken)
        {
            try
            {
                var workflow = CreateWorkflow(prompt, textureType, style, width, height);
                var response = await SubmitWorkflowAsync(workflow, cancellationToken);
                var imagePath = await DownloadGeneratedImageAsync(response, cancellationToken);
                
                var fileInfo = new FileInfo(imagePath);
                
                return new Texture
                {
                    Name = $"{textureType}_{DateTime.UtcNow:yyyyMMddHHmmss}",
                    Description = $"Generated {textureType} texture for: {prompt}",
                    FilePath = imagePath,
                    Width = width,
                    Height = height,
                    Format = "PNG",
                    FileSizeBytes = fileInfo.Length,
                    Prompt = prompt,
                    Style = style
                };
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to generate {textureType} texture: {ex.Message}", ex);
            }
        }
        
        private static object CreateWorkflow(string prompt, string textureType, string style, int width, int height)
        {
            // Create a ComfyUI workflow for texture generation
            // This is a simplified example - real implementation would be more complex
            return new
            {
                prompt = $"{prompt}, {textureType} texture, {style} style",
                width = width,
                height = height,
                steps = 20,
                cfg_scale = 7.5,
                sampler_name = "DPM++ 2M Karras",
                scheduler = "normal"
            };
        }
        
        private async Task<WorkflowResponse> SubmitWorkflowAsync(object workflow, CancellationToken cancellationToken)
        {
            var json = JsonSerializer.Serialize(workflow);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync("/prompt", content, cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<WorkflowResponse>(responseContent) ?? new WorkflowResponse();
        }
        
        private async Task<string> DownloadGeneratedImageAsync(WorkflowResponse response, CancellationToken cancellationToken)
        {
            // Wait for the workflow to complete
            await Task.Delay(5000, cancellationToken); // Simplified - real implementation would poll for completion
            
            // Download the generated image
            var imageUrl = $"{_baseUrl}/view?filename={response.Filename}";
            var imageResponse = await _httpClient.GetAsync(imageUrl, cancellationToken);
            imageResponse.EnsureSuccessStatusCode();
            
            var imageBytes = await imageResponse.Content.ReadAsByteArrayAsync(cancellationToken);
            var fileName = $"{response.Filename}_{DateTime.UtcNow:yyyyMMddHHmmss}.png";
            var filePath = Path.Combine(_outputPath, fileName);
            
            await File.WriteAllBytesAsync(filePath, imageBytes, cancellationToken);
            
            return filePath;
        }
        
        private static string GeneratePromptForAsset(AssetRequirement requirement)
        {
            return requirement.AssetType switch
            {
                "Weapon" => $"A {requirement.Name.ToLower()} weapon, game asset, high quality, detailed",
                "Enemy" => $"A {requirement.Name.ToLower()} enemy character, game asset, high quality, detailed",
                "Platform" => $"A {requirement.Name.ToLower()} platform, game asset, high quality, detailed",
                "Collectible" => $"A {requirement.Name.ToLower()} collectible item, game asset, high quality, detailed",
                "Environment" => $"A {requirement.Name.ToLower()} environment piece, game asset, high quality, detailed",
                _ => $"A {requirement.Name.ToLower()} game asset, high quality, detailed"
            };
        }
        
        private static string DetermineStyleForAsset(AssetRequirement requirement)
        {
            return requirement.AssetType switch
            {
                "Weapon" => "realistic, detailed, game-ready",
                "Enemy" => "stylized, game-ready, character design",
                "Platform" => "clean, game-ready, architectural",
                "Collectible" => "stylized, shiny, game-ready",
                "Environment" => "atmospheric, game-ready, environmental",
                _ => "game-ready, stylized"
            };
        }
        
        public void Dispose()
        {
            if (!_disposed)
            {
                _httpClient?.Dispose();
                _disposed = true;
            }
        }
    }
    
    /// <summary>
    /// Response from ComfyUI workflow submission.
    /// </summary>
    internal sealed record WorkflowResponse
    {
        public string PromptId { get; init; } = "";
        public string Filename { get; init; } = "";
        public string Status { get; init; } = "";
    }
}
