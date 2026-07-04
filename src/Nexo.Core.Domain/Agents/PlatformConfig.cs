namespace Nexo.Core.Domain.Agents;

/// <summary>
/// Platform-specific configuration for an agent.
/// </summary>
public class PlatformConfig
{
    /// <summary>Optional UI component identifier for platform-specific rendering.</summary>
    public string? UiComponent { get; init; }

    /// <summary>Entry point invoked when the agent starts on this platform.</summary>
    public string EntryPoint { get; init; } = default!;

    /// <summary>Capability identifiers advertised on this platform.</summary>
    public IReadOnlyList<string> Capabilities { get; init; } = [];
}
