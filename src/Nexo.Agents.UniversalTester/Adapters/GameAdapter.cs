using Microsoft.Extensions.Logging;
using Nexo.Agents.UniversalTester.Configuration;
using Nexo.Agents.UniversalTester.Models;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace Nexo.Agents.UniversalTester.Adapters;

/// <summary>
/// Adapter for Unity games. Communicates via:
/// 1. Screenshot capture (external)
/// 2. TCP/WebSocket to a Nexo plugin running in-game
/// </summary>
public class GameAdapter : ITargetAdapter
{
    private Process? _gameProcess;
    private TcpClient? _gameConnection;
    private StreamReader? _reader;
    private StreamWriter? _writer;
    private readonly ILogger<GameAdapter>? _logger;
    
    public TargetType TargetType => TargetType.Game;
    public bool IsConnected => _gameConnection?.Connected ?? false;
    
    public GameAdapter(ILogger<GameAdapter>? logger = null)
    {
        _logger = logger;
    }
    
    public async Task ConnectAsync(string target, CancellationToken ct = default)
    {
        // Launch game if it's an executable path
        if (File.Exists(target))
        {
            _gameProcess = Process.Start(new ProcessStartInfo
            {
                FileName = target,
                Arguments = "--nexo-test-mode --nexo-port=9999"
            });
            
            // Wait for game to start
            await Task.Delay(5000, ct);
        }
        
        // Connect to Nexo plugin in game
        _gameConnection = new TcpClient();
        try
        {
            await _gameConnection.ConnectAsync("localhost", 9999, ct);
            var stream = _gameConnection.GetStream();
            _reader = new StreamReader(stream);
            _writer = new StreamWriter(stream) { AutoFlush = true };
            _logger?.LogInformation("Connected to game at {Target}", target);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Could not connect to game plugin, continuing with limited functionality");
        }
    }
    
    public Task DisconnectAsync(CancellationToken ct = default)
    {
        _gameConnection?.Dispose();
        _gameProcess?.Kill();
        _gameProcess = null;
        _gameConnection = null;
        _reader = null;
        _writer = null;
        return Task.CompletedTask;
    }
    
    public async Task<byte[]?> CaptureScreenshotAsync(CancellationToken ct = default)
    {
        if (_writer == null) return null;
        
        try
        {
            await _writer.WriteLineAsync("SCREENSHOT");
            var response = await _reader!.ReadLineAsync(ct);
            
            if (response?.StartsWith("DATA:") == true)
            {
                return Convert.FromBase64String(response[5..]);
            }
        }
        catch
        {
            // Connection failed, return null
        }
        
        return null;
    }
    
    public async Task<GameState?> GetGameStateAsync(CancellationToken ct = default)
    {
        if (_writer == null) return null;
        
        try
        {
            await _writer.WriteLineAsync("GAMESTATE");
            var json = await _reader!.ReadLineAsync(ct);
            
            return json != null ? JsonSerializer.Deserialize<GameState>(json) : null;
        }
        catch
        {
            return null;
        }
    }
    
    public async Task<PlayerState?> GetPlayerStateAsync(CancellationToken ct = default)
    {
        if (_writer == null) return null;
        
        try
        {
            await _writer.WriteLineAsync("PLAYERSTATE");
            var json = await _reader!.ReadLineAsync(ct);
            
            return json != null ? JsonSerializer.Deserialize<PlayerState>(json) : null;
        }
        catch
        {
            return null;
        }
    }
    
    public async Task<IReadOnlyList<InteractiveElement>> GetInteractiveElementsAsync(CancellationToken ct = default)
    {
        if (_writer == null) return Array.Empty<InteractiveElement>();
        
        try
        {
            await _writer.WriteLineAsync("INTERACTABLES");
            var json = await _reader!.ReadLineAsync(ct);
            
            return json != null 
                ? JsonSerializer.Deserialize<List<InteractiveElement>>(json) ?? new List<InteractiveElement>()
                : Array.Empty<InteractiveElement>();
        }
        catch
        {
            return Array.Empty<InteractiveElement>();
        }
    }
    
    public async Task<string?> ExecuteActionAsync(TestAction action, CancellationToken ct = default)
    {
        if (_writer == null) return "Not connected";
        
        try
        {
            var command = action.Type switch
            {
                ActionType.Move => $"MOVE {action.InputValue}",
                ActionType.Look => $"LOOK {action.Coordinates?.X} {action.Coordinates?.Y}",
                ActionType.Jump => "INPUT jump",
                ActionType.Attack => "INPUT attack",
                ActionType.Interact => $"INTERACT {action.ElementId}",
                ActionType.UseItem => $"USEITEM {action.InputValue}",
                ActionType.OpenMenu => "INPUT menu",
                ActionType.Click => $"CLICK {action.Coordinates?.X} {action.Coordinates?.Y}",
                ActionType.KeyPress => $"KEY {action.Key}",
                _ => null
            };
            
            if (command == null) return $"Unsupported: {action.Type}";
            
            await _writer.WriteLineAsync(command);
            return await _reader!.ReadLineAsync(ct);
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }
    
    public Task<PerformanceMetrics?> GetPerformanceAsync(CancellationToken ct = default)
    {
        if (_writer == null) return Task.FromResult<PerformanceMetrics?>(null);
        
        try
        {
            _writer.WriteLine("PERFORMANCE");
            var json = _reader!.ReadLine();
            return Task.FromResult(json != null ? JsonSerializer.Deserialize<PerformanceMetrics>(json) : null);
        }
        catch
        {
            return Task.FromResult<PerformanceMetrics?>(null);
        }
    }
    
    public Task<string?> GetStructureAsync(CancellationToken ct = default) =>
        Task.FromResult<string?>(null);
    
    public Task<string?> GetAccessibilityTreeAsync(CancellationToken ct = default) =>
        Task.FromResult<string?>(null);
    
    public Task<IReadOnlyList<GameObject>> GetVisibleObjectsAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<GameObject>>(Array.Empty<GameObject>());
    
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
    
    public Task<string?> GetCurrentUrlAsync(CancellationToken ct = default) =>
        Task.FromResult<string?>(null);
    
    public Task<string?> GetWindowTitleAsync(CancellationToken ct = default) =>
        Task.FromResult<string?>(null);
    
    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
    }
}
