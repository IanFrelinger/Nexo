namespace Nexo.Infrastructure.Autonomy;

/// <summary>
/// Host configuration for the autonomous self-extension loop, bound from
/// <c>Nexo:Autonomy</c>. Fail-closed: <see cref="Enabled"/> is false, so a host that
/// merely references the package runs nothing until an operator opts in.
/// </summary>
public sealed class NexoAutonomyOptions
{
    /// <summary>Configuration section this binds from.</summary>
    public const string SectionName = "Nexo:Autonomy";

    /// <summary>Master switch. Off by default; nothing autonomous runs until this is true.</summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Container image proposal sessions start from. Required when
    /// <see cref="UseSandboxSessions"/> is true — an unnamed image cannot be attested,
    /// and an unattestable environment must not reach a certificate.
    /// </summary>
    public string? SessionImage { get; set; }

    /// <summary>Whether iterations open sandbox sessions at all (attestation + environment inputs).</summary>
    public bool UseSandboxSessions { get; set; }

    /// <summary>Committed generations retained for no-build rollback (R5.1). Minimum 1 to roll back at all.</summary>
    public int RetentionWindow { get; set; } = 2;

    /// <summary>Minimum seconds between autonomous swaps (R6.1). 0 disables the cadence floor.</summary>
    public int CadenceFloorSeconds { get; set; } = 300;

    /// <summary>Invocations of a new generation before error/latency verdicts are meaningful (R5.2).</summary>
    public int WatchMinInvocations { get; set; } = 5;

    /// <summary>Tolerated error-rate increase over the previous generation's baseline.</summary>
    public double WatchMaxErrorRateDelta { get; set; } = 0.2;

    /// <summary>Tolerated mean-latency multiple of the baseline.</summary>
    public double WatchMaxLatencyFactor { get; set; } = 3.0;

    /// <summary>Rollbacks on one lineage before it loses Tier-0 autonomy (R5.5).</summary>
    public int LineageDemotionThreshold { get; set; } = 2;

    /// <summary>Seconds between leaked-session reaper sweeps. 0 disables the sweep service.</summary>
    public int ReaperSweepSeconds { get; set; } = 60;

    /// <summary>Absolute per-iteration wall-clock ceiling in seconds (R4.6/B11.2).</summary>
    public int IterationCeilingSeconds { get; set; } = 600;

    /// <summary>Multiple of the median iteration a single iteration may take before the guard trims it.</summary>
    public double ThroughputGuardFactor { get; set; } = 4;
}
