namespace Nexo.Infrastructure.Networking;

/// <summary>
/// Configuration for the plasticity service (metrics and optional background tasks).
/// </summary>
public sealed class PlasticityOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Plasticity";

    /// <summary>Number of hot bricks to include in metrics. Default 10.</summary>
    public int HotBrickTopN { get; set; } = 10;

    /// <summary>Timespan for cold brick window (bricks not used in this period). Default 1 hour.</summary>
    public int ColdBrickWindowMinutes { get; set; } = 60;
}
