using Microsoft.Extensions.Options;

namespace Ashlar.Transport.A2A;

/// <summary>
/// Fail-closed startup validation for <see cref="A2ATransportOptions"/> — the A2A client
/// transport dials external agents, so it refuses to enable under the AirGapped profile and
/// verifies referenced API-key environment variables are present at boot.
/// </summary>
public sealed class ValidateA2ATransportOptions : IValidateOptions<A2ATransportOptions>
{
    /// <summary>Deployment-profile environment variable honored by the Ashlar kernel.</summary>
    public const string DeploymentProfileVariable = "ASHLAR_DEPLOYMENT_PROFILE";

    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, A2ATransportOptions options)
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
                $"{nameof(A2ATransportOptions.Enabled)}=true is not permitted under the AirGapped deployment profile " +
                $"({DeploymentProfileVariable}={profile}). The A2A transport dials external agents and stays off.");
        }

        foreach (var endpoint in options.Endpoints)
        {
            var label = string.IsNullOrWhiteSpace(endpoint.UrlPrefix) ? "<empty prefix>" : endpoint.UrlPrefix;

            if (string.IsNullOrWhiteSpace(endpoint.UrlPrefix) ||
                !Uri.TryCreate(endpoint.UrlPrefix, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                failures.Add($"A2A endpoint '{label}': UrlPrefix must be an absolute http(s) URL prefix.");
            }

            var hasHeader = !string.IsNullOrWhiteSpace(endpoint.ApiKeyHeader);
            var hasEnvVar = !string.IsNullOrWhiteSpace(endpoint.ApiKeyEnvVar);
            if (hasHeader != hasEnvVar)
            {
                failures.Add($"A2A endpoint '{label}': ApiKeyHeader and ApiKeyEnvVar must be set together.");
            }
            else if (hasEnvVar && string.IsNullOrEmpty(Environment.GetEnvironmentVariable(endpoint.ApiKeyEnvVar!)))
            {
                failures.Add($"A2A endpoint '{label}': environment variable '{endpoint.ApiKeyEnvVar}' is referenced but empty or unset.");
            }
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
