using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace Ashlar.Mcp.Client;

/// <summary>
/// Fail-closed startup validation for <see cref="AshlarMcpClientOptions"/> (wired via
/// <c>ValidateOnStart</c>). Beyond shape checks, this verifies referenced API-key environment
/// variables actually hold values — a configured-but-missing secret should stop the host at
/// boot, not surface as anonymous requests to a remote server at 3am.
/// </summary>
public sealed class ValidateAshlarMcpClientOptions : IValidateOptions<AshlarMcpClientOptions>
{
    /// <summary>Deployment-profile environment variable honored by the Ashlar kernel.</summary>
    public const string DeploymentProfileVariable = "ASHLAR_DEPLOYMENT_PROFILE";

    private static readonly Regex ServerNamePattern = new("^[A-Za-z0-9_-]+$", RegexOptions.Compiled);

    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, AshlarMcpClientOptions options)
    {
        if (!options.Enabled)
        {
            return ValidateOptionsResult.Success;
        }

        var failures = new List<string>();

        var profile = Environment.GetEnvironmentVariable(DeploymentProfileVariable);
        if (string.Equals(profile, "airgapped", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(profile, "air-gapped", StringComparison.OrdinalIgnoreCase))
        {
            failures.Add(
                $"{nameof(AshlarMcpClientOptions.Enabled)}=true is not permitted under the AirGapped deployment profile " +
                $"({DeploymentProfileVariable}={profile}). The MCP client dials external servers and stays off.");
        }

        if (options.ConnectTimeout <= TimeSpan.Zero)
        {
            failures.Add($"{nameof(AshlarMcpClientOptions.ConnectTimeout)} must be positive (got {options.ConnectTimeout}).");
        }

        if (options.ToolListRefreshInterval < TimeSpan.Zero)
        {
            failures.Add($"{nameof(AshlarMcpClientOptions.ToolListRefreshInterval)} must be zero (disabled) or positive.");
        }

        var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var server in options.Servers)
        {
            var label = string.IsNullOrWhiteSpace(server.Name) ? "<unnamed>" : server.Name;

            if (string.IsNullOrWhiteSpace(server.Name) || !ServerNamePattern.IsMatch(server.Name))
            {
                failures.Add($"Server '{label}': Name must match [A-Za-z0-9_-]+ (it becomes the mcp:{{name}}:{{tool}} id prefix).");
            }
            else if (!seenNames.Add(server.Name))
            {
                failures.Add($"Server '{label}': duplicate server name.");
            }

            if (string.IsNullOrWhiteSpace(server.Url) ||
                !Uri.TryCreate(server.Url, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                failures.Add($"Server '{label}': Url must be an absolute http(s) URI (stdio servers are not supported in v1).");
            }

            var hasHeader = !string.IsNullOrWhiteSpace(server.ApiKeyHeader);
            var hasEnvVar = !string.IsNullOrWhiteSpace(server.ApiKeyEnvVar);
            if (hasHeader != hasEnvVar)
            {
                failures.Add($"Server '{label}': ApiKeyHeader and ApiKeyEnvVar must be set together.");
            }
            else if (hasEnvVar && string.IsNullOrEmpty(Environment.GetEnvironmentVariable(server.ApiKeyEnvVar!)))
            {
                failures.Add($"Server '{label}': environment variable '{server.ApiKeyEnvVar}' is referenced but empty or unset.");
            }
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
