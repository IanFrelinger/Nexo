namespace Ashlar.Core.Domain.Clusters;

/// <summary>
/// Usage statistics for a cluster.
/// </summary>
public class ClusterStats
{
    public int UsageCount { get; set; }
    public int FavoriteCount { get; set; }
    public double AverageRating { get; set; }
}
