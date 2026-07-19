using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Nexo.Abstractions.HostBridge;
using Nexo.HostBridge;
using Nexo.Orchestration.Playtest.Models;
using Nexo.Orchestration.Playtest.Ports;

namespace Nexo.BackgroundAgents.HostRunners;

/// <summary>
/// First-class Nexo game-runner adapter for the Unity Weapon Lab localhost
/// bridge. It exposes semantic actions and structured state to AIPlayerAgent.
/// </summary>
public sealed class UnityWeaponLabGameRunner : IGameRunner, IAsyncDisposable
{
    private readonly ILogger<UnityWeaponLabGameRunner> _logger;
    private Process? _process;
    private IHostBridgeClient? _bridge;
    private uint _sequence;

    public UnityWeaponLabGameRunner(
        ILogger<UnityWeaponLabGameRunner> logger)
    {
        _logger = logger;
    }

    public bool IsRunning
        => _process is { HasExited: false } &&
           _bridge is { IsConnected: true };

    public async Task StartAsync(
        string buildPath,
        CancellationToken cancellationToken = default)
    {
        if (IsRunning)
            return;

        var executable = ResolveExecutable(buildPath);
        var port = ReadEnvironmentInt("NEXO_BR_PLAYTEST_PORT", 17420);
        var token = Environment.GetEnvironmentVariable(
                        "NEXO_BR_PLAYTEST_TOKEN")
                    ?? Guid.NewGuid().ToString("N");
        var artifactRoot = Path.GetFullPath(
            Environment.GetEnvironmentVariable(
                "NEXO_BR_PLAYTEST_ARTIFACT_ROOT")
            ?? Path.Combine(
                Directory.GetCurrentDirectory(),
                ".nexo",
                "playtest",
                "br-weapon-lab"));
        Directory.CreateDirectory(artifactRoot);

        var start = new ProcessStartInfo(executable)
        {
            UseShellExecute = false,
            WorkingDirectory = Path.GetDirectoryName(executable)!
        };
        start.Environment["NEXO_BR_PLAYTEST_PORT"] = port.ToString();
        start.Environment["NEXO_BR_PLAYTEST_TOKEN"] = token;
        start.Environment["NEXO_BR_PLAYTEST_ARTIFACT_ROOT"] = artifactRoot;
        start.ArgumentList.Add("-batchmode");
        _process = Process.Start(start)
            ?? throw new InvalidOperationException(
                "Could not launch Unity Weapon Lab.");

        _bridge = new TcpNdjsonHostBridgeClient(
            HostBridgeEndpoint.Localhost(port, token));
        await _bridge.ConnectAsync(TimeSpan.FromSeconds(30), cancellationToken)
            .ConfigureAwait(false);

        var deadline = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(30);
        while (DateTimeOffset.UtcNow < deadline)
        {
            var status = await SendAsync(
                "status",
                new { },
                cancellationToken).ConfigureAwait(false);
            if (status.TryGetProperty("ready", out var ready) &&
                ready.GetBoolean())
            {
                _logger.LogInformation(
                    "Unity Weapon Lab runner ready (PID {Pid})",
                    _process.Id);
                return;
            }

            await Task.Delay(250, cancellationToken).ConfigureAwait(false);
        }

        throw new TimeoutException("Unity Weapon Lab did not become ready.");
    }

    public async Task<GameState> GetGameStateAsync(
        CancellationToken cancellationToken = default)
    {
        var status = await SendAsync(
            "status",
            new { },
            cancellationToken).ConfigureAwait(false);
        var health = Number(status, "health");
        var maxHealth = Number(status, "maxHealth");
        var eliminated = Boolean(status, "eliminated");
        var weapon = Text(status, "weapon");
        var position = status.TryGetProperty("position", out var pos)
            ? pos.GetRawText()
            : "";
        var nearbyItems = status.TryGetProperty(
                "nearbyItems",
                out var items) &&
            items.ValueKind == JsonValueKind.Array
                ? items.EnumerateArray()
                    .Select(item => item.GetString() ?? "")
                    .Where(item => item.Length > 0)
                    .ToArray()
                : Array.Empty<string>();

        return new GameState
        {
            IsGameOver = eliminated,
            PlayerHealth = health,
            PlayerMaxHealth = maxHealth,
            PlayerPosition = position,
            EquippedWeapon = weapon,
            NearbyItems = nearbyItems,
            AvailableActions =
            [
                "move",
                "look",
                "fire",
                "reload",
                "interact",
                "capture",
                "wait"
            ],
            CurrentObjective =
                "Exercise weapon pickup, firing, reload, movement, and visual integrity."
        };
    }

    public async Task ExecuteActionAsync(
        string actionType,
        IReadOnlyDictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var action = actionType.Trim().ToLowerInvariant();
        if (action == "look")
        {
            await SendAsync("look", new
            {
                yaw = GetDouble(parameters, "yaw"),
                pitch = GetDouble(parameters, "pitch")
            }, cancellationToken).ConfigureAwait(false);
            return;
        }
        if (action == "capture")
        {
            await SendAsync("capture", new
            {
                path = GetString(parameters, "path", "runner-capture.png"),
                width = 640,
                height = 360
            }, cancellationToken).ConfigureAwait(false);
            return;
        }
        if (action == "wait")
        {
            await Task.Delay(
                Math.Clamp(GetInt(parameters, "duration_ms", 250), 1, 5000),
                cancellationToken).ConfigureAwait(false);
            return;
        }

        var duration = Math.Clamp(
            GetInt(parameters, "duration_ms", 250),
            16,
            5000);
        var fire = action == "fire";
        var reload = action == "reload";
        if (fire || reload)
            _sequence = unchecked(_sequence + 1);
        await SendAsync("input", new
        {
            moveX = action == "move"
                ? Math.Clamp(GetInt(parameters, "x"), -127, 127)
                : 0,
            moveY = action == "move"
                ? Math.Clamp(GetInt(parameters, "y", 127), -127, 127)
                : 0,
            fire,
            reload,
            interact = action == "interact",
            jump = action == "jump",
            ads = GetBoolean(parameters, "ads"),
            yaw = GetDouble(parameters, "yaw"),
            pitch = GetDouble(parameters, "pitch"),
            sequence = _sequence
        }, cancellationToken).ConfigureAwait(false);
        await Task.Delay(duration, cancellationToken).ConfigureAwait(false);
        await SendAsync(
            "clear_input",
            new { },
            cancellationToken).ConfigureAwait(false);
    }

    public async Task StopAsync(
        CancellationToken cancellationToken = default)
    {
        if (_bridge is { IsConnected: true })
        {
            try
            {
                await SendAsync(
                    "quit",
                    new { },
                    cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                // Process fallback below.
            }
        }
        if (_process is { HasExited: false })
        {
            try
            {
                await _process.WaitForExitAsync(cancellationToken)
                    .WaitAsync(TimeSpan.FromSeconds(5), cancellationToken)
                    .ConfigureAwait(false);
            }
            catch
            {
                _process.Kill(entireProcessTree: true);
            }
        }

        await DisposeTransportAsync().ConfigureAwait(false);
    }

    internal Task<JsonElement> SendAsync(
        string command,
        object arguments,
        CancellationToken cancellationToken)
    {
        if (_bridge is null)
            throw new InvalidOperationException("Unity Weapon Lab is not connected.");

        return _bridge.SendAsync(command, arguments, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (IsRunning)
            await StopAsync().ConfigureAwait(false);
        else
            await DisposeTransportAsync().ConfigureAwait(false);
        _process?.Dispose();
    }

    private async ValueTask DisposeTransportAsync()
    {
        if (_bridge is not null)
            await _bridge.DisposeAsync().ConfigureAwait(false);
        _bridge = null;
    }

    private static string ResolveExecutable(string buildPath)
    {
        var full = Path.GetFullPath(buildPath);
        var macOs = Path.Combine(full, "Contents", "MacOS");
        return Directory.GetFiles(macOs).FirstOrDefault()
               ?? throw new FileNotFoundException(
                   $"Unity Weapon Lab executable not found under {macOs}");
    }

    private static double Number(JsonElement root, string name)
        => root.TryGetProperty(name, out var value) &&
           value.ValueKind == JsonValueKind.Number
            ? value.GetDouble()
            : 0;

    private static bool Boolean(JsonElement root, string name)
        => root.TryGetProperty(name, out var value) &&
           value.ValueKind == JsonValueKind.True;

    private static string? Text(JsonElement root, string name)
        => root.TryGetProperty(name, out var value) &&
           value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static int ReadEnvironmentInt(string name, int fallback)
        => int.TryParse(Environment.GetEnvironmentVariable(name), out var value)
            ? value
            : fallback;

    private static int GetInt(
        IReadOnlyDictionary<string, object> values,
        string name,
        int fallback = 0)
    {
        if (!values.TryGetValue(name, out var value))
            return fallback;
        if (value is JsonElement json && json.TryGetInt32(out var number))
            return number;
        return Convert.ToInt32(value);
    }

    private static double GetDouble(
        IReadOnlyDictionary<string, object> values,
        string name,
        double fallback = 0)
    {
        if (!values.TryGetValue(name, out var value))
            return fallback;
        if (value is JsonElement json && json.TryGetDouble(out var number))
            return number;
        return Convert.ToDouble(value);
    }

    private static bool GetBoolean(
        IReadOnlyDictionary<string, object> values,
        string name,
        bool fallback = false)
    {
        if (!values.TryGetValue(name, out var value))
            return fallback;
        if (value is JsonElement json &&
            json.ValueKind is JsonValueKind.True or JsonValueKind.False)
            return json.GetBoolean();
        return Convert.ToBoolean(value);
    }

    private static string GetString(
        IReadOnlyDictionary<string, object> values,
        string name,
        string fallback)
    {
        if (!values.TryGetValue(name, out var value))
            return fallback;
        return value is JsonElement json
            ? json.GetString() ?? fallback
            : value.ToString() ?? fallback;
    }
}
