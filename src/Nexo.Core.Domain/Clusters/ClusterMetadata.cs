namespace Nexo.Core.Domain.Clusters;

/// <summary>
/// Metadata about a cluster.
/// </summary>
public class ClusterMetadata
{
    /// <summary>
    /// Who created this cluster.
    /// </summary>
    public string Author { get; init; } = default!;
    
    /// <summary>
    /// When created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    
    /// <summary>
    /// When last modified.
    /// </summary>
    public DateTimeOffset ModifiedAt { get; init; } = DateTimeOffset.UtcNow;
    
    /// <summary>
    /// Sharing scope.
    /// </summary>
    public SharingScope Scope { get; init; } = SharingScope.Private;
    
    /// <summary>
    /// Usage statistics.
    /// </summary>
    public ClusterStats Stats { get; init; } = new();
}
