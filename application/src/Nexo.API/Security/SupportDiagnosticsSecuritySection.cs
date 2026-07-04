using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Nexo.API.Security;

/// <summary>Built-in security posture included in support diagnostics export.</summary>
public sealed class SupportDiagnosticsSecuritySection
{
    /// <summary>Configured exposure profile name.</summary>
    public string ExposureProfile { get; init; } = "";

    /// <summary>Configured authorization mode name.</summary>
    public string AuthorizationMode { get; init; } = "";

    /// <summary>Configured authorization scope name.</summary>
    public string AuthorizationScope { get; init; } = "";

    /// <summary>Whether copilot read APIs require authentication.</summary>
    public bool RequireAuthForCopilotReadApis { get; init; }

    /// <summary>Whether an API key secret is configured.</summary>
    public bool ApiKeyConfigured { get; init; }

    /// <summary>Whether a bearer token secret is configured.</summary>
    public bool BearerTokenConfigured { get; init; }

    /// <summary>Whether basic auth credentials are configured.</summary>
    public bool BasicAuthConfigured { get; init; }
}
