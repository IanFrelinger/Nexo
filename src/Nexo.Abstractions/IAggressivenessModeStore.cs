namespace Nexo.Abstractions;

/// <summary>
/// Store for the current aggressiveness mode of background agents.
/// Mode is switchable at runtime without restart.
/// </summary>
public interface IAggressivenessModeStore
{
    /// <summary>Gets the current aggressiveness mode.</summary>
    BackgroundAgentAggressivenessMode GetMode();

    /// <summary>Sets the aggressiveness mode.</summary>
    void SetMode(BackgroundAgentAggressivenessMode mode);
}
