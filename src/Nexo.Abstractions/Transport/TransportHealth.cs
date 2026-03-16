namespace Nexo.Abstractions.Transport;

/// <summary>
/// Health status of an agent transport channel.
/// </summary>
/// <param name="IsHealthy">Whether the channel is currently healthy.</param>
/// <param name="TransportName">Logical transport name.</param>
/// <param name="Message">Optional detail message.</param>
public sealed record TransportHealth(
    bool IsHealthy,
    string TransportName,
    string? Message = null);
