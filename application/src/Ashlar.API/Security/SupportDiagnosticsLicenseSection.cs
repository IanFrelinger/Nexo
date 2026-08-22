using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Ashlar.API.Security;

/// <summary>Private license snapshot included in support diagnostics export.</summary>
public sealed class SupportDiagnosticsLicenseSection
{
    /// <summary>Resolved license state name.</summary>
    public string State { get; init; } = "";

    /// <summary>Customer identifier from the license file.</summary>
    public string? CustomerId { get; init; }

    /// <summary>Tenant identifier bound to the license.</summary>
    public string? TenantId { get; init; }

    /// <summary>License expiration timestamp when present.</summary>
    public DateTimeOffset? ExpiresAt { get; init; }

    /// <summary>Licensed seat count from the license file.</summary>
    public int Seats { get; init; }

    /// <summary>Human-readable status detail or error message.</summary>
    public string? Detail { get; init; }

    /// <summary>Whether license enforcement is active in configuration.</summary>
    public bool Enforced { get; init; }
}
