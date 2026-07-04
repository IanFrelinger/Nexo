namespace Nexo.Core.Domain.Bricks;

/// <summary>
/// Characteristics of an implementation (latency, determinism, resource usage).
/// </summary>
public class ImplementationCharacteristics
{
    /// <summary>Expected latency band (e.g. <c>&lt; 1s</c>, <c>1-5s</c>).</summary>
    public string Latency { get; init; } = "< 1s";

    /// <summary>Whether repeated runs with identical input produce identical output.</summary>
    public bool Deterministic { get; init; }

    /// <summary>Whether the implementation requires outbound network access.</summary>
    public bool RequiresNetwork { get; init; }

    /// <summary>Relative CPU/memory footprint of this implementation.</summary>
    public ResourceUsage ResourceUsage { get; init; } = ResourceUsage.Low;
}
