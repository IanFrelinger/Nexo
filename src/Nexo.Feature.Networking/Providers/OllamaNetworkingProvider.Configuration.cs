using System;
using System.Collections.Generic;
using System.Linq;
using Nexo.Feature.Networking.Models;

namespace Nexo.Feature.Networking.Providers;

/// <summary>
/// Configuration generation for OllamaNetworkingProvider.
/// </summary>
public partial class OllamaNetworkingProvider
{
    /// <summary>
    /// Generates simulated networking configuration
    /// </summary>
    private NetworkingConfiguration GenerateSimulatedNetworkingConfiguration(NetworkingRequest request)
    {
        var configuration = new NetworkingConfiguration
        {
            Name = $"Generated_{request.NetworkingType}_{Guid.NewGuid():N}",
            Type = request.NetworkingType,
            Server = new ServerConfiguration
            {
                IPAddress = request.ServerIP ?? "0.0.0.0",
                Port = request.Port,
                MaxPlayers = request.MaxPlayers,
                Name = $"Nexo {request.NetworkingType} Server",
                Description = $"Generated server for: {request.Prompt}",
                TickRate = 60,
                EnableDiscovery = true
            },
            Client = new ClientConfiguration
            {
                DefaultServerIP = request.ServerIP ?? "127.0.0.1",
                DefaultServerPort = request.Port,
                ConnectionTimeoutMs = 5000,
                ReconnectionAttempts = 3,
                EnableAutoReconnect = true
            },
            Security = request.Security,
            Events = GenerateNetworkEvents(request),
            MessageTypes = GenerateMessageTypes(request)
        };

        return configuration;
    }
}
