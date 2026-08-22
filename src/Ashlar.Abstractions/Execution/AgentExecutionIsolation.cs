namespace Ashlar.Abstractions.Execution;

/// <summary>
/// Helpers for propagating <see cref="AgentExecutionIsolationLevel"/> on agent invocation metadata
/// (<c>ashlar.execution.isolation</c> on <c>AgentInvocationRequest.Metadata</c>).
/// Transports and execution hosts read this key to select backends without coupling orchestration to Docker/K8s APIs.
/// </summary>
public static class AgentExecutionIsolation
{
    /// <summary>Metadata key on agent invocation requests.</summary>
    public const string MetadataKey = "ashlar.execution.isolation";

    /// <summary>Stable wire format: enum name (e.g. <c>InProcess</c>, <c>ContainerPerAgent</c>).</summary>
    public static string Format(AgentExecutionIsolationLevel level) => level.ToString("G");

    /// <summary>
    /// Parses the isolation tier from transport metadata. Returns <see langword="false"/> if the key is missing or invalid.
    /// </summary>
    public static bool TryParse(
        IReadOnlyDictionary<string, string>? metadata,
        out AgentExecutionIsolationLevel level)
    {
        level = AgentExecutionIsolationLevel.InProcess;
        if (metadata is null || !metadata.TryGetValue(MetadataKey, out var raw) || string.IsNullOrWhiteSpace(raw))
            return false;

        return Enum.TryParse(raw, ignoreCase: true, out level);
    }

    /// <summary>
    /// Effective isolation: explicit metadata wins; otherwise <paramref name="fallback"/>.
    /// </summary>
    public static AgentExecutionIsolationLevel GetEffective(
        IReadOnlyDictionary<string, string>? metadata,
        AgentExecutionIsolationLevel fallback)
        => TryParse(metadata, out var parsed) ? parsed : fallback;
}
