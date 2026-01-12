using Microsoft.Extensions.Logging;
using Nexo.Agents.UniversalTester.Configuration;
using Nexo.Agents.UniversalTester.Models;
using System.Diagnostics;
using System.Text;

namespace Nexo.Agents.UniversalTester.Adapters;

/// <summary>
/// CLI adapter for testing command-line applications.
/// </summary>
public class CliAdapter : ITargetAdapter
{
    private Process? _process;
    private readonly StringBuilder _output = new();
    private readonly ILogger<CliAdapter>? _logger;
    
    public TargetType TargetType => TargetType.Cli;
    public bool IsConnected => _process != null && !_process.HasExited;
    
    public CliAdapter(ILogger<CliAdapter>? logger = null)
    {
        _logger = logger;
    }
    
    public async Task ConnectAsync(string target, CancellationToken ct = default)
    {
        // target format: "cli://dotnet run" or "cli://npm test"
        var command = target.Replace("cli://", "");
        var parts = command.Split(' ', 2);
        
        var startInfo = new ProcessStartInfo
        {
            FileName = parts[0],
            Arguments = parts.Length > 1 ? parts[1] : "",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        
        _process = Process.Start(startInfo);
        
        if (_process == null)
            throw new InvalidOperationException($"Failed to start process: {command}");
        
        _process.OutputDataReceived += (_, e) => { if (e.Data != null) _output.AppendLine(e.Data); };
        _process.ErrorDataReceived += (_, e) => { if (e.Data != null) _output.AppendLine($"[ERROR] {e.Data}"); };
        
        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();
        
        _logger?.LogInformation("Started CLI process: {Command}", command);
        
        await Task.Delay(1000, ct); // Give process time to start
    }
    
    public Task DisconnectAsync(CancellationToken ct = default)
    {
        _process?.Kill();
        _process?.Dispose();
        _process = null;
        _output.Clear();
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
        // CLI doesn't have interactive elements in the same way
        return Task.FromResult<IReadOnlyList<InteractiveElement>>(Array.Empty<InteractiveElement>());
    }
    
    public async Task<string?> ExecuteActionAsync(TestAction action, CancellationToken ct = default)
    {
        if (_process == null || _process.HasExited) return "Not connected";
        
        try
        {
            switch (action.Type)
            {
                case ActionType.ExecuteCommand:
                    if (!string.IsNullOrEmpty(action.Command))
                    {
                        await _process.StandardInput.WriteLineAsync(action.Command);
                        await Task.Delay(500, ct); // Wait for output
                        return "Command executed";
                    }
                    break;
                    
                case ActionType.SendInput:
                    if (!string.IsNullOrEmpty(action.InputValue))
                    {
                        await _process.StandardInput.WriteAsync(action.InputValue);
                        await Task.Delay(200, ct);
                        return "Input sent";
                    }
                    break;
                    
                case ActionType.KeyPress:
                    // Send Enter key
                    await _process.StandardInput.WriteLineAsync("");
                    return "Key pressed";
            }
            
            return "Action executed";
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }
    
    public Task<string?> GetTerminalOutputAsync(CancellationToken ct = default) =>
        Task.FromResult<string?>(_output.ToString());
    
    public Task<string?> GetCurrentPromptAsync(CancellationToken ct = default) =>
        Task.FromResult<string?>(null);
    
    public Task<IReadOnlyList<string>> GetConsoleLogAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<string>>(_output.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries));
    
    public Task<IReadOnlyList<string>> GetErrorsAsync(CancellationToken ct = default)
    {
        var errors = _output.ToString()
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Where(line => line.Contains("[ERROR]", StringComparison.OrdinalIgnoreCase) || 
                          line.Contains("error", StringComparison.OrdinalIgnoreCase))
            .ToList();
        return Task.FromResult<IReadOnlyList<string>>(errors);
    }
    
    public Task<IReadOnlyList<string>> GetWarningsAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
    
    public Task<PerformanceMetrics?> GetPerformanceAsync(CancellationToken ct = default) =>
        Task.FromResult<PerformanceMetrics?>(null);
    
    public Task<string?> GetCurrentUrlAsync(CancellationToken ct = default) =>
        Task.FromResult<string?>(null);
    
    public Task<string?> GetWindowTitleAsync(CancellationToken ct = default) =>
        Task.FromResult<string?>(null);
    
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
    
    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
    }
}
