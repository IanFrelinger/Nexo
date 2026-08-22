namespace Ashlar.Abstractions.Barriers;

/// <summary>
/// Immutable barrier context propagated through orchestration and transports.
/// </summary>
public sealed record BarrierContext
{
    private BarrierContext() { }

    /// <summary>Active barrier level name (e.g. <c>public</c>, <c>internal</c>).</summary>
    public string Level { get; init; } = string.Empty;

    /// <summary>Authority source that established this barrier level.</summary>
    public string AuthoritySource { get; init; } = string.Empty;

    /// <summary>Principal or agent name the barrier context was issued to.</summary>
    public string IssuedTo { get; init; } = string.Empty;

    /// <summary>UTC timestamp when the barrier context was created.</summary>
    public DateTimeOffset IssuedAt { get; init; }

    /// <summary>Correlation id linking this context to distributed traces and audit events.</summary>
    public string CorrelationId { get; init; } = string.Empty;

    /// <summary>Optional resolver diagnostic detail for operators and audit sinks.</summary>
    public string? ResolutionDetail { get; init; }

    /// <summary>
    /// Creates a validated barrier context for the given level within the operator-defined hierarchy.
    /// </summary>
    /// <param name="level">Barrier level name; must exist in <paramref name="hierarchy"/>.</param>
    /// <param name="authoritySource">Origin of the authority claim.</param>
    /// <param name="issuedTo">Principal receiving the barrier context.</param>
    /// <param name="correlationId">Request correlation id.</param>
    /// <param name="hierarchy">Validated barrier hierarchy for level lookup.</param>
    /// <param name="resolutionDetail">Optional diagnostic detail from the identity resolver.</param>
    /// <returns>A new immutable barrier context stamped with the current UTC time.</returns>
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

    /// <summary>
    /// Returns a copy of this context with <see cref="IssuedTo"/> set to the target agent name.
    /// Used when propagating barrier context into agent invocations.
    /// </summary>
    /// <param name="agentName">Agent receiving the propagated context.</param>
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
