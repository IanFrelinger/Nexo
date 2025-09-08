using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Agents.Specialized;

namespace Nexo.Feature.AI.Agents.Coordination;

/// <summary>
/// Facilitates communication between specialized agents
/// </summary>
public class AgentCommunicationHub : IAgentCommunicationHub
{
    private readonly ILogger<AgentCommunicationHub> _logger;
    
    public AgentCommunicationHub(ILogger<AgentCommunicationHub> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public async Task BroadcastMessageAsync(string message, IEnumerable<ISpecializedAgent> agents)
    {
        try
        {
            _logger.LogDebug("Broadcasting message to {AgentCount} agents", agents.Count());
            
            var tasks = agents.Select(async agent =>
            {
                try
                {
                    // Create a simple request to communicate the message
                    var request = new AgentRequest
                    {
                        Input = $"Communication: {message}",
                        Context = new Dictionary<string, object>
                        {
                            ["MessageType"] = "Broadcast",
                            ["Timestamp"] = DateTime.UtcNow
                        }
                    };
                    
                    // Process the communication request
                    await agent.ProcessAsync(request);
                    _logger.LogDebug("Message delivered to agent {AgentId}", agent.AgentId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to deliver message to agent {AgentId}", agent.AgentId);
                }
            });
            
            await Task.WhenAll(tasks);
            _logger.LogInformation("Broadcast completed to {AgentCount} agents", agents.Count());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during message broadcast");
            throw;
        }
    }
    
    public async Task<T> RequestFromAgentAsync<T>(ISpecializedAgent agent, string request)
    {
        try
        {
            _logger.LogDebug("Requesting information from agent {AgentId}", agent.AgentId);
            
            var agentRequest = new AgentRequest
            {
                Input = request,
                Context = new Dictionary<string, object>
                {
                    ["RequestType"] = "Information",
                    ["Timestamp"] = DateTime.UtcNow
                }
            };
            
            var response = await agent.ProcessAsync(agentRequest);
            
            if (!response.Success)
            {
                _logger.LogWarning("Agent {AgentId} failed to process request: {Error}", 
                    agent.AgentId, response.ErrorMessage);
                return default(T)!;
            }
            
            // Try to parse the response as the requested type
            if (typeof(T) == typeof(string))
            {
                return (T)(object)response.Result;
            }
            
            // For other types, attempt to deserialize from metadata
            if (response.Metadata?.TryGetValue("Result", out var result) == true && result is T typedResult)
            {
                return typedResult;
            }
            
            _logger.LogWarning("Could not parse response from agent {AgentId} as type {Type}", 
                agent.AgentId, typeof(T).Name);
            return default(T)!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error requesting from agent {AgentId}", agent.AgentId);
            return default(T)!;
        }
    }
    
    public async Task<Dictionary<string, T>> RequestFromMultipleAgentsAsync<T>(
        IEnumerable<ISpecializedAgent> agents, 
        string request)
    {
        try
        {
            _logger.LogDebug("Requesting information from {AgentCount} agents", agents.Count());
            
            var tasks = agents.Select(async agent =>
            {
                try
                {
                    var result = await RequestFromAgentAsync<T>(agent, request);
                    return new { AgentId = agent.AgentId, Result = result };
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to get response from agent {AgentId}", agent.AgentId);
                    return new { AgentId = agent.AgentId, Result = default(T)! };
                }
            });
            
            var results = await Task.WhenAll(tasks);
            
            var responseDict = results.ToDictionary(
                r => r.AgentId, 
                r => r.Result);
            
            _logger.LogDebug("Received responses from {Count} agents", responseDict.Count);
            return responseDict;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error requesting from multiple agents");
            return new Dictionary<string, T>();
        }
    }
}
