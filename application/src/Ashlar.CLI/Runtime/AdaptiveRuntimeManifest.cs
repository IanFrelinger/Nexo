namespace Ashlar.CLI.Runtime;

/// <summary>
/// Optional user/runtime context that can steer adaptive execution.
/// </summary>
public sealed record AdaptiveRuntimeManifest
{
    /// <summary>
    /// Domain packs relevant to the user objective (e.g. personal, unity, ui).
    /// </summary>
    public string[] DomainPacks { get; init; } = Array.Empty<string>();

    /// <summary>
    /// User preferences that may influence generated behavior and UX.
    /// </summary>
    public Dictionary<string, string> Preferences { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// UI/runtime capabilities expected by the user or target surface.
    /// </summary>
    public string[] UiCapabilities { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Optional QA policy profile override: demo | prod | research.
    /// </summary>
    public string? QaPolicyProfile { get; init; }

    /// <summary>Returns an empty default adaptive runtime manifest.</summary>
    public static AdaptiveRuntimeManifest Default() => new();
}
