using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Bricks;

namespace Nexo.Infrastructure.Execution;

/// <summary>
/// Manages execution state for active WebSocket sessions.
/// </summary>
public class ExecutionStateManager : IExecutionStateManager
{
    private readonly Dictionary<string, string> _providers = new();
    private readonly Dictionary<string, bool> _offlineStates = new();
    private readonly Dictionary<string, Dictionary<string, ImplementationType>> _brickImplementations = new();
    private readonly ILogger<ExecutionStateManager> _logger;
    
    public ExecutionStateManager(ILogger<ExecutionStateManager> logger)
    {
        _logger = logger;
    }
    
    public void SetProvider(string connectionId, string provider)
    {
        _providers[connectionId] = provider;
        _logger.LogDebug("Set provider {Provider} for connection {ConnectionId}", provider, connectionId);
    }
    
    public string GetCurrentProvider(string connectionId)
    {
        return _providers.TryGetValue(connectionId, out var provider) ? provider : "openai";
    }
    
    public void SetOffline(string connectionId, bool offline)
    {
        _offlineStates[connectionId] = offline;
        _logger.LogDebug("Set offline mode {Offline} for connection {ConnectionId}", offline, connectionId);
    }
    
    public bool IsOffline(string connectionId)
    {
        return _offlineStates.TryGetValue(connectionId, out var offline) && offline;
    }
    
    public void SetBrickImplementation(string connectionId, string brickId, ImplementationType implementation)
    {
        if (!_brickImplementations.TryGetValue(connectionId, out var implementations))
        {
            implementations = new Dictionary<string, ImplementationType>();
            _brickImplementations[connectionId] = implementations;
        }
        
        implementations[brickId] = implementation;
        _logger.LogDebug("Set implementation {Implementation} for brick {BrickId} on connection {ConnectionId}", 
            implementation, brickId, connectionId);
    }
    
    public ImplementationType? GetBrickImplementation(string connectionId, string brickId)
    {
        if (_brickImplementations.TryGetValue(connectionId, out var implementations))
        {
            if (implementations.TryGetValue(brickId, out var implementation))
            {
                return implementation;
            }
        }
        return null;
    }
}

