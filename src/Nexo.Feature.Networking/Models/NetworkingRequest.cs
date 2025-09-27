using System.Collections.Generic;
using System.Net;

namespace Nexo.Feature.Networking.Models;

/// <summary>
/// Request for networking/multiplayer functionality.
/// This class acts as an orchestrator, delegating specific model categories to partial class implementations.
/// </summary>
public record NetworkingRequest
{
    /// <summary>
    /// Text prompt describing the networking functionality to generate
    /// </summary>
    public string Prompt { get; init; } = string.Empty;
    
    /// <summary>
    /// Type of networking functionality
    /// </summary>
    public NetworkingType NetworkingType { get; init; } = NetworkingType.ClientServer;
    
    /// <summary>
    /// Maximum number of players
    /// </summary>
    public int MaxPlayers { get; init; } = 4;
    
    /// <summary>
    /// Server port
    /// </summary>
    public int Port { get; init; } = 7777;
    
    /// <summary>
    /// Server IP address
    /// </summary>
    public string? ServerIP { get; init; }
    
    /// <summary>
    /// Network protocol to use
    /// </summary>
    public NetworkProtocol Protocol { get; init; } = NetworkProtocol.TCP;
    
    /// <summary>
    /// Security settings
    /// </summary>
    public SecuritySettings Security { get; init; } = new();
    
    /// <summary>
    /// Additional parameters specific to the provider
    /// </summary>
    public Dictionary<string, object> Parameters { get; init; } = new();
}
// This class acts as an orchestrator for various networking model functionalities,
// with specific categories defined in partial classes.