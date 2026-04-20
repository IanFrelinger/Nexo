namespace Nexo.Abstractions.Resources;

/// <summary>
/// Unified async lifecycle for orchestration-scoped resources (agents, databases, etc.).
/// </summary>
public interface IProvisionedResource : IAsyncDisposable
{
    /// <summary>
    /// Resource classification for ordering and telemetry.
    /// </summary>
    ProvisionedResourceKind Kind { get; }
}
