namespace Nexo.BackgroundAgents.Configuration;

/// <summary>
/// Aggressiveness level for background agent autonomous action.
/// Controls whether agents observe only, suggest, or implement changes.
/// </summary>
public enum BackgroundAgentAggressivenessMode
{
    /// <summary>
    /// Observe only; surface suggestions, no autonomous action.
    /// </summary>
    Passive,

    /// <summary>
    /// Suggest + implement with approval gate.
    /// </summary>
    SemiActive,

    /// <summary>
    /// Implement and report.
    /// </summary>
    Active,

    /// <summary>
    /// Implement without notification; log everything.
    /// </summary>
    Ambient
}
