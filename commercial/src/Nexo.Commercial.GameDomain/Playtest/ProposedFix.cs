namespace Nexo.Commercial.GameDomain.Playtest;

/// <summary>A single proposed playtest fix with iterate command metadata.</summary>
public sealed record ProposedFix
{
    /// <summary>Sequential fix number within the playtest report.</summary>
    public int FixNumber { get; init; }
    /// <summary>Target game system path (for example Weapons/RocketLauncher).</summary>
    public string TargetSystem { get; init; } = string.Empty; // "Weapons/RocketLauncher", "Map/EastCorridor"
    /// <summary>Human-readable fix description for operators.</summary>
    public string Description { get; init; } = string.Empty;
    /// <summary>CLI iterate command to apply this fix.</summary>
    public string IterateCommand { get; init; } = string.Empty;
    /// <summary>Severity label (for example warning or critical).</summary>
    public string Severity { get; init; } = "warning";
}
