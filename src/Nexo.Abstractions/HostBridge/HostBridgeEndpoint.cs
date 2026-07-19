using System;

namespace Nexo.Abstractions.HostBridge;

/// <summary>Connection target for an NDJSON host bridge client.</summary>
public sealed class HostBridgeEndpoint
{
    /// <summary>Creates a host bridge endpoint.</summary>
    public HostBridgeEndpoint(
        string host,
        int port,
        string token)
    {
        if (string.IsNullOrWhiteSpace(host))
            throw new ArgumentException("Host is required.", nameof(host));
        if (port is < 1 or > 65535)
            throw new ArgumentOutOfRangeException(nameof(port), port, "Port must be 1–65535.");
        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentException("Token is required.", nameof(token));

        Host = host.Trim();
        Port = port;
        Token = token;
    }

    /// <summary>Hostname or IP (typically <c>127.0.0.1</c>).</summary>
    public string Host { get; }

    /// <summary>TCP port.</summary>
    public int Port { get; }

    /// <summary>Shared secret required by the host.</summary>
    public string Token { get; }

    /// <summary>Creates a localhost endpoint.</summary>
    public static HostBridgeEndpoint Localhost(int port, string token)
        => new("127.0.0.1", port, token);
}
