using Microsoft.Extensions.Options;

namespace Nexo.Transport.A2A.Server;

/// <summary>
/// Fail-closed startup validation for <see cref="NexoA2AServerOptions"/> (wired via
/// <c>ValidateOnStart</c>).
/// </summary>
public sealed class ValidateNexoA2AServerOptions : IValidateOptions<NexoA2AServerOptions>
{
    /// <summary>Deployment-profile environment variable honored by the Nexo kernel.</summary>
    public const string DeploymentProfileVariable = "NEXO_DEPLOYMENT_PROFILE";

    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, NexoA2AServerOptions options)
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
                $"{nameof(NexoA2AServerOptions.Enabled)}=true is not permitted under the AirGapped deployment profile " +
                $"({DeploymentProfileVariable}={profile}). The A2A server is a network protocol surface and stays off.");
        }

        if (string.IsNullOrWhiteSpace(options.PublicBaseUrl) ||
            !Uri.TryCreate(options.PublicBaseUrl, UriKind.Absolute, out var baseUrl) ||
            (baseUrl.Scheme != Uri.UriSchemeHttp && baseUrl.Scheme != Uri.UriSchemeHttps))
        {
            failures.Add(
                $"{nameof(NexoA2AServerOptions.PublicBaseUrl)} must be an absolute http(s) URL when the A2A server is " +
                "enabled — it is embedded into agent cards that remote agents dial back.");
        }

        if (options.DefaultExecutionTimeout <= TimeSpan.Zero)
        {
            failures.Add($"{nameof(NexoA2AServerOptions.DefaultExecutionTimeout)} must be positive.");
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
