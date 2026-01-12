using Nexo.Agents.UniversalTester.Configuration;
using Nexo.Agents.UniversalTester.Models;

namespace Nexo.Agents.UniversalTester.Adapters;

/// <summary>
/// Adapter for interacting with different types of applications.
/// Implementations handle web, games, desktop, APIs, CLIs, etc.
/// Methods return null/empty for unsupported capabilities.
/// </summary>
public interface ITargetAdapter : IAsyncDisposable
{
    TargetType TargetType { get; }
    
    // Lifecycle
    Task ConnectAsync(string target, CancellationToken ct = default);
    Task DisconnectAsync(CancellationToken ct = default);
    bool IsConnected { get; }
    
    // Perception - Visual
    Task<byte[]?> CaptureScreenshotAsync(CancellationToken ct = default);
    
    // Perception - Structure
    Task<string?> GetStructureAsync(CancellationToken ct = default);
    Task<string?> GetAccessibilityTreeAsync(CancellationToken ct = default);
    Task<IReadOnlyList<InteractiveElement>> GetInteractiveElementsAsync(CancellationToken ct = default);
    
    // Perception - Game
    Task<GameState?> GetGameStateAsync(CancellationToken ct = default);
    Task<IReadOnlyList<GameObject>> GetVisibleObjectsAsync(CancellationToken ct = default);
    Task<PlayerState?> GetPlayerStateAsync(CancellationToken ct = default);
    
    // Perception - API
    Task<ApiResponse?> GetLastApiResponseAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ApiEndpoint>> GetAvailableEndpointsAsync(CancellationToken ct = default);
    
    // Perception - CLI
    Task<string?> GetTerminalOutputAsync(CancellationToken ct = default);
    Task<string?> GetCurrentPromptAsync(CancellationToken ct = default);
    
    // Perception - Universal
    Task<IReadOnlyList<string>> GetConsoleLogAsync(CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetErrorsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetWarningsAsync(CancellationToken ct = default);
    Task<PerformanceMetrics?> GetPerformanceAsync(CancellationToken ct = default);
    Task<string?> GetCurrentUrlAsync(CancellationToken ct = default);
    Task<string?> GetWindowTitleAsync(CancellationToken ct = default);
    
    // Action Execution
    Task<string?> ExecuteActionAsync(TestAction action, CancellationToken ct = default);
}
