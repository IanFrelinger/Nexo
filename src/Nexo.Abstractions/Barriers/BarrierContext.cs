namespace Nexo.Abstractions.Barriers;

/// <summary>
/// Immutable barrier context propagated through orchestration and transports.
/// </summary>
public sealed record BarrierContext
{
    private BarrierContext() { }

    public string Level { get; init; } = string.Empty;

    public string AuthoritySource { get; init; } = string.Empty;

    public string IssuedTo { get; init; } = string.Empty;

    public DateTimeOffset IssuedAt { get; init; }

    public string CorrelationId { get; init; } = string.Empty;

    public string? ResolutionDetail { get; init; }

    public static BarrierContext Create(
        string level,
        string authoritySource,
        string issuedTo,
        string correlationId,
        BarrierHierarchy hierarchy,
        string? resolutionDetail = null)
    {
        ThrowIfNullCompat(hierarchy, nameof(hierarchy));
        if (!hierarchy.IsKnown(level))
            throw new ArgumentException(
                $"Unknown barrier level: '{level}'. Configured levels: {string.Join(", ", hierarchy)}",
                nameof(level));

        return new BarrierContext
        {
            Level = level,
            AuthoritySource = authoritySource ?? string.Empty,
            IssuedTo = issuedTo ?? string.Empty,
            IssuedAt = DateTimeOffset.UtcNow,
            CorrelationId = correlationId ?? string.Empty,
            ResolutionDetail = resolutionDetail
        };
    }

    public BarrierContext ForAgent(string agentName)
        => this with { IssuedTo = agentName };

    private static void ThrowIfNullCompat(object? value, string paramName)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(value, paramName);
#else
        if (value is null)
            throw new ArgumentNullException(paramName);
#endif
    }
}
