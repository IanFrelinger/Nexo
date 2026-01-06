using Nexo.Orchestration.Playtest.Models;

namespace Nexo.Orchestration.Playtest.Ports;

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

/// <summary>
/// Current state of the game.
/// 
/// Contains:
/// - Game over status
/// - Player health and position
/// - Current objective
/// - Currency and equipped weapon
/// - Nearby enemies and items
/// - Available actions
/// 
/// Returned by IGameRunner to represent current game state.
/// Used by AIPlayerAgent to make decisions.
/// </summary>
public sealed record GameState
{
    public bool IsGameOver { get; init; }
    public double PlayerHealth { get; init; }
    public double PlayerMaxHealth { get; init; }
    public string PlayerPosition { get; init; } = "";
    public string? CurrentObjective { get; init; }
    public int Currency { get; init; }
    public string? EquippedWeapon { get; init; }
    public IReadOnlyList<string> NearbyEnemies { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> NearbyItems { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> AvailableActions { get; init; } = Array.Empty<string>();
}

