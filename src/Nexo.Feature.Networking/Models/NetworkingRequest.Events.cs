using System.Collections.Generic;

namespace Nexo.Feature.Networking.Models;

/// <summary>
/// Event and message models for networking functionality
/// </summary>
public partial class NetworkingRequest
{
    // This partial class contains event and message models
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
