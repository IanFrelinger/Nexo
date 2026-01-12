using Microsoft.Extensions.Logging;
using Nexo.Agents.UniversalTester.Configuration;
using Nexo.Agents.UniversalTester.Models;

namespace Nexo.Agents.UniversalTester.Adapters;

/// <summary>
/// Desktop adapter for native desktop applications.
/// Uses Windows UI Automation or similar platform-specific APIs.
/// </summary>
public class DesktopAdapter : ITargetAdapter
{
    private readonly ILogger<DesktopAdapter>? _logger;
    
    public TargetType TargetType => TargetType.DesktopApp;
    public bool IsConnected { get; private set; }
    
    public DesktopAdapter(ILogger<DesktopAdapter>? logger = null)
    {
        _logger = logger;
    }
    
    public Task ConnectAsync(string target, CancellationToken ct = default)
    {
        // target format: "process://Notepad" or file path
        IsConnected = true;
        _logger?.LogInformation("Connected to desktop app: {Target}", target);
        return Task.CompletedTask;
    }
    
    public Task DisconnectAsync(CancellationToken ct = default)
    {
        IsConnected = false;
        return Task.CompletedTask;
    }
    
    public Task<byte[]?> CaptureScreenshotAsync(CancellationToken ct = default) =>
        Task.FromResult<byte[]?>(null);
    
    public Task<string?> GetStructureAsync(CancellationToken ct = default) =>
        Task.FromResult<string?>(null);
    
    public Task<string?> GetAccessibilityTreeAsync(CancellationToken ct = default) =>
        Task.FromResult<string?>(null);
    
    public Task<IReadOnlyList<InteractiveElement>> GetInteractiveElementsAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<InteractiveElement>>(Array.Empty<InteractiveElement>());
    
    public Task<string?> ExecuteActionAsync(TestAction action, CancellationToken ct = default) =>
        Task.FromResult<string?>("Desktop automation not yet implemented");
    
    public Task<GameState?> GetGameStateAsync(CancellationToken ct = default) =>
        Task.FromResult<GameState?>(null);
    
    public Task<IReadOnlyList<GameObject>> GetVisibleObjectsAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<GameObject>>(Array.Empty<GameObject>());
    
    public Task<PlayerState?> GetPlayerStateAsync(CancellationToken ct = default) =>
        Task.FromResult<PlayerState?>(null);
    
    public Task<ApiResponse?> GetLastApiResponseAsync(CancellationToken ct = default) =>
        Task.FromResult<ApiResponse?>(null);
    
    public Task<IReadOnlyList<ApiEndpoint>> GetAvailableEndpointsAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<ApiEndpoint>>(Array.Empty<ApiEndpoint>());
    
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
        Task.FromResult<string?>(null);
    
    public Task<string?> GetWindowTitleAsync(CancellationToken ct = default) =>
        Task.FromResult<string?>(null);
    
    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
    }
}
