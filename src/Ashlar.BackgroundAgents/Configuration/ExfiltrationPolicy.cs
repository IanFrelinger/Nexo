namespace Ashlar.BackgroundAgents.Configuration;

/// <summary>
/// Exfiltration prevention policy.
/// </summary>
public class ExfiltrationPolicy
{
    /// <summary>
    /// Whether to block sending data to external LLM providers.
    /// </summary>
    public bool BlockExternalLLMs { get; set; }

    /// <summary>
    /// Whether to block web search for sensitive data.
    /// </summary>
    public bool BlockWebSearch { get; set; }

    /// <summary>
    /// Whether to block network-based exports.
    /// </summary>
    public bool BlockNetworkExports { get; set; }

    /// <summary>
    /// Whether to require all processing to be local-only.
    /// </summary>
    public bool RequireLocalOnly { get; set; }

    /// <summary>
    /// Whitelist of allowed destinations (optional).
    /// </summary>
    public List<string>? AllowedDestinations { get; set; }

    /// <summary>
    /// Maximum sensitivity level name that can be processed.
    /// </summary>
    public string MaxAllowedLevel { get; set; } = "Public";
}
