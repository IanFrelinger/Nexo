using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Nexo.API.Security;

/// <summary>
/// Redacted support bundle for on-call (Product Fleet Phase 0.5).
/// </summary>
public static class SupportDiagnosticsExporter
{
    private static readonly HashSet<string> SensitiveKeyFragments = new(StringComparer.OrdinalIgnoreCase)
    {
        "password",
        "secret",
        "token",
        "apikey",
        "api_key",
        "bearer",
        "hmac",
        "privatekey",
        "private_key",
        "connectionstring",
        "connection_string",
    };

    public static SupportDiagnosticsResponse Build(
        IConfiguration configuration,
        IHostEnvironment environment,
        NexoEntitlementsOptions entitlements,
        NexoProductOptions product,
        NexoSecurityOptions security,
        PrivateLicenseStatus licenseStatus)
    {
        var version = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString()
            ?? "unknown";

        return new SupportDiagnosticsResponse
        {
            ExportedAt = DateTimeOffset.UtcNow,
            Application = "Nexo.API",
            Version = version,
            Environment = environment.EnvironmentName,
            DeploymentMode = entitlements.DeploymentMode,
            Product = new SupportDiagnosticsProductSection
            {
                DefaultTenantId = product.DefaultTenantId,
                AllowedTenantCount = (product.AllowedTenantIds ?? []).Count(x => !string.IsNullOrWhiteSpace(x)),
            },
            Entitlements = new SupportDiagnosticsEntitlementsSection
            {
                MaxCopilotSubmissionsPerHour = entitlements.MaxCopilotSubmissionsPerHour,
                Seats = entitlements.Seats,
                IncludedJobs = entitlements.IncludedJobs,
                MaxConcurrency = entitlements.MaxConcurrency,
                RetentionDays = entitlements.RetentionDays,
                SsoEnabled = entitlements.SsoEnabled,
                AuditExportEnabled = entitlements.AuditExportEnabled,
            },
            Security = new SupportDiagnosticsSecuritySection
            {
                ExposureProfile = security.ExposureProfile,
                AuthorizationMode = security.AuthorizationMode,
                AuthorizationScope = security.AuthorizationScope,
                RequireAuthForCopilotReadApis = security.RequireAuthForCopilotReadApis,
                ApiKeyConfigured = !string.IsNullOrWhiteSpace(security.ApiKey),
                BearerTokenConfigured = !string.IsNullOrWhiteSpace(security.BearerToken),
                BasicAuthConfigured = !string.IsNullOrWhiteSpace(security.BasicAuthUsername)
                                       && !string.IsNullOrWhiteSpace(security.BasicAuthPassword),
            },
            License = new SupportDiagnosticsLicenseSection
            {
                State = licenseStatus.State.ToString(),
                CustomerId = licenseStatus.CustomerId,
                TenantId = licenseStatus.TenantId,
                ExpiresAt = licenseStatus.ExpiresAt,
                Seats = licenseStatus.Seats,
                Detail = licenseStatus.Detail,
                Enforced = configuration.GetValue<bool>($"{NexoPrivateLicenseOptions.SectionPath}:EnforceLicense"),
            },
            RedactedConfiguration = RedactConfiguration(configuration),
        };
    }

    private static IReadOnlyDictionary<string, string?> RedactConfiguration(IConfiguration configuration)
    {
        var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var child in configuration.AsEnumerable())
        {
            if (string.IsNullOrEmpty(child.Key) || child.Value is null)
                continue;

            if (!child.Key.StartsWith("Nexo:", StringComparison.OrdinalIgnoreCase))
                continue;

            result[child.Key] = IsSensitiveKey(child.Key) ? "***REDACTED***" : child.Value;
        }

        return result;
    }

    private static bool IsSensitiveKey(string key)
    {
        foreach (var fragment in SensitiveKeyFragments)
        {
            if (key.Contains(fragment, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}

public sealed class SupportDiagnosticsResponse
{
    public DateTimeOffset ExportedAt { get; init; }

    public string Application { get; init; } = "";

    public string Version { get; init; } = "";

    public string Environment { get; init; } = "";

    public string DeploymentMode { get; init; } = "";

    public SupportDiagnosticsProductSection Product { get; init; } = new();

    public SupportDiagnosticsEntitlementsSection Entitlements { get; init; } = new();

    public SupportDiagnosticsSecuritySection Security { get; init; } = new();

    public SupportDiagnosticsLicenseSection License { get; init; } = new();

    public IReadOnlyDictionary<string, string?> RedactedConfiguration { get; init; }
        = new Dictionary<string, string?>();
}

public sealed class SupportDiagnosticsProductSection
{
    public string DefaultTenantId { get; init; } = "";

    public int AllowedTenantCount { get; init; }
}

public sealed class SupportDiagnosticsEntitlementsSection
{
    public int MaxCopilotSubmissionsPerHour { get; init; }

    public int Seats { get; init; }

    public int IncludedJobs { get; init; }

    public int MaxConcurrency { get; init; }

    public int RetentionDays { get; init; }

    public bool SsoEnabled { get; init; }

    public bool AuditExportEnabled { get; init; }
}

public sealed class SupportDiagnosticsSecuritySection
{
    public string ExposureProfile { get; init; } = "";

    public string AuthorizationMode { get; init; } = "";

    public string AuthorizationScope { get; init; } = "";

    public bool RequireAuthForCopilotReadApis { get; init; }

    public bool ApiKeyConfigured { get; init; }

    public bool BearerTokenConfigured { get; init; }

    public bool BasicAuthConfigured { get; init; }
}

public sealed class SupportDiagnosticsLicenseSection
{
    public string State { get; init; } = "";

    public string? CustomerId { get; init; }

    public string? TenantId { get; init; }

    public DateTimeOffset? ExpiresAt { get; init; }

    public int Seats { get; init; }

    public string? Detail { get; init; }

    public bool Enforced { get; init; }
}
