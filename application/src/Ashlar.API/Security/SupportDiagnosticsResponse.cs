using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Ashlar.API.Security;

/// <summary>Redacted support diagnostics bundle for on-call export.</summary>
public sealed class SupportDiagnosticsResponse
{
    /// <summary>UTC timestamp when the bundle was generated.</summary>
    public DateTimeOffset ExportedAt { get; init; }

    /// <summary>Application name (Ashlar.API).</summary>
    public string Application { get; init; } = "";

    /// <summary>Assembly informational version string.</summary>
    public string Version { get; init; } = "";

    /// <summary>Host environment name.</summary>
    public string Environment { get; init; } = "";

    /// <summary>Configured deployment mode from entitlements.</summary>
    public string DeploymentMode { get; init; } = "";

    /// <summary>Product and tenant defaults section.</summary>
    public SupportDiagnosticsProductSection Product { get; init; } = new();

    /// <summary>Plan entitlements section.</summary>
    public SupportDiagnosticsEntitlementsSection Entitlements { get; init; } = new();

    /// <summary>Built-in security posture section.</summary>
    public SupportDiagnosticsSecuritySection Security { get; init; } = new();

    /// <summary>Private license status section.</summary>
    public SupportDiagnosticsLicenseSection License { get; init; } = new();

    /// <summary>Ashlar configuration keys with sensitive values redacted.</summary>
    public IReadOnlyDictionary<string, string?> RedactedConfiguration { get; init; }
        = new Dictionary<string, string?>();
}
