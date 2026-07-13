namespace Nexo.Provenance.Graph.Models;

/// <summary>Graph metadata node recording projection staleness anchor.</summary>
public sealed record ProvenanceGraphMetadata
{
    /// <summary>Chain-head hash the graph was last projected from.</summary>
    public required string ChainHeadHash { get; init; }

    /// <summary>UTC timestamp of the last successful projection.</summary>
    public required DateTimeOffset ProjectedAt { get; init; }
}
