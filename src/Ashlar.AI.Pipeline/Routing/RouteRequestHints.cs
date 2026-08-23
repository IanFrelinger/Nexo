using Microsoft.Extensions.AI;

namespace Ashlar.AI.Pipeline.Routing;

/// <summary>
/// Resolves a capability tier from <see cref="ChatOptions"/>.
/// </summary>
public static class RouteRequestHints
{
    /// <summary>AdditionalProperties key for capability tier (<c>fast</c>/<c>balanced</c>/<c>heavy</c>).</summary>
    public const string CapabilityTierKey = "ashlar.route.capability";

    /// <summary>AdditionalProperties key for an explicit preferred target key.</summary>
    public const string PreferredTargetKey = "ashlar.route.preferred_target";

    /// <summary>Reads the requested capability tier (defaults to <see cref="RouteCapabilityTier.Fast"/>).</summary>
    public static RouteCapabilityTier GetCapabilityTier(ChatOptions? options)
    {
        if (options?.AdditionalProperties is null)
        {
            return RouteCapabilityTier.Fast;
        }

        if (!options.AdditionalProperties.TryGetValue(CapabilityTierKey, out var raw) || raw is null)
        {
            return RouteCapabilityTier.Fast;
        }

        var text = raw as string ?? raw.ToString();
        if (string.IsNullOrWhiteSpace(text))
        {
            return RouteCapabilityTier.Fast;
        }

        if (Enum.TryParse<RouteCapabilityTier>(text, ignoreCase: true, out var parsed))
        {
            return parsed;
        }

        return text.Trim().ToLowerInvariant() switch
        {
            "fast" => RouteCapabilityTier.Fast,
            "balanced" => RouteCapabilityTier.Balanced,
            "heavy" => RouteCapabilityTier.Heavy,
            _ => RouteCapabilityTier.Fast,
        };
    }

    /// <summary>Optional explicit preferred target override.</summary>
    public static string? GetPreferredTarget(ChatOptions? options)
    {
        if (options?.AdditionalProperties is null)
        {
            return null;
        }

        if (!options.AdditionalProperties.TryGetValue(PreferredTargetKey, out var raw) || raw is null)
        {
            return null;
        }

        var text = raw as string ?? raw.ToString();
        return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
    }
}
