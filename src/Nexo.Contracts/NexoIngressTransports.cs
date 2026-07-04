namespace Nexo.Contracts;

/// <summary>Stable transport labels for logging, envelopes, and capability checks.</summary>
public static class NexoIngressTransports
{
    /// <summary>Standard HTTP request ingress.</summary>
    public const string Http = "http";

    /// <summary>WebSocket connection ingress.</summary>
    public const string WebSocket = "websocket";

    /// <summary>Simulated SMS ingress for local development and operator testing.</summary>
    public const string SmsSimulated = "sms-simulated";
}
