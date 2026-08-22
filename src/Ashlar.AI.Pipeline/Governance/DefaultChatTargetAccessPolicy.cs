namespace Ashlar.AI.Pipeline.Governance;

/// <summary>
/// Default local-first access policy: allows all <c>local:*</c> targets; denies <c>cloud:*</c>
/// unless explicitly listed in <see cref="AllowedCloudTargets"/>.
/// </summary>
public sealed class DefaultChatTargetAccessPolicy : IChatTargetAccessPolicy
{
    /// <summary>Optional allow-list of cloud target keys.</summary>
    public ISet<string> AllowedCloudTargets { get; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public bool IsAllowed(string? callerIdentity, string targetKey, string? modelId, out string? denyReason)
    {
        _ = callerIdentity;
        _ = modelId;

        if (string.IsNullOrWhiteSpace(targetKey))
        {
            denyReason = "Target key is required.";
            return false;
        }

        if (targetKey.StartsWith("local:", StringComparison.OrdinalIgnoreCase)
            || targetKey.StartsWith("peer:", StringComparison.OrdinalIgnoreCase))
        {
            denyReason = null;
            return true;
        }

        if (targetKey.StartsWith("cloud:", StringComparison.OrdinalIgnoreCase))
        {
            if (AllowedCloudTargets.Contains(targetKey))
            {
                denyReason = null;
                return true;
            }

            denyReason = $"Cloud target '{targetKey}' is not permitted by the active policy pack.";
            return false;
        }

        denyReason = $"Unknown target trust tier for '{targetKey}'.";
        return false;
    }
}
