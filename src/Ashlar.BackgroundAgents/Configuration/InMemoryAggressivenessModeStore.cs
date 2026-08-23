using Ashlar.Abstractions;

namespace Ashlar.BackgroundAgents.Configuration;

/// <summary>
/// Thread-safe in-memory store for aggressiveness mode.
/// </summary>
public sealed class InMemoryAggressivenessModeStore : IAggressivenessModeStore
{
    private volatile BackgroundAgentAggressivenessMode _mode = BackgroundAgentAggressivenessMode.Active;

    /// <inheritdoc />
    public BackgroundAgentAggressivenessMode GetMode() => _mode;

    /// <inheritdoc />
    public void SetMode(BackgroundAgentAggressivenessMode mode) => _mode = mode;
}
