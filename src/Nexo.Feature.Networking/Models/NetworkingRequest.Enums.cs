namespace Nexo.Feature.Networking.Models;

/// <summary>
/// Enums and enumeration types for networking functionality
/// </summary>
public partial class NetworkingRequest
{
    // This partial class contains enumeration types
}

/// <summary>
/// Types of networking functionality
/// </summary>
public enum NetworkingType
{
    /// <summary>
    /// Client-server architecture
    /// </summary>
    ClientServer,
    
    /// <summary>
    /// Peer-to-peer networking
    /// </summary>
    PeerToPeer,
    
    /// <summary>
    /// Hybrid networking
    /// </summary>
    Hybrid,
    
    /// <summary>
    /// Dedicated server
    /// </summary>
    DedicatedServer,
    
    /// <summary>
    /// Cloud-based multiplayer
    /// </summary>
    CloudMultiplayer,
    
    /// <summary>
    /// Custom networking setup
    /// </summary>
    Custom
}

/// <summary>
/// Network protocols
/// </summary>
public enum NetworkProtocol
{
    /// <summary>
    /// Transmission Control Protocol
    /// </summary>
    TCP,
    
    /// <summary>
    /// User Datagram Protocol
    /// </summary>
    UDP,
    
    /// <summary>
    /// WebSocket protocol
    /// </summary>
    WebSocket,
    
    /// <summary>
    /// HTTP/HTTPS
    /// </summary>
    HTTP,
    
    /// <summary>
    /// Custom protocol
    /// </summary>
    Custom
}

/// <summary>
/// Network event types
/// </summary>
public enum NetworkEventType
{
    /// <summary>
    /// Player connected
    /// </summary>
    PlayerConnected,
    
    /// <summary>
    /// Player disconnected
    /// </summary>
    PlayerDisconnected,
    
    /// <summary>
    /// Player joined game
    /// </summary>
    PlayerJoined,
    
    /// <summary>
    /// Player left game
    /// </summary>
    PlayerLeft,
    
    /// <summary>
    /// Game started
    /// </summary>
    GameStarted,
    
    /// <summary>
    /// Game ended
    /// </summary>
    GameEnded,
    
    /// <summary>
    /// Custom event
    /// </summary>
    Custom
}

/// <summary>
/// Message priority levels
/// </summary>
public enum MessagePriority
{
    /// <summary>
    /// Low priority
    /// </summary>
    Low,
    
    /// <summary>
    /// Normal priority
    /// </summary>
    Normal,
    
    /// <summary>
    /// High priority
    /// </summary>
    High,
    
    /// <summary>
    /// Critical priority
    /// </summary>
    Critical
}
