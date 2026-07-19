using System.Diagnostics;
using System.Text.Json;
using Nexo.Abstractions.HostBridge;
using Nexo.HostBridge;

namespace Nexo.BRPlaytestAgent;

/// <summary>
/// Thin Weapon Lab façade over the shared NDJSON host-bridge client.
/// </summary>
internal sealed class WeaponLabClient : IAsyncDisposable
{
    private readonly IHostBridgeClient _bridge;

    public WeaponLabClient(int port, string token)
    {
        _bridge = new TcpNdjsonHostBridgeClient(
            HostBridgeEndpoint.Localhost(port, token));
    }

    public Task ConnectAsync(
        TimeSpan timeout,
        CancellationToken cancellationToken)
        => _bridge.ConnectAsync(timeout, cancellationToken);

    public Task<JsonElement> SendAsync(
        string command,
        object? arguments,
        CancellationToken cancellationToken)
        => _bridge.SendAsync(command, arguments, cancellationToken);

    public ValueTask DisposeAsync() => _bridge.DisposeAsync();
}

internal sealed class PlaytestSession : IAsyncDisposable
{
    public sealed record StatusCheckpoint(string Label, JsonElement Status);

    public required string BuildPath { get; init; }
    public required string ArtifactRoot { get; init; }
    public required int Port { get; init; }
    public required string Token { get; init; }
    public bool VirtualPlayerMode { get; init; }
    public bool BuiltInCaptureMode { get; init; }
    public string? BuiltInCaptureOutputPath { get; init; }

    public Process? Process { get; set; }
    public WeaponLabClient? Client { get; set; }
    public List<string> Actions { get; } = new();
    public List<string> Screenshots { get; } = new();
    public List<string> Videos { get; } = new();
    public List<StatusCheckpoint> Checkpoints { get; } = new();
    public bool AccessibilityVerified { get; set; }
    public bool ScreenRecordingVerified { get; set; }

    public async ValueTask DisposeAsync()
    {
        if (Client is not null)
            await Client.DisposeAsync().ConfigureAwait(false);
        if (Process is { HasExited: false })
        {
            Process.Kill(entireProcessTree: true);
            await Process.WaitForExitAsync().ConfigureAwait(false);
        }
        Process?.Dispose();
    }
}
