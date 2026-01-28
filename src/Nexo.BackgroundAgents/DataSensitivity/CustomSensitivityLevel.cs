namespace Nexo.BackgroundAgents.DataSensitivity;

/// <summary>
/// Configuration model for custom sensitivity levels.
/// 
/// Used for deserializing custom sensitivity levels from configuration files.
/// </summary>
public class CustomSensitivityLevel
{
    /// <summary>
    /// Level name (must be unique).
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Sensitivity value for ordering (lower = less sensitive, higher = more sensitive).
    /// </summary>
    public int SensitivityValue { get; set; }

    /// <summary>
    /// Whether this level allows external LLM calls.
    /// </summary>
    public bool AllowsExternalLLM { get; set; }

    /// <summary>
    /// Whether this level allows web search.
    /// </summary>
    public bool AllowsWebSearch { get; set; }

    /// <summary>
    /// Whether this level requires local-only processing.
    /// </summary>
    public bool RequiresLocalOnly { get; set; }

    /// <summary>
    /// Whether this level allows network exports.
    /// </summary>
    public bool AllowsNetworkExports { get; set; }

    /// <summary>
    /// Description of this sensitivity level.
    /// </summary>
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Configurable sensitivity level implementation.
/// 
/// Created from CustomSensitivityLevel configuration.
/// </summary>
public sealed record ConfigurableSensitivityLevel(
    string Value,
    string Display,
    int SensitivityValue,
    bool AllowsExternalLLM,
    bool AllowsWebSearch,
    bool RequiresLocalOnly,
    bool AllowsNetworkExports,
    string Description) : IDataSensitivityLevel;
