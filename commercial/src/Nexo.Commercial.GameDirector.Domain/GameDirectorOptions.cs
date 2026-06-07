namespace GameDirector.Domain;

public sealed class GameDirectorOptions
{
    public const string SectionName = "GameDirector";

    public string BalanceWatchPath { get; set; } = "data/balance";
    public string MapWatchPath { get; set; } = "data/maps";
    public double DriftThresholdPercent { get; set; } = 15.0;
    public TimeSpan BalanceWatcherInterval { get; set; } = TimeSpan.FromMinutes(15);
    public TimeSpan BalanceWatcherStartupDelay { get; set; } = TimeSpan.FromSeconds(5);
    public TimeSpan MapValidatorFullScanInterval { get; set; } = TimeSpan.FromMinutes(60);
    public TimeSpan MapValidatorLoopInterval { get; set; } = TimeSpan.FromSeconds(30);
    public string TrustPackId { get; set; } = "internal-only";
}
