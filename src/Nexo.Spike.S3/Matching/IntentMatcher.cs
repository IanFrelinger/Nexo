using Nexo.Spike.S3.Models;

namespace Nexo.Spike.S3.Matching;

/// <summary>
/// Deterministic intent matching for registry lookup. See <c>docs/spike/s3-intent-matching.md</c>.
/// </summary>
public static class IntentMatcher
{
    public static string ComputeLookupKey(IntentDescriptor descriptor)
    {
        if (!string.IsNullOrWhiteSpace(descriptor.CapabilityKey))
            return NormalizeToken(descriptor.CapabilityKey);

        var tags = string.Join(
            ",",
            descriptor.Tags
                .Select(NormalizeToken)
                .OrderBy(t => t, StringComparer.Ordinal));

        return $"{NormalizeToken(descriptor.IntentKey)}|{tags}";
    }

    public static bool Matches(IntentDescriptor query, IntentDescriptor stored) =>
        string.Equals(ComputeLookupKey(query), ComputeLookupKey(stored), StringComparison.Ordinal);

    private static string NormalizeToken(string value) =>
        value.Trim().ToLowerInvariant();
}
