using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Capturing;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;
using FlaUI.UIA3;
using Microsoft.Extensions.Logging;
using Nexo.Agents.UniversalTester.Adapters;
using Nexo.Agents.UniversalTester.Configuration;
using Nexo.Agents.UniversalTester.Models;
using System.Drawing.Imaging;
using System.Text.Json;

namespace Nexo.Agents.UniversalTester.Windows.Adapters;

/// <summary>
/// Windows desktop UI automation adapter using FlaUI (UIA3).
/// Only available on Windows.
/// </summary>
public sealed class WindowsDesktopAdapter : ITargetAdapter
{
    private Application? _app;
    private UIA3Automation? _automation;
    private Window? _mainWindow;
    private readonly ILogger<WindowsDesktopAdapter>? _logger;
    private readonly List<string> _errors = new();

    public WindowsDesktopAdapter(ILogger<WindowsDesktopAdapter>? logger = null)
    {
        _logger = logger;
    }

    public TargetType TargetType => TargetType.DesktopApp;
    public bool IsConnected => _app != null && !_app.HasExited && _mainWindow != null;

    public Task ConnectAsync(string target, CancellationToken ct = default)
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("WindowsDesktopAdapter can only run on Windows.");

        try
        {
            _automation = new UIA3Automation();

            if (target.StartsWith("process://", StringComparison.OrdinalIgnoreCase))
            {
                var processName = target["process://".Length..].Trim();
                var proc = System.Diagnostics.Process.GetProcessesByName(processName).FirstOrDefault()
                           ?? throw new InvalidOperationException($"Process '{processName}' not found");
                _app = Application.Attach(proc);
            }
            else if (File.Exists(target))
            {
                _app = Application.Launch(target);
            }
            else
            {
                throw new FileNotFoundException($"Target not found: {target}");
            }

            // Wait for main window
            _mainWindow = _app.GetMainWindow(_automation);
            if (_mainWindow == null)
                throw new InvalidOperationException("Could not find main window");

            _logger?.LogInformation("Connected to desktop app (pid={Pid})", _app.ProcessId);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _errors.Add(ex.Message);
            _logger?.LogError(ex, "Failed to connect to desktop app: {Target}", target);
            throw;
        }
    }

    public Task DisconnectAsync(CancellationToken ct = default)
    {
        try
        {
            _mainWindow = null;
            _automation?.Dispose();
            _automation = null;
            _app?.Dispose();
            _app = null;
        }
        catch
        {
            // ignore
        }
        return Task.CompletedTask;
    }

    public Task<byte[]?> CaptureScreenshotAsync(CancellationToken ct = default)
    {
        if (_mainWindow == null) return Task.FromResult<byte[]?>(null);

        try
        {
            using var bmp = Capture.Element(_mainWindow).Bitmap;
            using var ms = new MemoryStream();
            bmp.Save(ms, ImageFormat.Png);
            return Task.FromResult<byte[]?>(ms.ToArray());
        }
        catch (Exception ex)
        {
            _errors.Add($"Screenshot failed: {ex.Message}");
            return Task.FromResult<byte[]?>(null);
        }
    }

    public Task<string?> GetStructureAsync(CancellationToken ct = default)
    {
        if (_mainWindow == null) return Task.FromResult<string?>(null);
        try
        {
            var root = ToTreeNode(_mainWindow, depth: 3);
            return Task.FromResult<string?>(JsonSerializer.Serialize(root, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch
        {
            return Task.FromResult<string?>(null);
        }
    }

    public Task<string?> GetAccessibilityTreeAsync(CancellationToken ct = default) => GetStructureAsync(ct);

    public Task<IReadOnlyList<InteractiveElement>> GetInteractiveElementsAsync(CancellationToken ct = default)
    {
        if (_mainWindow == null) return Task.FromResult<IReadOnlyList<InteractiveElement>>(Array.Empty<InteractiveElement>());

        try
        {
            var els = new List<InteractiveElement>();
            var descendants = _mainWindow.FindAllDescendants();

            foreach (var d in descendants)
            {
                var ctType = d.ControlType;

                var type = ctType.ToString();
                var isInteractive = ctType == ControlType.Button
                                    || ctType == ControlType.Hyperlink
                                    || ctType == ControlType.Edit
                                    || ctType == ControlType.ComboBox
                                    || ctType == ControlType.CheckBox
                                    || ctType == ControlType.RadioButton
                                    || ctType == ControlType.MenuItem
                                    || ctType == ControlType.TabItem;

                if (!isInteractive) continue;

                var props = d.Properties;
                var id = props.AutomationId?.ValueOrDefault ?? props.Name?.ValueOrDefault ?? Guid.NewGuid().ToString("N");
                var name = props.Name?.ValueOrDefault;
                var help = props.HelpText?.ValueOrDefault;
                var bounds = d.BoundingRectangle;

                els.Add(new InteractiveElement
                {
                    Id = id,
                    Type = type,
                    Label = name,
                    Description = help,
                    Bounds = bounds.IsEmpty
                        ? null
                        : new BoundingBox
                        {
                            X = bounds.Left,
                            Y = bounds.Top,
                            Width = bounds.Width,
                            Height = bounds.Height
                        },
                    IsVisible = !bounds.IsEmpty,
                    IsEnabled = props.IsEnabled?.ValueOrDefault ?? true,
                    CurrentValue = TryGetValue(d),
                    PossibleActions = InferActions(ctType)
                });
            }

            return Task.FromResult<IReadOnlyList<InteractiveElement>>(els);
        }
        catch (Exception ex)
        {
            _errors.Add($"GetInteractiveElements failed: {ex.Message}");
            return Task.FromResult<IReadOnlyList<InteractiveElement>>(Array.Empty<InteractiveElement>());
        }
    }

    public Task<string?> ExecuteActionAsync(TestAction action, CancellationToken ct = default)
    {
        if (_mainWindow == null) return Task.FromResult<string?>("Not connected");

        try
        {
            switch (action.Type)
            {
                case ActionType.Click:
                case ActionType.DoubleClick:
                case ActionType.RightClick:
                    return Task.FromResult(ExecuteClick(action));
                case ActionType.Type:
                case ActionType.Fill:
                    return Task.FromResult(ExecuteType(action));
                case ActionType.KeyPress:
                    return Task.FromResult(ExecuteKeyPress(action));
                case ActionType.SwitchWindow:
                    _mainWindow.Focus();
                    return Task.FromResult<string?>("Focused main window");
                case ActionType.Wait:
                    return Task.Delay(action.Duration ?? TimeSpan.FromMilliseconds(250), ct)
                        .ContinueWith(_ => (string?)"Waited", ct);
                default:
                    return Task.FromResult<string?>($"Unsupported action: {action.Type}");
            }
        }
        catch (Exception ex)
        {
            _errors.Add($"ExecuteAction failed: {ex.Message}");
            return Task.FromResult<string?>($"Error: {ex.Message}");
        }
    }

    public Task<GameState?> GetGameStateAsync(CancellationToken ct = default) => Task.FromResult<GameState?>(null);
    public Task<IReadOnlyList<GameObject>> GetVisibleObjectsAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<GameObject>>(Array.Empty<GameObject>());
    public Task<PlayerState?> GetPlayerStateAsync(CancellationToken ct = default) => Task.FromResult<PlayerState?>(null);
    public Task<ApiResponse?> GetLastApiResponseAsync(CancellationToken ct = default) => Task.FromResult<ApiResponse?>(null);
    public Task<IReadOnlyList<ApiEndpoint>> GetAvailableEndpointsAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<ApiEndpoint>>(Array.Empty<ApiEndpoint>());
    public Task<string?> GetTerminalOutputAsync(CancellationToken ct = default) => Task.FromResult<string?>(null);
    public Task<string?> GetCurrentPromptAsync(CancellationToken ct = default) => Task.FromResult<string?>(null);
    public Task<IReadOnlyList<string>> GetConsoleLogAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
    public Task<IReadOnlyList<string>> GetErrorsAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<string>>(_errors);
    public Task<IReadOnlyList<string>> GetWarningsAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
    public Task<PerformanceMetrics?> GetPerformanceAsync(CancellationToken ct = default) => Task.FromResult<PerformanceMetrics?>(null);
    public Task<string?> GetCurrentUrlAsync(CancellationToken ct = default) => Task.FromResult<string?>(null);
    public Task<string?> GetWindowTitleAsync(CancellationToken ct = default) => Task.FromResult<string?>(_mainWindow?.Title);

    public async ValueTask DisposeAsync() => await DisconnectAsync();

    private string? ExecuteClick(TestAction action)
    {
        if (action.Coordinates != null)
        {
            var p = new System.Drawing.Point((int)action.Coordinates.X, (int)action.Coordinates.Y);
            if (action.Type == ActionType.DoubleClick) Mouse.DoubleClick(p);
            else if (action.Type == ActionType.RightClick) Mouse.RightClick(p);
            else Mouse.LeftClick(p);
            return "Clicked by coordinates";
        }

        var el = FindElement(action.Selector ?? action.ElementId);
        if (el == null) return "Element not found";

        el.Focus();
        if (action.Type == ActionType.DoubleClick) el.DoubleClick();
        else if (action.Type == ActionType.RightClick) el.RightClick();
        else el.Click();
        return "Clicked";
    }

    private string? ExecuteType(TestAction action)
    {
        var value = action.InputValue ?? "";
        var el = FindElement(action.Selector ?? action.ElementId);
        if (el == null) return "Element not found";

        el.Focus();

        // Prefer ValuePattern when available
        if (el.Patterns.Value.IsSupported)
        {
            el.Patterns.Value.Pattern.SetValue(value);
            return "Typed (value pattern)";
        }

        Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_A);
        Keyboard.Type(VirtualKeyShort.BACK);
        Keyboard.Type(value);
        return "Typed";
    }

    private string? ExecuteKeyPress(TestAction action)
    {
        var key = action.Key?.Trim();
        if (string.IsNullOrEmpty(key)) return "No key provided";

        // Minimal mapping
        if (key.Equals("enter", StringComparison.OrdinalIgnoreCase)) Keyboard.Type(VirtualKeyShort.RETURN);
        else if (key.Equals("tab", StringComparison.OrdinalIgnoreCase)) Keyboard.Type(VirtualKeyShort.TAB);
        else if (key.Equals("escape", StringComparison.OrdinalIgnoreCase) || key.Equals("esc", StringComparison.OrdinalIgnoreCase)) Keyboard.Type(VirtualKeyShort.ESCAPE);
        else Keyboard.Type(key);

        return "Key pressed";
    }

    private AutomationElement? FindElement(string? selector)
    {
        if (_mainWindow == null || string.IsNullOrWhiteSpace(selector)) return null;

        // Try AutomationId, then Name
        var byId = _mainWindow.FindFirstDescendant(cf => cf.ByAutomationId(selector));
        if (byId != null) return byId;

        var byName = _mainWindow.FindFirstDescendant(cf => cf.ByName(selector));
        return byName;
    }

    private static IReadOnlyList<string> InferActions(ControlType controlType)
    {
        if (controlType == ControlType.Edit) return new[] { "type", "click" };
        if (controlType == ControlType.CheckBox) return new[] { "click", "check", "uncheck" };
        if (controlType == ControlType.ComboBox) return new[] { "click", "select" };
        return new[] { "click" };
    }

    private static string? TryGetValue(AutomationElement el)
    {
        try
        {
            if (el.Patterns.Value.IsSupported)
            {
                return el.Patterns.Value.Pattern.Value.Value;
            }
        }
        catch { /* ignore */ }
        return null;
    }

    private static object ToTreeNode(AutomationElement el, int depth)
    {
        var props = el.Properties;
        var node = new Dictionary<string, object?>
        {
            ["controlType"] = el.ControlType.ToString(),
            ["name"] = props.Name?.ValueOrDefault,
            ["automationId"] = props.AutomationId?.ValueOrDefault,
            ["className"] = props.ClassName?.ValueOrDefault
        };

        if (depth <= 0) return node;

        var children = el.FindAllChildren();
        node["children"] = children.Take(50).Select(c => ToTreeNode(c, depth - 1)).ToList();
        return node;
    }
}

