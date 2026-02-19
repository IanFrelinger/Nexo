using Microsoft.Extensions.Logging;
using Nexo.Agents.UniversalTester.Configuration;
using Nexo.Agents.UniversalTester.Models;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace Nexo.Agents.UniversalTester.Adapters;

/// <summary>
/// Adapter for games with a Nexo plugin (e.g., Unity). Communicates via TCP to an in-game server.
/// Protocol: HELLO, SCREENSHOT (base64 PNG), GAMESTATE, PLAYERSTATE, INTERACTABLES, and action commands
/// (CLICK, MOVE, LOOK, JUMP, ATTACK, INTERACT, etc.). Target format: game://host:port or tcp://host:port.
/// </summary>
public class GameAdapter : ITargetAdapter
{
    private Process? _gameProcess;
    private TcpClient? _gameConnection;
    private StreamReader? _reader;
    private StreamWriter? _writer;
    private readonly ILogger<GameAdapter>? _logger;
    private string _host = "localhost";
    private int _port = 9999;
    private bool _handshakeAttempted;
    
    public TargetType TargetType => TargetType.Game;
    public bool IsConnected => _gameConnection?.Connected ?? false;
    
    private readonly bool _hasConfigOverride;

    /// <summary>
    /// Creates a GameAdapter. configHost/configPort override target parsing when provided (e.g., from runtime config).
    /// </summary>
    public GameAdapter(ILogger<GameAdapter>? logger = null, string? configHost = null, int? configPort = null)
    {
        _logger = logger;
        _hasConfigOverride = !string.IsNullOrWhiteSpace(configHost) || (configPort is > 0 and < 65536);
        if (!string.IsNullOrWhiteSpace(configHost))
            _host = configHost;
        if (configPort is > 0 and < 65536)
            _port = configPort.Value;
    }
    
    /// <summary>Parses target (path, game://, tcp://), launches game if path given, connects to plugin TCP server.</summary>
    public async Task ConnectAsync(string target, CancellationToken ct = default)
    {
        ParseTarget(target);

        // Launch game if it's an executable path
        if (File.Exists(target))
        {
            _gameProcess = Process.Start(new ProcessStartInfo
            {
                FileName = target,
                Arguments = $"--nexo-test-mode --nexo-port={_port}"
            });
            
            // Wait for game to start
            await Task.Delay(5000, ct);
        }
        
        // Connect to Nexo plugin in game
        _gameConnection = new TcpClient();
        try
        {
            await _gameConnection.ConnectAsync(_host, _port, ct);
            var stream = _gameConnection.GetStream();
            _reader = new StreamReader(stream);
            _writer = new StreamWriter(stream) { AutoFlush = true };
            _logger?.LogInformation("Connected to game plugin at {Host}:{Port}", _host, _port);

            await TryHandshakeAsync(ct);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Could not connect to game plugin, continuing with limited functionality");
        }
    }
    
    /// <inheritdoc />
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
    
    /// <summary>Sends SCREENSHOT command; returns base64-decoded PNG bytes from plugin.</summary>
    public async Task<byte[]?> CaptureScreenshotAsync(CancellationToken ct = default)
    {
        if (_writer == null) return null;
        
        try
        {
            await SendAsync("SCREENSHOT", ct);
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
    
    /// <summary>Sends GAMESTATE command; returns deserialized GameState JSON from plugin.</summary>
    public async Task<GameState?> GetGameStateAsync(CancellationToken ct = default)
    {
        if (_writer == null) return null;
        
        try
        {
            await SendAsync("GAMESTATE", ct);
            var json = await _reader!.ReadLineAsync(ct);
            
            return json != null ? JsonSerializer.Deserialize<GameState>(json) : null;
        }
        catch
        {
            return null;
        }
    }
    
    /// <inheritdoc />
    public async Task<PlayerState?> GetPlayerStateAsync(CancellationToken ct = default)
    {
        if (_writer == null) return null;
        
        try
        {
            await SendAsync("PLAYERSTATE", ct);
            var json = await _reader!.ReadLineAsync(ct);
            
            return json != null ? JsonSerializer.Deserialize<PlayerState>(json) : null;
        }
        catch
        {
            return null;
        }
    }
    
    /// <inheritdoc />
    public async Task<IReadOnlyList<InteractiveElement>> GetInteractiveElementsAsync(CancellationToken ct = default)
    {
        if (_writer == null) return Array.Empty<InteractiveElement>();
        
        try
        {
            await SendAsync("INTERACTABLES", ct);
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
    
    /// <summary>Maps TestAction to plugin command (CLICK, MOVE, INTERACT, etc.) and sends it; returns plugin response.</summary>
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
            
            await SendAsync(command, ct);
            return await _reader!.ReadLineAsync(ct);
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }
    
    /// <inheritdoc />
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
    
    /// <inheritdoc />
    public Task<string?> GetStructureAsync(CancellationToken ct = default) =>
        Task.FromResult<string?>(null);
    
    /// <inheritdoc />
    public Task<string?> GetAccessibilityTreeAsync(CancellationToken ct = default) =>
        Task.FromResult<string?>(null);
    
    /// <inheritdoc />
    public Task<IReadOnlyList<GameObject>> GetVisibleObjectsAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<GameObject>>(Array.Empty<GameObject>());
    
    /// <inheritdoc />
    public Task<ApiResponse?> GetLastApiResponseAsync(CancellationToken ct = default) =>
        Task.FromResult<ApiResponse?>(null);
    
    /// <inheritdoc />
    public Task<IReadOnlyList<ApiEndpoint>> GetAvailableEndpointsAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<ApiEndpoint>>(Array.Empty<ApiEndpoint>());
    
    /// <inheritdoc />
    public Task<string?> GetTerminalOutputAsync(CancellationToken ct = default) =>
        Task.FromResult<string?>(null);
    
    /// <inheritdoc />
    public Task<string?> GetCurrentPromptAsync(CancellationToken ct = default) =>
        Task.FromResult<string?>(null);
    
    /// <inheritdoc />
    public Task<IReadOnlyList<string>> GetConsoleLogAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
    
    /// <inheritdoc />
    public Task<IReadOnlyList<string>> GetErrorsAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
    
    /// <inheritdoc />
    public Task<IReadOnlyList<string>> GetWarningsAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
    
    /// <inheritdoc />
    public Task<string?> GetCurrentUrlAsync(CancellationToken ct = default) =>
        Task.FromResult<string?>(null);
    
    /// <inheritdoc />
    public Task<string?> GetWindowTitleAsync(CancellationToken ct = default) =>
        Task.FromResult<string?>(null);
    
    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
    }

    /// <summary>Extracts host/port from target (tcp://host:port, game://host:port) unless config override is set.</summary>
    private void ParseTarget(string target)
    {
        if (_hasConfigOverride)
            return; // Constructor host/port take precedence
        // Supports: file path, tcp://host:port, game://host:port
        if (target.StartsWith("tcp://", StringComparison.OrdinalIgnoreCase) ||
            target.StartsWith("game://", StringComparison.OrdinalIgnoreCase))
        {
            if (Uri.TryCreate(target, UriKind.Absolute, out var uri))
            {
                _host = string.IsNullOrWhiteSpace(uri.Host) ? "localhost" : uri.Host;
                _port = uri.Port > 0 ? uri.Port : 9999;
            }
        }
        else
        {
            _host = "localhost";
            _port = 9999;
        }
    }

    /// <summary>Sends HELLO; expects NEXO_PLUGIN 1.0 response for protocol versioning.</summary>
    private async Task TryHandshakeAsync(CancellationToken ct)
    {
        if (_handshakeAttempted || _writer == null) return;
        _handshakeAttempted = true;

        try
        {
            await SendAsync("HELLO", ct);
            var line = await _reader!.ReadLineAsync(ct);
            if (!string.IsNullOrWhiteSpace(line))
            {
                _logger?.LogInformation("Game plugin handshake: {Line}", line);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "Game plugin handshake failed (non-fatal)");
        }
    }

    private async Task SendAsync(string line, CancellationToken ct)
    {
        // Basic cancellation-friendly send wrapper.
        ct.ThrowIfCancellationRequested();
        await _writer!.WriteLineAsync(line);
        ct.ThrowIfCancellationRequested();
    }
}
