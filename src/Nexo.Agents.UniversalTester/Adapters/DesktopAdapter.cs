using Microsoft.Extensions.Logging;
using Nexo.Agents.UniversalTester.Configuration;
using Nexo.Agents.UniversalTester.Models;
using System.Diagnostics;

namespace Nexo.Agents.UniversalTester.Adapters;

/// <summary>
/// Desktop adapter for native desktop applications.
/// 
/// NOTE: This is a stub implementation. Full implementation requires:
/// - Windows: System.Windows.Automation (UIA) or FlaUI library
/// - macOS: AppleScript or Accessibility API
/// - Linux: AT-SPI (Assistive Technology Service Provider Interface)
/// 
/// Target format: "process://Notepad" or executable file path
/// </summary>
public class DesktopAdapter : ITargetAdapter
{
    private Process? _process;
    private readonly ILogger<DesktopAdapter>? _logger;
    private readonly List<string> _errors = new();
    private ITargetAdapter? _platformAdapter;
    
    public TargetType TargetType => TargetType.DesktopApp;
    public bool IsConnected => _platformAdapter?.IsConnected ?? (_process != null && !_process.HasExited);
    
    public DesktopAdapter(ILogger<DesktopAdapter>? logger = null)
    {
        _logger = logger;
    }
    
    public async Task ConnectAsync(string target, CancellationToken ct = default)
    {
        // If we're on Windows and the Windows automation adapter is available, delegate to it.
        if (OperatingSystem.IsWindows())
        {
            var delegated = await TryConnectWindowsAdapterAsync(target, ct);
            if (delegated)
            {
                return;
            }
        }

        try
        {
            // Parse target format: "process://Notepad" or file path
            if (target.StartsWith("process://", StringComparison.OrdinalIgnoreCase))
            {
                var processName = target["process://".Length..];
                var processes = Process.GetProcessesByName(processName);
                if (processes.Length > 0)
                {
                    _process = processes[0];
                    _logger?.LogInformation("Connected to existing process: {Process}", processName);
                }
                else
                {
                    throw new InvalidOperationException($"Process '{processName}' not found");
                }
            }
            else if (File.Exists(target))
            {
                // Launch executable
                _process = Process.Start(new ProcessStartInfo
                {
                    FileName = target,
                    UseShellExecute = true
                });
                
                if (_process == null)
                    throw new InvalidOperationException($"Failed to start process: {target}");
                
                // Wait a bit for process to initialize
                await Task.Delay(1000, ct);
                _logger?.LogInformation("Launched desktop app: {Target}", target);
            }
            else
            {
                throw new FileNotFoundException($"Target not found: {target}");
            }
            
            if (!IsConnected)
            {
                _errors.Add("Process exited immediately after launch");
                throw new InvalidOperationException("Failed to connect to desktop application");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to connect to desktop app: {Target}", target);
            _errors.Add($"Connection failed: {ex.Message}");
            throw;
        }
    }
    
    public Task DisconnectAsync(CancellationToken ct = default)
    {
        if (_platformAdapter != null)
        {
            var adapter = _platformAdapter;
            _platformAdapter = null;
            return adapter.DisconnectAsync(ct);
        }

        // Don't kill the process - it might be user's application
        // Just mark as disconnected
        _process = null;
        return Task.CompletedTask;
    }
    
    public Task<byte[]?> CaptureScreenshotAsync(CancellationToken ct = default)
    {
        if (_platformAdapter != null) return _platformAdapter.CaptureScreenshotAsync(ct);

        // TODO: Implement screenshot capture using platform-specific APIs
        // Windows: BitBlt or GDI+
        // macOS: CGWindowListCreateImage
        // Linux: X11 screenshot APIs
        _logger?.LogWarning("Screenshot capture not implemented for desktop apps");
        return Task.FromResult<byte[]?>(null);
    }
    
    public Task<string?> GetStructureAsync(CancellationToken ct = default)
    {
        if (_platformAdapter != null) return _platformAdapter.GetStructureAsync(ct);

        // TODO: Implement UI tree extraction using UI Automation
        return Task.FromResult<string?>(null);
    }
    
    public Task<string?> GetAccessibilityTreeAsync(CancellationToken ct = default)
    {
        if (_platformAdapter != null) return _platformAdapter.GetAccessibilityTreeAsync(ct);

        // TODO: Implement accessibility tree using platform APIs
        return Task.FromResult<string?>(null);
    }
    
    public Task<IReadOnlyList<InteractiveElement>> GetInteractiveElementsAsync(CancellationToken ct = default)
    {
        if (_platformAdapter != null) return _platformAdapter.GetInteractiveElementsAsync(ct);

        // TODO: Implement element discovery using UI Automation
        _logger?.LogWarning("Interactive element discovery not implemented for desktop apps");
        return Task.FromResult<IReadOnlyList<InteractiveElement>>(Array.Empty<InteractiveElement>());
    }
    
    public Task<string?> ExecuteActionAsync(TestAction action, CancellationToken ct = default)
    {
        if (_platformAdapter != null) return _platformAdapter.ExecuteActionAsync(action, ct);

        // TODO: Implement action execution using UI Automation
        // This requires:
        // - Finding elements by automation ID, name, or coordinates
        // - Sending input events (clicks, keyboard, etc.)
        // - Waiting for UI updates
        _logger?.LogWarning("Action execution not implemented for desktop apps. Action: {ActionType}", action.Type);
        return Task.FromResult<string?>("Desktop automation requires UI Automation implementation");
    }
    
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
        _platformAdapter != null ? _platformAdapter.GetErrorsAsync(ct) : Task.FromResult<IReadOnlyList<string>>(_errors);
    
    public Task<IReadOnlyList<string>> GetWarningsAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
    
    public Task<PerformanceMetrics?> GetPerformanceAsync(CancellationToken ct = default)
    {
        if (_platformAdapter != null) return _platformAdapter.GetPerformanceAsync(ct);

        if (_process != null && !_process.HasExited)
        {
            try
            {
                _process.Refresh();
                return Task.FromResult<PerformanceMetrics?>(new PerformanceMetrics
                {
                    CpuPercent = _process.TotalProcessorTime.TotalMilliseconds / 1000.0, // Approximate
                    MemoryUsageMb = _process.WorkingSet64 / (1024 * 1024)
                });
            }
            catch
            {
                // Process may have exited
            }
        }
        return Task.FromResult<PerformanceMetrics?>(null);
    }
    
    public Task<string?> GetCurrentUrlAsync(CancellationToken ct = default) =>
        Task.FromResult<string?>(null);
    
    public Task<string?> GetWindowTitleAsync(CancellationToken ct = default)
    {
        if (_platformAdapter != null) return _platformAdapter.GetWindowTitleAsync(ct);

        if (_process != null && !_process.HasExited)
        {
            try
            {
                // TODO: Get window title using platform-specific APIs
                // Windows: GetWindowText via P/Invoke
                // macOS: NSWindow title
                // Linux: X11 window properties
                return Task.FromResult<string?>(_process.ProcessName);
            }
            catch
            {
                return Task.FromResult<string?>(null);
            }
        }
        return Task.FromResult<string?>(null);
    }
    
    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
    }

    private async Task<bool> TryConnectWindowsAdapterAsync(string target, CancellationToken ct)
    {
        try
        {
            // Load optional windows adapter assembly if present.
            // Assembly: Nexo.Agents.UniversalTester.Windows
            var typeName = "Nexo.Agents.UniversalTester.Windows.Adapters.WindowsDesktopAdapter, Nexo.Agents.UniversalTester.Windows";
            var type = Type.GetType(typeName);
            if (type == null) return false;

            var adapter = Activator.CreateInstance(type) as ITargetAdapter;
            if (adapter == null) return false;

            await adapter.ConnectAsync(target, ct);
            _platformAdapter = adapter;
            _logger?.LogInformation("Using Windows desktop UI automation adapter");
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Windows desktop automation adapter failed; falling back");
            return false;
        }
    }
}
