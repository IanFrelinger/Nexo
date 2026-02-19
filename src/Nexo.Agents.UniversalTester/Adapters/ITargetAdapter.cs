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
    /// <summary>Target type (WebApp, Game, DesktopApp, Api, Cli).</summary>
    TargetType TargetType { get; }
    
    /// <summary>Connects to the target (URL, process name, tcp address, etc.).</summary>
    Task ConnectAsync(string target, CancellationToken ct = default);
    /// <summary>Disconnects and releases resources.</summary>
    Task DisconnectAsync(CancellationToken ct = default);
    /// <summary>True if connected to the target.</summary>
    bool IsConnected { get; }
    
    /// <summary>Captures a screenshot; returns null if unsupported or not connected.</summary>
    Task<byte[]?> CaptureScreenshotAsync(CancellationToken ct = default);
    
    /// <summary>Gets DOM/HTML or structure snapshot; null for non-web targets.</summary>
    Task<string?> GetStructureAsync(CancellationToken ct = default);
    /// <summary>Gets accessibility tree; null if not available.</summary>
    Task<string?> GetAccessibilityTreeAsync(CancellationToken ct = default);
    /// <summary>Gets interactive UI elements (buttons, inputs, links); empty for unsupported.</summary>
    Task<IReadOnlyList<InteractiveElement>> GetInteractiveElementsAsync(CancellationToken ct = default);
    
    /// <summary>Gets game state (scene, level, paused, etc.); null for non-game targets.</summary>
    Task<GameState?> GetGameStateAsync(CancellationToken ct = default);
    /// <summary>Gets visible game objects; empty for non-game or unsupported.</summary>
    Task<IReadOnlyList<GameObject>> GetVisibleObjectsAsync(CancellationToken ct = default);
    /// <summary>Gets player state (health, inventory); null for non-game targets.</summary>
    Task<PlayerState?> GetPlayerStateAsync(CancellationToken ct = default);
    
    /// <summary>Gets last API response; null for non-API targets.</summary>
    Task<ApiResponse?> GetLastApiResponseAsync(CancellationToken ct = default);
    /// <summary>Gets available API endpoints; empty for non-API.</summary>
    Task<IReadOnlyList<ApiEndpoint>> GetAvailableEndpointsAsync(CancellationToken ct = default);
    
    /// <summary>Gets terminal output; null for non-CLI targets.</summary>
    Task<string?> GetTerminalOutputAsync(CancellationToken ct = default);
    /// <summary>Gets current CLI prompt; null for non-CLI.</summary>
    Task<string?> GetCurrentPromptAsync(CancellationToken ct = default);
    
    /// <summary>Gets console log entries; empty if not captured.</summary>
    Task<IReadOnlyList<string>> GetConsoleLogAsync(CancellationToken ct = default);
    /// <summary>Gets error messages; empty if none.</summary>
    Task<IReadOnlyList<string>> GetErrorsAsync(CancellationToken ct = default);
    /// <summary>Gets warning messages; empty if none.</summary>
    Task<IReadOnlyList<string>> GetWarningsAsync(CancellationToken ct = default);
    /// <summary>Gets performance metrics (FPS, memory, etc.); null if unavailable.</summary>
    Task<PerformanceMetrics?> GetPerformanceAsync(CancellationToken ct = default);
    /// <summary>Gets current URL; null for non-web.</summary>
    Task<string?> GetCurrentUrlAsync(CancellationToken ct = default);
    /// <summary>Gets window/tab title; null if unavailable.</summary>
    Task<string?> GetWindowTitleAsync(CancellationToken ct = default);
    
    /// <summary>Executes the given action; returns status message or error.</summary>
    Task<string?> ExecuteActionAsync(TestAction action, CancellationToken ct = default);
}
