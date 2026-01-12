using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Nexo.Agents.UniversalTester.Configuration;
using Nexo.Agents.UniversalTester.Models;
using System.Text.Json;

namespace Nexo.Agents.UniversalTester.Adapters;

/// <summary>
/// Web adapter using Playwright for browser automation.
/// </summary>
public class WebAdapter : ITargetAdapter
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private IPage? _page;
    private readonly List<string> _consoleLog = new();
    private readonly List<string> _errors = new();
    private readonly ILogger<WebAdapter>? _logger;
    
    public TargetType TargetType => TargetType.WebApp;
    public bool IsConnected => _page != null;
    
    public WebAdapter(ILogger<WebAdapter>? logger = null)
    {
        _logger = logger;
    }
    
    public async Task ConnectAsync(string target, CancellationToken ct = default)
    {
        try
        {
            _playwright = await Playwright.CreateAsync();
            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
            _page = await _browser.NewPageAsync();
            
            // Capture console and errors
            _page.Console += (_, msg) => _consoleLog.Add($"[{msg.Type}] {msg.Text}");
            _page.PageError += (_, error) => _errors.Add(error);
            
            await _page.GotoAsync(target, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
            _logger?.LogInformation("Connected to web target: {Target}", target);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to connect to web target: {Target}", target);
            throw;
        }
    }
    
    public async Task DisconnectAsync(CancellationToken ct = default)
    {
        if (_browser != null)
        {
            await _browser.DisposeAsync();
            _browser = null;
        }
        _playwright?.Dispose();
        _playwright = null;
        _page = null;
    }
    
    public async Task<byte[]?> CaptureScreenshotAsync(CancellationToken ct = default)
    {
        if (_page == null) return null;
        return await _page.ScreenshotAsync(new PageScreenshotOptions { FullPage = true });
    }
    
    public async Task<string?> GetStructureAsync(CancellationToken ct = default)
    {
        if (_page == null) return null;
        return await _page.ContentAsync();
    }
    
    public Task<string?> GetAccessibilityTreeAsync(CancellationToken ct = default)
    {
        // Accessibility API is deprecated in Playwright
        return Task.FromResult<string?>(null);
    }
    
    public async Task<IReadOnlyList<InteractiveElement>> GetInteractiveElementsAsync(CancellationToken ct = default)
    {
        if (_page == null) return Array.Empty<InteractiveElement>();
        
        var elements = await _page.EvaluateAsync<List<InteractiveElement>>(@"() => {
            const result = [];
            const selectors = 'button, a, input, select, textarea, [role=""button""], [onclick], [tabindex]';
            
            document.querySelectorAll(selectors).forEach((el, i) => {
                const rect = el.getBoundingClientRect();
                const style = window.getComputedStyle(el);
                
                result.push({
                    id: el.id || `element-${i}`,
                    type: el.tagName.toLowerCase(),
                    label: el.textContent?.trim()?.substring(0, 100) || el.getAttribute('aria-label') || el.getAttribute('placeholder') || '',
                    description: el.getAttribute('title') || el.getAttribute('aria-description') || '',
                    bounds: { x: rect.x, y: rect.y, width: rect.width, height: rect.height },
                    isVisible: rect.width > 0 && rect.height > 0 && style.visibility !== 'hidden' && style.display !== 'none',
                    isEnabled: !el.disabled,
                    currentValue: el.value || '',
                    possibleActions: ['click', 'hover', el.tagName === 'INPUT' ? 'type' : null].filter(Boolean)
                });
            });
            
            return result;
        }");
        
        return elements ?? new List<InteractiveElement>();
    }
    
    public async Task<string?> ExecuteActionAsync(TestAction action, CancellationToken ct = default)
    {
        if (_page == null) return "Not connected";
        
        try
        {
            switch (action.Type)
            {
                case ActionType.Click:
                    if (!string.IsNullOrEmpty(action.Selector))
                        await _page.ClickAsync(action.Selector!);
                    else if (action.Coordinates != null)
                        await _page.Mouse.ClickAsync((float)action.Coordinates.X, (float)action.Coordinates.Y);
                    return "Clicked";
                    
                case ActionType.Type:
                    if (!string.IsNullOrEmpty(action.Selector))
                        await _page.FillAsync(action.Selector!, action.InputValue ?? "");
                    return "Typed";
                    
                case ActionType.Navigate:
                    if (!string.IsNullOrEmpty(action.InputValue))
                        await _page.GotoAsync(action.InputValue!, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
                    return "Navigated";
                    
                case ActionType.Scroll:
                    if (!string.IsNullOrEmpty(action.Selector))
                        await _page.EvaluateAsync($"document.querySelector('{action.Selector}')?.scrollIntoView()");
                    return "Scrolled";
                    
                case ActionType.Hover:
                    if (!string.IsNullOrEmpty(action.Selector))
                        await _page.HoverAsync(action.Selector!);
                    return "Hovered";
                    
                case ActionType.KeyPress:
                    if (!string.IsNullOrEmpty(action.Key))
                        await _page.Keyboard.PressAsync(action.Key!);
                    return "Key pressed";
                    
                case ActionType.Wait:
                    await Task.Delay(action.Duration ?? TimeSpan.FromSeconds(1), ct);
                    return "Waited";
                    
                default:
                    return $"Unsupported action: {action.Type}";
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Action execution failed: {ActionType}", action.Type);
            return $"Error: {ex.Message}";
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
        Task.FromResult<IReadOnlyList<string>>(_consoleLog);
    
    public Task<IReadOnlyList<string>> GetErrorsAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<string>>(_errors);
    
    public Task<IReadOnlyList<string>> GetWarningsAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
    
    public Task<PerformanceMetrics?> GetPerformanceAsync(CancellationToken ct = default) =>
        Task.FromResult<PerformanceMetrics?>(null);
    
    public Task<string?> GetCurrentUrlAsync(CancellationToken ct = default)
    {
        if (_page == null) return Task.FromResult<string?>(null);
        return Task.FromResult<string?>(_page.Url);
    }
    
    public async Task<string?> GetWindowTitleAsync(CancellationToken ct = default)
    {
        if (_page == null) return null;
        return await _page.TitleAsync();
    }
    
    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
    }
}
