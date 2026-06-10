namespace Nexo.API.Security;

/// <summary>
/// Plan entitlements for multi-tenant shaping (Product Fleet Phase 0).
/// Enforcement is incremental: copilot hourly quota is active; other fields are config hooks for Phase 0.2+.
/// </summary>
public sealed class NexoEntitlementsOptions
{
    public const string SectionPath = "Nexo:Entitlements";

    /// <summary>Zero means unlimited.</summary>
    public int MaxCopilotSubmissionsPerHour { get; set; }

    /// <summary>Licensed seat count (0 = not enforced).</summary>
    public int Seats { get; set; }

    /// <summary>Included copilot/job units per billing period (0 = not enforced).</summary>
    public int IncludedJobs { get; set; }

    /// <summary>Max concurrent executions per tenant (0 = not enforced).</summary>
    public int MaxConcurrency { get; set; }

    /// <summary>Artifact/history retention in days (0 = platform default).</summary>
    public int RetentionDays { get; set; }

    public bool SsoEnabled { get; set; }

    public bool AuditExportEnabled { get; set; }

    /// <summary>e.g. Private, Cloud, Enterprise — documentation / license profile hint.</summary>
    public string DeploymentMode { get; set; } = "";
}
