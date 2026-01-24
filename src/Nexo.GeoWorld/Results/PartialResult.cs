namespace Nexo.GeoWorld.Results;

/// <summary>
/// Represents a result that may be partially successful, with some items succeeding and others failing.
/// Used for operations that process multiple items (e.g., downloading multiple tiles) where some may fail.
/// </summary>
public sealed record PartialResult<T>
{
    /// <summary>
    /// Whether the overall operation succeeded (all items succeeded).
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Whether the operation was partially successful (some items succeeded, some failed).
    /// </summary>
    public required bool Partial { get; init; }

    /// <summary>
    /// The successful results.
    /// </summary>
    public required IReadOnlyList<T> Results { get; init; }

    /// <summary>
    /// List of failure descriptions (one per failed item).
    /// </summary>
    public required IReadOnlyList<string> Failures { get; init; }

    /// <summary>
    /// Total number of items attempted.
    /// </summary>
    public int TotalAttempted => Results.Count + Failures.Count;

    /// <summary>
    /// Success rate (0.0 to 1.0).
    /// </summary>
    public double SuccessRate => TotalAttempted == 0 ? 0.0 : (double)Results.Count / TotalAttempted;
}

/// <summary>
/// Factory methods for creating PartialResult instances.
/// </summary>
public static class PartialResult
{
    /// <summary>
    /// Creates a fully successful result.
    /// </summary>
    public static PartialResult<T> Successful<T>(IReadOnlyList<T> results)
    {
        return new PartialResult<T>
        {
            Success = true,
            Partial = false,
            Results = results ?? Array.Empty<T>(),
            Failures = Array.Empty<string>()
        };
    }

    /// <summary>
    /// Creates a fully failed result.
    /// </summary>
    public static PartialResult<T> Failed<T>(IReadOnlyList<string> failures)
    {
        return new PartialResult<T>
        {
            Success = false,
            Partial = false,
            Results = Array.Empty<T>(),
            Failures = failures ?? Array.Empty<string>()
        };
    }

    /// <summary>
    /// Creates a partially successful result.
    /// </summary>
    public static PartialResult<T> PartialSuccess<T>(IReadOnlyList<T> results, IReadOnlyList<string> failures)
    {
        return new PartialResult<T>
        {
            Success = false,
            Partial = true,
            Results = results ?? Array.Empty<T>(),
            Failures = failures ?? Array.Empty<string>()
        };
    }
}
