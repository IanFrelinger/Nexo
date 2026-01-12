using Microsoft.Extensions.Logging;
using Nexo.Agents.UniversalTester.Configuration;
using Nexo.Agents.UniversalTester.Models;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Nexo.Agents.UniversalTester.Adapters;

/// <summary>
/// API adapter for testing REST APIs.
/// </summary>
public class ApiAdapter : ITargetAdapter
{
    private HttpClient? _httpClient;
    private string? _baseUrl;
    private ApiResponse? _lastResponse;
    private readonly List<ApiEndpoint> _discoveredEndpoints = new();
    private readonly ILogger<ApiAdapter>? _logger;
    
    public TargetType TargetType => TargetType.Api;
    public bool IsConnected => _httpClient != null;
    
    public ApiAdapter(ILogger<ApiAdapter>? logger = null)
    {
        _logger = logger;
    }
    
    public async Task ConnectAsync(string target, CancellationToken ct = default)
    {
        // target format: "api://https://api.example.com"
        _baseUrl = target.Replace("api://", "").TrimEnd('/');
        _httpClient = new HttpClient { BaseAddress = new Uri(_baseUrl) };
        
        // Try to discover endpoints from OpenAPI/Swagger
        try
        {
            var spec = await _httpClient.GetStringAsync("/openapi.json", ct);
            _discoveredEndpoints.AddRange(ParseOpenApiSpec(spec));
            _logger?.LogInformation("Discovered {Count} endpoints from OpenAPI spec", _discoveredEndpoints.Count);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Could not discover endpoints from OpenAPI spec");
        }
    }
    
    public Task DisconnectAsync(CancellationToken ct = default)
    {
        _httpClient?.Dispose();
        _httpClient = null;
        return Task.CompletedTask;
    }
    
    public Task<byte[]?> CaptureScreenshotAsync(CancellationToken ct = default) =>
        Task.FromResult<byte[]?>(null);
    
    public Task<string?> GetStructureAsync(CancellationToken ct = default) =>
        Task.FromResult<string?>(null);
    
    public Task<string?> GetAccessibilityTreeAsync(CancellationToken ct = default) =>
        Task.FromResult<string?>(null);
    
    public Task<IReadOnlyList<InteractiveElement>> GetInteractiveElementsAsync(CancellationToken ct = default)
    {
        // Convert endpoints to "interactive elements"
        var elements = _discoveredEndpoints.Select(e => new InteractiveElement
        {
            Id = $"{e.Method}:{e.Path}",
            Type = "endpoint",
            Label = $"{e.Method} {e.Path}",
            Description = e.Description,
            IsVisible = true,
            IsEnabled = true,
            PossibleActions = new[] { "api_call" }
        }).ToList();
        
        return Task.FromResult<IReadOnlyList<InteractiveElement>>(elements);
    }
    
    public async Task<string?> ExecuteActionAsync(TestAction action, CancellationToken ct = default)
    {
        if (_httpClient == null || action.Type != ActionType.ApiRequest)
            return "Invalid action for API";
        
        try
        {
            var method = new HttpMethod(action.HttpMethod ?? "GET");
            var request = new HttpRequestMessage(method, action.Endpoint);
            
            if (action.RequestBody != null)
                request.Content = new StringContent(action.RequestBody, Encoding.UTF8, "application/json");
            
            if (action.Headers != null)
                foreach (var (key, value) in action.Headers)
                    request.Headers.TryAddWithoutValidation(key, value);
            
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var response = await _httpClient.SendAsync(request, ct);
            sw.Stop();
            
            var body = await response.Content.ReadAsStringAsync(ct);
            
            _lastResponse = new ApiResponse
            {
                StatusCode = (int)response.StatusCode,
                Body = body,
                Headers = response.Headers.ToDictionary(h => h.Key, h => string.Join(", ", h.Value)),
                Duration = sw.Elapsed
            };
            
            return $"{response.StatusCode}: {body.Substring(0, Math.Min(200, body.Length))}";
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "API request failed");
            return $"Error: {ex.Message}";
        }
    }
    
    public Task<ApiResponse?> GetLastApiResponseAsync(CancellationToken ct = default) =>
        Task.FromResult(_lastResponse);
    
    public Task<IReadOnlyList<ApiEndpoint>> GetAvailableEndpointsAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<ApiEndpoint>>(_discoveredEndpoints);
    
    public Task<GameState?> GetGameStateAsync(CancellationToken ct = default) =>
        Task.FromResult<GameState?>(null);
    
    public Task<IReadOnlyList<GameObject>> GetVisibleObjectsAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<GameObject>>(Array.Empty<GameObject>());
    
    public Task<PlayerState?> GetPlayerStateAsync(CancellationToken ct = default) =>
        Task.FromResult<PlayerState?>(null);
    
    public Task<string?> GetTerminalOutputAsync(CancellationToken ct = default) =>
        Task.FromResult<string?>(null);
    
    public Task<string?> GetCurrentPromptAsync(CancellationToken ct = default) =>
        Task.FromResult<string?>(null);
    
    public Task<IReadOnlyList<string>> GetConsoleLogAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
    
    public Task<IReadOnlyList<string>> GetErrorsAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
    
    public Task<IReadOnlyList<string>> GetWarningsAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
    
    public Task<PerformanceMetrics?> GetPerformanceAsync(CancellationToken ct = default) =>
        Task.FromResult<PerformanceMetrics?>(null);
    
    public Task<string?> GetCurrentUrlAsync(CancellationToken ct = default) =>
        Task.FromResult(_baseUrl);
    
    public Task<string?> GetWindowTitleAsync(CancellationToken ct = default) =>
        Task.FromResult<string?>(null);
    
    private static List<ApiEndpoint> ParseOpenApiSpec(string json)
    {
        var endpoints = new List<ApiEndpoint>();
        try
        {
            var doc = JsonDocument.Parse(json);
            var paths = doc.RootElement.GetProperty("paths");
            
            foreach (var path in paths.EnumerateObject())
            {
                foreach (var method in path.Value.EnumerateObject())
                {
                    endpoints.Add(new ApiEndpoint
                    {
                        Method = method.Name.ToUpperInvariant(),
                        Path = path.Name,
                        Description = method.Value.TryGetProperty("summary", out var summary) 
                            ? summary.GetString() 
                            : null
                    });
                }
            }
        }
        catch
        {
            // Failed to parse, return empty
        }
        
        return endpoints;
    }
    
    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
    }
}
