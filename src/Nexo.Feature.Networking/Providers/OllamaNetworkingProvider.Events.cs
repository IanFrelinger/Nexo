using System;
using System.Collections.Generic;
using System.Linq;
using Nexo.Feature.Networking.Models;

namespace Nexo.Feature.Networking.Providers;

/// <summary>
/// Event generation for OllamaNetworkingProvider.
/// </summary>
public partial class OllamaNetworkingProvider
{
    /// <summary>
    /// Generates network events
    /// </summary>
    private List<NetworkEvent> GenerateNetworkEvents(NetworkingRequest request)
    {
        var events = new List<NetworkEvent>();

        // Standard events for all networking types
        events.Add(new NetworkEvent
        {
            Name = "PlayerConnected",
            Type = NetworkEventType.PlayerConnected,
            Description = "Triggered when a player connects to the server",
            Parameters = new List<EventParameter>
            {
                new() { Name = "PlayerId", Type = "int", Description = "Unique player identifier", Required = true },
                new() { Name = "PlayerName", Type = "string", Description = "Player display name", Required = true },
                new() { Name = "IPAddress", Type = "string", Description = "Player IP address", Required = false }
            },
            HandlerCode = GeneratePlayerConnectedHandler()
        });

        events.Add(new NetworkEvent
        {
            Name = "PlayerDisconnected",
            Type = NetworkEventType.PlayerDisconnected,
            Description = "Triggered when a player disconnects from the server",
            Parameters = new List<EventParameter>
            {
                new() { Name = "PlayerId", Type = "int", Description = "Unique player identifier", Required = true },
                new() { Name = "Reason", Type = "string", Description = "Disconnection reason", Required = false }
            },
            HandlerCode = GeneratePlayerDisconnectedHandler()
        });

        // Add events based on networking type
        switch (request.NetworkingType)
        {
            case NetworkingType.ClientServer:
                events.AddRange(GenerateClientServerEvents());
                break;
            case NetworkingType.PeerToPeer:
                events.AddRange(GeneratePeerToPeerEvents());
                break;
            case NetworkingType.DedicatedServer:
                events.AddRange(GenerateDedicatedServerEvents());
                break;
            case NetworkingType.CloudMultiplayer:
                events.AddRange(GenerateCloudMultiplayerEvents());
                break;
        }

        return events;
    }

    /// <summary>
    /// Generates client-server events
    /// </summary>
    private List<NetworkEvent> GenerateClientServerEvents()
    {
        return new List<NetworkEvent>
        {
            new()
            {
                Name = "GameStarted",
                Type = NetworkEventType.GameStarted,
                Description = "Triggered when the game starts",
                Parameters = new List<EventParameter>
                {
                    new() { Name = "GameMode", Type = "string", Description = "Game mode identifier", Required = true },
                    new() { Name = "MapName", Type = "string", Description = "Map name", Required = true }
                },
                HandlerCode = GenerateGameStartedHandler()
            },
            new()
            {
                Name = "GameEnded",
                Type = NetworkEventType.GameEnded,
                Description = "Triggered when the game ends",
                Parameters = new List<EventParameter>
                {
                    new() { Name = "WinnerId", Type = "int", Description = "Winner player ID", Required = false },
                    new() { Name = "Score", Type = "int", Description = "Final score", Required = false }
                },
                HandlerCode = GenerateGameEndedHandler()
            }
        };
    }

    /// <summary>
    /// Generates peer-to-peer events
    /// </summary>
    private List<NetworkEvent> GeneratePeerToPeerEvents()
    {
        return new List<NetworkEvent>
        {
            new()
            {
                Name = "PeerJoined",
                Type = NetworkEventType.Custom,
                Description = "Triggered when a peer joins the network",
                Parameters = new List<EventParameter>
                {
                    new() { Name = "PeerId", Type = "string", Description = "Peer identifier", Required = true },
                    new() { Name = "PeerAddress", Type = "string", Description = "Peer network address", Required = true }
                },
                HandlerCode = GeneratePeerJoinedHandler()
            }
        };
    }

    /// <summary>
    /// Generates dedicated server events
    /// </summary>
    private List<NetworkEvent> GenerateDedicatedServerEvents()
    {
        return new List<NetworkEvent>
        {
            new()
            {
                Name = "ServerShutdown",
                Type = NetworkEventType.Custom,
                Description = "Triggered when the server shuts down",
                Parameters = new List<EventParameter>
                {
                    new() { Name = "Reason", Type = "string", Description = "Shutdown reason", Required = true }
                },
                HandlerCode = GenerateServerShutdownHandler()
            }
        };
    }

    /// <summary>
    /// Generates cloud multiplayer events
    /// </summary>
    private List<NetworkEvent> GenerateCloudMultiplayerEvents()
    {
        return new List<NetworkEvent>
        {
            new()
            {
                Name = "CloudSessionCreated",
                Type = NetworkEventType.Custom,
                Description = "Triggered when a cloud session is created",
                Parameters = new List<EventParameter>
                {
                    new() { Name = "SessionId", Type = "string", Description = "Cloud session ID", Required = true },
                    new() { Name = "Region", Type = "string", Description = "Cloud region", Required = true }
                },
                HandlerCode = GenerateCloudSessionCreatedHandler()
            }
        };
    }
}
