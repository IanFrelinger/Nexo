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
    private string? _processName; // For macOS/Linux: target app name for focus/automation
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
                    _processName = _process.ProcessName;
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
                _processName = Path.GetFileNameWithoutExtension(target); // e.g. "Nexo.Guide"
                if (_process != null && !_process.HasExited)
                    _processName = _process.ProcessName; // Prefer actual process name
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
        _processName = null;
        return Task.CompletedTask;
    }

    public Task<byte[]?> CaptureScreenshotAsync(CancellationToken ct = default)
    {
        if (_platformAdapter != null) return _platformAdapter.CaptureScreenshotAsync(ct);

        if (OperatingSystem.IsMacOS())
            return CaptureScreenshotMacOsAsync(ct);
        if (OperatingSystem.IsLinux())
            return CaptureScreenshotLinuxAsync(ct);

        _logger?.LogWarning("Screenshot capture not implemented for this platform");
        return Task.FromResult<byte[]?>(null);
    }

    private Task<byte[]?> CaptureScreenshotMacOsAsync(CancellationToken ct)
    {
        try
        {
            var path = Path.Combine(Path.GetTempPath(), "nexo-ut-cap-" + Guid.NewGuid().ToString("N") + ".png");
            var psi = new ProcessStartInfo
            {
                FileName = "screencapture",
                ArgumentList = { "-x", "-t", "png", path },
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var p = Process.Start(psi);
            if (p == null) return Task.FromResult<byte[]?>(null);
            p.WaitForExit(5000);
            if (p.ExitCode != 0 || !File.Exists(path))
            {
                try { File.Delete(path); } catch { }
                return Task.FromResult<byte[]?>(null);
            }
            var bytes = File.ReadAllBytes(path);
            try { File.Delete(path); } catch { }
            return Task.FromResult<byte[]?>(bytes);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "macOS screencapture failed");
            return Task.FromResult<byte[]?>(null);
        }
    }

    private Task<byte[]?> CaptureScreenshotLinuxAsync(CancellationToken ct)
    {
        try
        {
            var path = Path.Combine(Path.GetTempPath(), "nexo-ut-cap-" + Guid.NewGuid().ToString("N") + ".png");
            var psi = new ProcessStartInfo
            {
                FileName = "scrot",
                ArgumentList = { "-o", path },
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var p = Process.Start(psi);
            if (p == null) return Task.FromResult<byte[]?>(null);
            p.WaitForExit(5000);
            if (p.ExitCode != 0 || !File.Exists(path))
            {
                try { File.Delete(path); } catch { }
                return Task.FromResult<byte[]?>(null);
            }
            var bytes = File.ReadAllBytes(path);
            try { File.Delete(path); } catch { }
            return Task.FromResult<byte[]?>(bytes);
        }
        catch
        {
            _logger?.LogWarning("Linux scrot screenshot not available");
            return Task.FromResult<byte[]?>(null);
        }
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

        if (OperatingSystem.IsMacOS())
            return ExecuteActionMacOsAsync(action, ct);
        if (OperatingSystem.IsLinux())
            return ExecuteActionLinuxAsync(action, ct);

        _logger?.LogWarning("Action execution not implemented for this platform. Action: {ActionType}", action.Type);
        return Task.FromResult<string?>("Desktop automation not implemented for this platform");
    }

    private async Task<string?> ExecuteActionMacOsAsync(TestAction action, CancellationToken ct)
    {
        var processName = _processName ?? _process?.ProcessName ?? "Nexo.Guide";
        switch (action.Type)
        {
            case ActionType.Wait:
                await Task.Delay(action.Duration ?? TimeSpan.FromMilliseconds(500), ct);
                return "Waited";
            case ActionType.Click:
            case ActionType.DoubleClick:
            case ActionType.RightClick:
                return ExecuteClickMacOs(processName, action);
            case ActionType.Type:
            case ActionType.Fill:
                return ExecuteTypeMacOs(processName, action);
            case ActionType.KeyPress:
                return ExecuteKeyPressMacOs(processName, action);
            case ActionType.SwitchWindow:
                return RunOsascript($"tell application \"System Events\" to set frontmost of first process whose name is \"{processName}\" to true");
            default:
                return $"Unsupported action: {action.Type}";
        }
    }

    private string? ExecuteClickMacOs(string processName, TestAction action)
    {
        int x, y;
        if (action.Coordinates != null)
        {
            x = (int)action.Coordinates.X;
            y = (int)action.Coordinates.Y;
        }
        else if (TryParseCoordinates(action.Selector ?? action.ElementId, out x, out y))
        {
            // Target was "x,y" from vision
        }
        else
            return "No coordinates or selector for click";

        var script = $@"
tell application ""System Events"" to set frontmost of first process whose name is ""{processName}"" to true
delay 0.2
tell application ""System Events"" to click at {{{x}, {y}}}
";
        return RunOsascript(script);
    }

    private string? ExecuteTypeMacOs(string processName, TestAction action)
    {
        var value = action.InputValue ?? "";
        value = value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        var script = $@"
tell application ""System Events"" to set frontmost of first process whose name is ""{processName}"" to true
delay 0.2
tell application ""System Events"" to keystroke ""{value}""
";
        return RunOsascript(script);
    }

    private string? ExecuteKeyPressMacOs(string processName, TestAction action)
    {
        var key = action.Key?.Trim() ?? "";
        var code = KeyCodeToMacCode(key.ToLowerInvariant());
        var script = $@"
tell application ""System Events"" to set frontmost of first process whose name is ""{processName}"" to true
delay 0.1
tell application ""System Events"" to key code {code}
";
        return RunOsascript(script);
    }

    private static int KeyCodeToMacCode(string key)
    {
        return key switch
        {
            "enter" or "return" => 36,
            "tab" => 48,
            "escape" or "esc" => 53,
            "backspace" or "delete" => 51,
            _ => 0
        };
    }

    private static bool TryParseCoordinates(string? target, out int x, out int y)
    {
        x = y = 0;
        if (string.IsNullOrWhiteSpace(target)) return false;
        var parts = target.Trim().Split(',');
        if (parts.Length != 2) return false;
        if (!int.TryParse(parts[0].Trim(), out x) || !int.TryParse(parts[1].Trim(), out y)) return false;
        return true;
    }

    private string? RunOsascript(string script)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "osascript",
                ArgumentList = { "-e", script },
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true
            };
            using var p = Process.Start(psi);
            if (p == null) return "Failed to start osascript";
            var err = p.StandardError.ReadToEnd();
            p.WaitForExit(5000);
            return p.ExitCode == 0 ? "OK" : (string.IsNullOrWhiteSpace(err) ? "osascript failed" : err);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "osascript failed");
            return ex.Message;
        }
    }

    private async Task<string?> ExecuteActionLinuxAsync(TestAction action, CancellationToken ct)
    {
        switch (action.Type)
        {
            case ActionType.Wait:
                await Task.Delay(action.Duration ?? TimeSpan.FromMilliseconds(500), ct);
                return "Waited";
            default:
                _logger?.LogWarning("Linux desktop automation only supports Wait (install xdotool for click/type)");
                return "Linux action not implemented";
        }
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
