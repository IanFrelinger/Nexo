using Nexo.Orchestration.Playtest.Models;

namespace Nexo.Orchestration.Playtest.Ports;

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
    /// <summary>Whether the game has ended.</summary>
    public bool IsGameOver { get; init; }

    /// <summary>Current player health points.</summary>
    public double PlayerHealth { get; init; }

    /// <summary>Maximum player health points.</summary>
    public double PlayerMaxHealth { get; init; }

    /// <summary>Current player position label or coordinates.</summary>
    public string PlayerPosition { get; init; } = "";

    /// <summary>Active objective description, if any.</summary>
    public string? CurrentObjective { get; init; }

    /// <summary>Player currency balance.</summary>
    public int Currency { get; init; }

    /// <summary>Currently equipped weapon name.</summary>
    public string? EquippedWeapon { get; init; }

    /// <summary>Enemy identifiers near the player.</summary>
    public IReadOnlyList<string> NearbyEnemies { get; init; } = Array.Empty<string>();

    /// <summary>Item identifiers near the player.</summary>
    public IReadOnlyList<string> NearbyItems { get; init; } = Array.Empty<string>();

    /// <summary>Actions the player can take next.</summary>
    public IReadOnlyList<string> AvailableActions { get; init; } = Array.Empty<string>();
}
