namespace Ashlar.Commercial.Fleet.Infrastructure.Execution;
/// <summary>
/// Configuration for <see cref="AdaptiveBrickCache"/> (dynamic TTL by usage).
/// </summary>
public class AdaptiveBrickCacheOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "AdaptiveBrickCache";

    /// <summary>TTL in seconds for hot bricks (top 20% by usage). Default 300 (5 min).</summary>
    public int HotBrickTtlSeconds { get; set; } = 300;

    /// <summary>TTL in seconds for cold bricks. Default 30.</summary>
    public int ColdBrickTtlSeconds { get; set; } = 30;

    /// <summary>Number of top-used bricks considered hot. Default 50.</summary>
    public int HotBrickTopN { get; set; } = 50;
}
