namespace Nexo.Abstractions;

/// <summary>
/// Store for the current aggressiveness mode of background agents.
/// Mode is switchable at runtime without restart.
/// </summary>
public interface IAggressivenessModeStore
{
    /// <summary>Gets the current aggressiveness mode.</summary>
    BackgroundAgentAggressivenessMode GetMode();

    /// <summary>Sets the aggressiveness mode at runtime without restarting background agents.</summary>
    /// <param name="mode">New aggressiveness mode to apply.</param>
    void SetMode(BackgroundAgentAggressivenessMode mode);
}
