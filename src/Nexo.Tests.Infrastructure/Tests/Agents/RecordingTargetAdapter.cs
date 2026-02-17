using Nexo.Agents.UniversalTester.Adapters;
using Nexo.Agents.UniversalTester.Configuration;
using Nexo.Agents.UniversalTester.Models;

namespace Nexo.Tests.Infrastructure.Tests.Agents;

/// <summary>
/// Test double that records every ExecuteActionAsync(TestAction) call for assertions.
/// Validates that the testing agent produces the correct clicks, keystrokes, and other actions.
/// </summary>
public sealed class RecordingTargetAdapter : ITargetAdapter
{
    private readonly List<TestAction> _executedActions = new();
    private readonly byte[]? _screenshotOverride;

    public RecordingTargetAdapter(byte[]? screenshotOverride = null)
    {
        _screenshotOverride = screenshotOverride;
    }

    public TargetType TargetType => TargetType.DesktopApp;
    public bool IsConnected => true;

    public IReadOnlyList<TestAction> ExecutedActions => _executedActions;

    public Task ConnectAsync(string target, CancellationToken ct = default) => Task.CompletedTask;
    public Task DisconnectAsync(CancellationToken ct = default) => Task.CompletedTask;

    public Task<byte[]?> CaptureScreenshotAsync(CancellationToken ct = default) =>
        Task.FromResult<byte[]?>(_screenshotOverride ?? Array.Empty<byte>());

    public Task<string?> GetStructureAsync(CancellationToken ct = default) => Task.FromResult<string?>(null);
    public Task<string?> GetAccessibilityTreeAsync(CancellationToken ct = default) => Task.FromResult<string?>(null);
    public Task<IReadOnlyList<InteractiveElement>> GetInteractiveElementsAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<InteractiveElement>>(Array.Empty<InteractiveElement>());
    public Task<GameState?> GetGameStateAsync(CancellationToken ct = default) => Task.FromResult<GameState?>(null);
    public Task<IReadOnlyList<GameObject>> GetVisibleObjectsAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<GameObject>>(Array.Empty<GameObject>());
    public Task<PlayerState?> GetPlayerStateAsync(CancellationToken ct = default) => Task.FromResult<PlayerState?>(null);
    public Task<ApiResponse?> GetLastApiResponseAsync(CancellationToken ct = default) => Task.FromResult<ApiResponse?>(null);
    public Task<IReadOnlyList<ApiEndpoint>> GetAvailableEndpointsAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<ApiEndpoint>>(Array.Empty<ApiEndpoint>());
    public Task<string?> GetTerminalOutputAsync(CancellationToken ct = default) => Task.FromResult<string?>(null);
    public Task<string?> GetCurrentPromptAsync(CancellationToken ct = default) => Task.FromResult<string?>(null);
    public Task<IReadOnlyList<string>> GetConsoleLogAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
    public Task<IReadOnlyList<string>> GetErrorsAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
    public Task<IReadOnlyList<string>> GetWarningsAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
    public Task<PerformanceMetrics?> GetPerformanceAsync(CancellationToken ct = default) => Task.FromResult<PerformanceMetrics?>(null);
    public Task<string?> GetCurrentUrlAsync(CancellationToken ct = default) => Task.FromResult<string?>(null);
    public Task<string?> GetWindowTitleAsync(CancellationToken ct = default) => Task.FromResult<string?>(null);

    public Task<string?> ExecuteActionAsync(TestAction action, CancellationToken ct = default)
    {
        _executedActions.Add(action);
        return Task.FromResult<string?>("OK");
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
