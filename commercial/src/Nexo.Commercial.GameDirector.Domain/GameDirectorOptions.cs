namespace GameDirector.Domain;

/// <summary>Configuration options for game director.</summary>
public sealed class GameDirectorOptions
{
    public const string SectionName = "GameDirector";

    /// <summary>Balance watch path.</summary>
    public string BalanceWatchPath { get; set; } = "data/balance";
    /// <summary>Map watch path.</summary>
    public string MapWatchPath { get; set; } = "data/maps";
    /// <summary>Drift threshold percent.</summary>
    public double DriftThresholdPercent { get; set; } = 15.0;
    /// <summary>Balance watcher interval.</summary>
    public TimeSpan BalanceWatcherInterval { get; set; } = TimeSpan.FromMinutes(15);
    /// <summary>Balance watcher startup delay.</summary>
    public TimeSpan BalanceWatcherStartupDelay { get; set; } = TimeSpan.FromSeconds(5);
    /// <summary>Map validator full scan interval.</summary>
    public TimeSpan MapValidatorFullScanInterval { get; set; } = TimeSpan.FromMinutes(60);
    /// <summary>Map validator loop interval.</summary>
    public TimeSpan MapValidatorLoopInterval { get; set; } = TimeSpan.FromSeconds(30);
    /// <summary>Trust pack id.</summary>
    public string TrustPackId { get; set; } = "internal-only";
}
