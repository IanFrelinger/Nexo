using System;
using System.Collections.Generic;
using System.Linq;
using Nexo.Feature.Networking.Models;

namespace Nexo.Feature.Networking.Providers;

/// <summary>
/// Message type generation for OllamaNetworkingProvider.
/// </summary>
public partial class OllamaNetworkingProvider
{
    /// <summary>
    /// Generates message types
    /// </summary>
    private List<MessageType> GenerateMessageTypes(NetworkingRequest request)
    {
        var messageTypes = new List<MessageType>
        {
            new()
            {
                Name = "PlayerPosition",
                Id = 1,
                Description = "Player position update",
                Fields = new List<MessageField>
                {
                    new() { Name = "PlayerId", Type = "int", Description = "Player identifier", Required = true },
                    new() { Name = "X", Type = "float", Description = "X coordinate", Required = true },
                    new() { Name = "Y", Type = "float", Description = "Y coordinate", Required = true },
                    new() { Name = "Z", Type = "float", Description = "Z coordinate", Required = true }
                },
                Reliable = false,
                Priority = MessagePriority.High
            },
            new()
            {
                Name = "PlayerAction",
                Id = 2,
                Description = "Player action message",
                Fields = new List<MessageField>
                {
                    new() { Name = "PlayerId", Type = "int", Description = "Player identifier", Required = true },
                    new() { Name = "Action", Type = "string", Description = "Action type", Required = true },
                    new() { Name = "Data", Type = "byte[]", Description = "Action data", Required = false }
                },
                Reliable = true,
                Priority = MessagePriority.Normal
            },
            new()
            {
                Name = "ChatMessage",
                Id = 3,
                Description = "Chat message",
                Fields = new List<MessageField>
                {
                    new() { Name = "PlayerId", Type = "int", Description = "Player identifier", Required = true },
                    new() { Name = "Message", Type = "string", Description = "Chat message", Required = true },
                    new() { Name = "Timestamp", Type = "long", Description = "Message timestamp", Required = true }
                },
                Reliable = true,
                Priority = MessagePriority.Low
            }
        };

        // Add message types based on networking type
        switch (request.NetworkingType)
        {
            case NetworkingType.ClientServer:
                messageTypes.AddRange(GenerateClientServerMessageTypes());
                break;
            case NetworkingType.PeerToPeer:
                messageTypes.AddRange(GeneratePeerToPeerMessageTypes());
                break;
        }

        return messageTypes;
    }

    /// <summary>
    /// Generates client-server message types
    /// </summary>
    private List<MessageType> GenerateClientServerMessageTypes()
    {
        return new List<MessageType>
        {
            new()
            {
                Name = "ServerCommand",
                Id = 100,
                Description = "Server command message",
                Fields = new List<MessageField>
                {
                    new() { Name = "Command", Type = "string", Description = "Command type", Required = true },
                    new() { Name = "Parameters", Type = "string[]", Description = "Command parameters", Required = false }
                },
                Reliable = true,
                Priority = MessagePriority.Critical
            }
        };
    }

    /// <summary>
    /// Generates peer-to-peer message types
    /// </summary>
    private List<MessageType> GeneratePeerToPeerMessageTypes()
    {
        return new List<MessageType>
        {
            new()
            {
                Name = "PeerDiscovery",
                Id = 200,
                Description = "Peer discovery message",
                Fields = new List<MessageField>
                {
                    new() { Name = "PeerId", Type = "string", Description = "Peer identifier", Required = true },
                    new() { Name = "Address", Type = "string", Description = "Peer address", Required = true },
                    new() { Name = "Port", Type = "int", Description = "Peer port", Required = true }
                },
                Reliable = false,
                Priority = MessagePriority.Normal
            }
        };
    }
}
