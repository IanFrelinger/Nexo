namespace Nexo.SDK.Estimation;

/// <summary>
/// Represents estimated or actual resource usage (cost and memory) for a geospatial operation.
/// </summary>
public sealed record ResourceEstimate
{
    /// <summary>
    /// Estimated or actual cost in USD.
    /// </summary>
    public required decimal CostUsd { get; init; }

    /// <summary>
    /// Estimated or actual memory footprint in bytes.
    /// </summary>
    public required long MemoryBytes { get; init; }

    /// <summary>
    /// Breakdown of cost by component (e.g., "mapbox-tiles", "srtm-downloads").
    /// </summary>
    public IReadOnlyDictionary<string, decimal>? CostBreakdown { get; init; }

    /// <summary>
    /// Breakdown of memory by component (e.g., "elevation-grid", "mesh-vertices").
    /// </summary>
    public IReadOnlyDictionary<string, long>? MemoryBreakdown { get; init; }

    /// <summary>
    /// Whether this is an estimate (before operation) or actual (after operation).
    /// </summary>
    public bool IsEstimate { get; init; } = true;

    /// <summary>
    /// Additional metadata about the estimation.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }

    /// <summary>
    /// Gets memory footprint in megabytes.
    /// </summary>
    public double MemoryMegabytes => MemoryBytes / (1024.0 * 1024.0);

    /// <summary>
    /// Gets memory footprint in gigabytes.
    /// </summary>
    public double MemoryGigabytes => MemoryBytes / (1024.0 * 1024.0 * 1024.0);

    /// <summary>
    /// Combines two resource estimates (adds costs and memory).
    /// </summary>
    public static ResourceEstimate operator +(ResourceEstimate a, ResourceEstimate b)
    {
        var costBreakdown = new Dictionary<string, decimal>();
        if (a.CostBreakdown != null)
        {
            foreach (var kvp in a.CostBreakdown)
                costBreakdown[kvp.Key] = kvp.Value;
        }
        if (b.CostBreakdown != null)
        {
            foreach (var kvp in b.CostBreakdown)
                costBreakdown[kvp.Key] = (costBreakdown.TryGetValue(kvp.Key, out var existing) ? existing : 0) + kvp.Value;
        }

        var memoryBreakdown = new Dictionary<string, long>();
        if (a.MemoryBreakdown != null)
        {
            foreach (var kvp in a.MemoryBreakdown)
                memoryBreakdown[kvp.Key] = kvp.Value;
        }
        if (b.MemoryBreakdown != null)
        {
            foreach (var kvp in b.MemoryBreakdown)
                memoryBreakdown[kvp.Key] = (memoryBreakdown.TryGetValue(kvp.Key, out var existing) ? existing : 0) + kvp.Value;
        }

        return new ResourceEstimate
        {
            CostUsd = a.CostUsd + b.CostUsd,
            MemoryBytes = a.MemoryBytes + b.MemoryBytes,
            CostBreakdown = costBreakdown.Count > 0 ? costBreakdown : null,
            MemoryBreakdown = memoryBreakdown.Count > 0 ? memoryBreakdown : null,
            IsEstimate = a.IsEstimate || b.IsEstimate,
            Metadata = CombineMetadata(a.Metadata, b.Metadata)
        };
    }

    private static IReadOnlyDictionary<string, object>? CombineMetadata(
        IReadOnlyDictionary<string, object>? a,
        IReadOnlyDictionary<string, object>? b)
    {
        if (a == null && b == null) return null;
        var combined = new Dictionary<string, object>();
        if (a != null)
        {
            foreach (var kvp in a)
                combined[kvp.Key] = kvp.Value;
        }
        if (b != null)
        {
            foreach (var kvp in b)
                combined[kvp.Key] = kvp.Value;
        }
        return combined;
    }
}
