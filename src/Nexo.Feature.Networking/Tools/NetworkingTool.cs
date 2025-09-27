using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Agent.Contracts;
using Nexo.Agent.Models;
using Nexo.Feature.Networking.Interfaces;
using Nexo.Feature.Networking.Models;
using Nexo.Feature.Networking.Services;

namespace Nexo.Feature.Networking.Tools;

/// <summary>
/// Agent tool for networking configuration generation
/// </summary>
public partial class NetworkingTool : ITool
{
    private readonly ILogger<NetworkingTool> _logger;
    private readonly NetworkingOrchestrator _orchestrator;

    public NetworkingTool(
        ILogger<NetworkingTool> logger,
        NetworkingOrchestrator orchestrator)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
    }

    public ToolManifest Manifest => new(
        Id: "tool.networking.generation",
        Name: "Networking Generation",
        Version: "1.0.0",
        Description: "Generates networking and multiplayer configurations using AI models",
        Author: "Nexo Networking",
        InputSchema: """
        {
            "type": "object",
            "properties": {
                "prompt": {"type": "string", "description": "Text prompt describing the networking system to generate"},
                "networkingType": {"type": "string", "enum": ["ClientServer", "PeerToPeer", "Hybrid", "DedicatedServer", "CloudMultiplayer", "Custom"], "default": "ClientServer"},
                "maxPlayers": {"type": "integer", "default": 4, "description": "Maximum number of players"},
                "port": {"type": "integer", "default": 7777, "description": "Server port"},
                "serverIP": {"type": "string", "description": "Server IP address"},
                "protocol": {"type": "string", "enum": ["TCP", "UDP", "WebSocket", "HTTP", "Custom"], "default": "TCP"},
                "enableEncryption": {"type": "boolean", "default": true, "description": "Enable encryption"},
                "enableAuthentication": {"type": "boolean", "default": true, "description": "Enable authentication"},
                "enableAntiCheat": {"type": "boolean", "default": false, "description": "Enable anti-cheat"},
                "providerId": {"type": "string", "description": "Specific provider to use (optional)"}
            },
            "required": ["prompt"]
        }
        """,
        OutputSchema: """
        {
            "type": "object",
            "properties": {
                "success": {"type": "boolean"},
                "configuration": {"type": "object", "description": "Generated networking configuration"},
                "filePath": {"type": "string", "description": "File path where configuration was saved"},
                "format": {"type": "string", "description": "Configuration format (JSON, YAML, etc.)"},
                "error": {"type": "string", "description": "Error message if generation failed"},
                "generationTimeMs": {"type": "integer", "description": "Generation time in milliseconds"}
            }
        }
        """,
        RequiredPermissions: ToolPermissions.FileWrite,
        Capabilities: new[] { "networking-generation", "multiplayer", "ai", "offline", "online" },
        Timeout: TimeSpan.FromMinutes(5),
        CreatedAt: DateTime.UtcNow
    );

    public async Task<string> ExecuteAsync(string input, ToolContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Executing networking generation tool with input: {Input}", input);

            var requestData = JsonSerializer.Deserialize<JsonElement>(input);
            
            var request = new NetworkingRequest
            {
                Prompt = requestData.GetProperty("prompt").GetString() ?? string.Empty,
                NetworkingType = Enum.TryParse<NetworkingType>(requestData.GetProperty("networkingType").GetString(), out var networkingType) 
                    ? networkingType : NetworkingType.ClientServer,
                MaxPlayers = requestData.TryGetProperty("maxPlayers", out var maxPlayersProp) 
                    ? maxPlayersProp.GetInt32() : 4,
                Port = requestData.TryGetProperty("port", out var portProp) 
                    ? portProp.GetInt32() : 7777,
                ServerIP = requestData.TryGetProperty("serverIP", out var serverIPProp) 
                    ? serverIPProp.GetString() : null,
                Protocol = Enum.TryParse<NetworkProtocol>(requestData.GetProperty("protocol").GetString(), out var protocol) 
                    ? protocol : NetworkProtocol.TCP,
                Security = new SecuritySettings
                {
                    EnableEncryption = requestData.TryGetProperty("enableEncryption", out var encryptionProp) 
                        ? encryptionProp.GetBoolean() : true,
                    EnableAuthentication = requestData.TryGetProperty("enableAuthentication", out var authProp) 
                        ? authProp.GetBoolean() : true,
                    EnableAntiCheat = requestData.TryGetProperty("enableAntiCheat", out var antiCheatProp) 
                        ? antiCheatProp.GetBoolean() : false
                }
            };

            var providerId = requestData.TryGetProperty("providerId", out var providerProp) 
                ? providerProp.GetString() : null;

            var result = await _orchestrator.GenerateNetworkingConfigurationAsync(request, providerId, cancellationToken);

            var response = new
            {
                success = result.Success,
                configuration = result.Configuration,
                filePath = result.FilePath,
                format = result.Format,
                error = result.Error,
                generationTimeMs = result.GenerationTimeMs
            };

            return JsonSerializer.Serialize(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing networking generation tool");
            
            var errorResponse = new
            {
                success = false,
                error = ex.Message,
                generationTimeMs = 0
            };

            return JsonSerializer.Serialize(errorResponse);
        }
    }
}

