using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Nexo.API.Security;

/// <summary>Entitlement limits included in support diagnostics export.</summary>
public sealed class SupportDiagnosticsEntitlementsSection
{
    /// <summary>Hourly copilot submission cap (0 = unlimited).</summary>
    public int MaxCopilotSubmissionsPerHour { get; init; }

    /// <summary>Licensed seat count.</summary>
    public int Seats { get; init; }

    /// <summary>Included job units per billing period.</summary>
    public int IncludedJobs { get; init; }

    /// <summary>Max concurrent executions per tenant.</summary>
    public int MaxConcurrency { get; init; }

    /// <summary>Artifact retention in days.</summary>
    public int RetentionDays { get; init; }

    /// <summary>Whether SSO entitlement is enabled.</summary>
    public bool SsoEnabled { get; init; }

    /// <summary>Whether audit export entitlement is enabled.</summary>
    public bool AuditExportEnabled { get; init; }
}
