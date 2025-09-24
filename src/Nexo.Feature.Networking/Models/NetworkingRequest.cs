using System.Collections.Generic;
using System.Net;

namespace Nexo.Feature.Networking.Models;

/// <summary>
/// Request for networking/multiplayer functionality
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

/// <summary>
/// Result of networking functionality generation
/// </summary>
public record NetworkingResult
{
    /// <summary>
    /// Whether the generation was successful
    /// </summary>
    public bool Success { get; init; }
    
    /// <summary>
    /// Generated networking configuration
    /// </summary>
    public NetworkingConfiguration? Configuration { get; init; }
    
    /// <summary>
    /// File path where configuration was saved
    /// </summary>
    public string? FilePath { get; init; }
    
    /// <summary>
    /// Configuration format (JSON, YAML, etc.)
    /// </summary>
    public string Format { get; init; } = "JSON";
    
    /// <summary>
    /// Error message if generation failed
    /// </summary>
    public string? Error { get; init; }
    
    /// <summary>
    /// Generation time in milliseconds
    /// </summary>
    public long GenerationTimeMs { get; init; }
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
/// Security settings for networking
/// </summary>
public record SecuritySettings
{
    /// <summary>
    /// Enable encryption
    /// </summary>
    public bool EnableEncryption { get; init; } = true;
    
    /// <summary>
    /// Enable authentication
    /// </summary>
    public bool EnableAuthentication { get; init; } = true;
    
    /// <summary>
    /// Enable anti-cheat
    /// </summary>
    public bool EnableAntiCheat { get; init; } = false;
    
    /// <summary>
    /// Enable rate limiting
    /// </summary>
    public bool EnableRateLimiting { get; init; } = true;
    
    /// <summary>
    /// Maximum connections per IP
    /// </summary>
    public int MaxConnectionsPerIP { get; init; } = 5;
    
    /// <summary>
    /// Connection timeout in seconds
    /// </summary>
    public int ConnectionTimeoutSeconds { get; init; } = 30;
}

/// <summary>
/// Networking configuration
/// </summary>
public record NetworkingConfiguration
{
    /// <summary>
    /// Configuration name
    /// </summary>
    public string Name { get; init; } = string.Empty;
    
    /// <summary>
    /// Networking type
    /// </summary>
    public NetworkingType Type { get; init; }
    
    /// <summary>
    /// Server configuration
    /// </summary>
    public ServerConfiguration Server { get; init; } = new();
    
    /// <summary>
    /// Client configuration
    /// </summary>
    public ClientConfiguration Client { get; init; } = new();
    
    /// <summary>
    /// Security settings
    /// </summary>
    public SecuritySettings Security { get; init; } = new();
    
    /// <summary>
    /// Network events
    /// </summary>
    public List<NetworkEvent> Events { get; init; } = new();
    
    /// <summary>
    /// Message types
    /// </summary>
    public List<MessageType> MessageTypes { get; init; } = new();
}

/// <summary>
/// Server configuration
/// </summary>
public record ServerConfiguration
{
    /// <summary>
    /// Server IP address
    /// </summary>
    public string IPAddress { get; init; } = "0.0.0.0";
    
    /// <summary>
    /// Server port
    /// </summary>
    public int Port { get; init; } = 7777;
    
    /// <summary>
    /// Maximum players
    /// </summary>
    public int MaxPlayers { get; init; } = 4;
    
    /// <summary>
    /// Server name
    /// </summary>
    public string Name { get; init; } = "Nexo Game Server";
    
    /// <summary>
    /// Server description
    /// </summary>
    public string Description { get; init; } = "Generated by Nexo";
    
    /// <summary>
    /// Tick rate (updates per second)
    /// </summary>
    public int TickRate { get; init; } = 60;
    
    /// <summary>
    /// Enable server discovery
    /// </summary>
    public bool EnableDiscovery { get; init; } = true;
}

/// <summary>
/// Client configuration
/// </summary>
public record ClientConfiguration
{
    /// <summary>
    /// Default server IP
    /// </summary>
    public string DefaultServerIP { get; init; } = "127.0.0.1";
    
    /// <summary>
    /// Default server port
    /// </summary>
    public int DefaultServerPort { get; init; } = 7777;
    
    /// <summary>
    /// Connection timeout
    /// </summary>
    public int ConnectionTimeoutMs { get; init; } = 5000;
    
    /// <summary>
    /// Reconnection attempts
    /// </summary>
    public int ReconnectionAttempts { get; init; } = 3;
    
    /// <summary>
    /// Enable auto-reconnect
    /// </summary>
    public bool EnableAutoReconnect { get; init; } = true;
}

/// <summary>
/// Network event
/// </summary>
public record NetworkEvent
{
    /// <summary>
    /// Event name
    /// </summary>
    public string Name { get; init; } = string.Empty;
    
    /// <summary>
    /// Event type
    /// </summary>
    public NetworkEventType Type { get; init; }
    
    /// <summary>
    /// Event description
    /// </summary>
    public string Description { get; init; } = string.Empty;
    
    /// <summary>
    /// Event parameters
    /// </summary>
    public List<EventParameter> Parameters { get; init; } = new();
    
    /// <summary>
    /// Event handler code
    /// </summary>
    public string HandlerCode { get; init; } = string.Empty;
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
/// Event parameter
/// </summary>
public record EventParameter
{
    /// <summary>
    /// Parameter name
    /// </summary>
    public string Name { get; init; } = string.Empty;
    
    /// <summary>
    /// Parameter type
    /// </summary>
    public string Type { get; init; } = string.Empty;
    
    /// <summary>
    /// Parameter description
    /// </summary>
    public string Description { get; init; } = string.Empty;
    
    /// <summary>
    /// Whether parameter is required
    /// </summary>
    public bool Required { get; init; } = true;
}

/// <summary>
/// Message type definition
/// </summary>
public record MessageType
{
    /// <summary>
    /// Message type name
    /// </summary>
    public string Name { get; init; } = string.Empty;
    
    /// <summary>
    /// Message ID
    /// </summary>
    public int Id { get; init; }
    
    /// <summary>
    /// Message description
    /// </summary>
    public string Description { get; init; } = string.Empty;
    
    /// <summary>
    /// Message fields
    /// </summary>
    public List<MessageField> Fields { get; init; } = new();
    
    /// <summary>
    /// Whether message is reliable
    /// </summary>
    public bool Reliable { get; init; } = true;
    
    /// <summary>
    /// Message priority
    /// </summary>
    public MessagePriority Priority { get; init; } = MessagePriority.Normal;
}

/// <summary>
/// Message field
/// </summary>
public record MessageField
{
    /// <summary>
    /// Field name
    /// </summary>
    public string Name { get; init; } = string.Empty;
    
    /// <summary>
    /// Field type
    /// </summary>
    public string Type { get; init; } = string.Empty;
    
    /// <summary>
    /// Field description
    /// </summary>
    public string Description { get; init; } = string.Empty;
    
    /// <summary>
    /// Whether field is required
    /// </summary>
    public bool Required { get; init; } = true;
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

