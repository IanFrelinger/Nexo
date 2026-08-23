namespace Ashlar.Contracts;

/// <summary>Stable transport labels for logging, envelopes, and capability checks.</summary>
public static class AshlarIngressTransports
{
    /// <summary>Standard HTTP request ingress.</summary>
    public const string Http = "http";

    /// <summary>WebSocket connection ingress.</summary>
    public const string WebSocket = "websocket";

    /// <summary>Simulated SMS ingress for local development and operator testing.</summary>
    public const string SmsSimulated = "sms-simulated";

    /// <summary>Model Context Protocol ingress (AI clients calling Ashlar tools).</summary>
    public const string Mcp = "mcp";

    /// <summary>Agent2Agent protocol ingress (external agents delegating tasks to Ashlar agents).</summary>
    public const string A2A = "a2a";
}
