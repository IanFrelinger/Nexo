using Ashlar.Orchestration.Playtest.Models;

namespace Ashlar.Orchestration.Playtest.Ports;

/// <summary>
/// Port for running games in headless mode for playtesting.
/// 
/// Defines the contract for game runner adapters:
/// - Start/stop game sessions
/// - Get current game state
/// - Execute actions in the game
/// - Monitor game status
/// 
/// Implementations provide game engine-specific logic for headless execution.
/// Used by AIPlayerAgent for automated playtesting.
/// </summary>
public interface IGameRunner
{
    /// <summary>
    /// Starts a game session.
    /// </summary>
    Task StartAsync(string buildPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current game state.
    /// </summary>
    Task<GameState> GetGameStateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes an action in the game.
    /// </summary>
    Task ExecuteActionAsync(string actionType, IReadOnlyDictionary<string, object> parameters, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the game session.
    /// </summary>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Whether the game is currently running.
    /// </summary>
    bool IsRunning { get; }
}
