using Microsoft.AspNetCore.SignalR;
using Nexo.Core.Domain.Agents;
using Nexo.Core.Domain.Behaviors;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution.Events;
using Nexo.Infrastructure.Execution;

namespace Nexo.Demo.Api.Hubs;

/// <summary>
/// SignalR hub interface for client communication.
/// </summary>
public interface IExecutionClient
{
    Task OnBehaviorStarted(BehaviorStartedEvent evt);
    Task OnStepStarted(StepStartedEvent evt);
    Task OnStepCompleted(StepCompletedEvent evt);
    Task OnStepSkipped(StepSkippedEvent evt);
    Task OnStepError(StepErrorEvent evt);
    Task OnCacheHit(CacheHitEvent evt);
    Task OnBehaviorCompleted(BehaviorCompletedEvent evt);
    Task OnProviderSwitched(ProviderSwitchedEvent evt);
    Task OnOfflineModeActivated(OfflineModeActivatedEvent evt);
}

/// <summary>
/// SignalR hub for streaming execution events to clients.
/// </summary>
public class ExecutionHub : Hub<IExecutionClient>
{
    private readonly IBehaviorExecutor _executor;
    private readonly IAgentRegistry _agentRegistry;
    private readonly IBehaviorRegistry _behaviorRegistry;
    private readonly IExecutionStateManager _stateManager;
    private readonly ILogger<ExecutionHub> _logger;
    
    public ExecutionHub(
        IBehaviorExecutor executor,
        IAgentRegistry agentRegistry,
        IBehaviorRegistry behaviorRegistry,
        IExecutionStateManager stateManager,
        ILogger<ExecutionHub> logger)
    {
        _executor = executor;
        _agentRegistry = agentRegistry;
        _behaviorRegistry = behaviorRegistry;
        _stateManager = stateManager;
        _logger = logger;
    }
    
    /// <summary>
    /// Execute a behavior and stream events to the client.
    /// </summary>
    public async Task ExecuteBehavior(ExecuteBehaviorRequest request)
    {
        var agent = _agentRegistry.GetAgent(request.AgentId);
        if (agent == null)
        {
            await Clients.Caller.OnStepError(new StepErrorEvent(
                "init", $"Agent not found: {request.AgentId}"));
            return;
        }
        
        var behavior = _behaviorRegistry.GetBehavior(request.BehaviorId);
        if (behavior == null)
        {
            await Clients.Caller.OnStepError(new StepErrorEvent(
                "init", $"Behavior not found: {request.BehaviorId}"));
            return;
        }
        
        var input = new BehaviorInput(request.Parameters);
        var options = new ExecutionOptions
        {
            IsAirGapped = request.IsOffline,
            AuditMode = request.AuditMode,
            Provider = request.Provider ?? "openai"
        };
        
        var cancellationToken = Context.ConnectionAborted;
        
        await foreach (var evt in _executor.ExecuteWithEventsAsync(
            agent, behavior, input, options, cancellationToken))
        {
            await DispatchEvent(evt);
        }
    }
    
    /// <summary>
    /// Switch provider mid-execution (for demo).
    /// </summary>
    public async Task SwitchProvider(string provider)
    {
        var currentProvider = _stateManager.GetCurrentProvider(Context.ConnectionId);
        _stateManager.SetProvider(Context.ConnectionId, provider);
        await Clients.Caller.OnProviderSwitched(new ProviderSwitchedEvent(
            currentProvider,
            provider));
    }
    
    /// <summary>
    /// Toggle offline mode.
    /// </summary>
    public async Task SetOfflineMode(bool offline)
    {
        _stateManager.SetOffline(Context.ConnectionId, offline);
        if (offline)
        {
            await Clients.Caller.OnOfflineModeActivated(new OfflineModeActivatedEvent());
        }
    }
    
    /// <summary>
    /// Update implementation for a specific brick in the current session.
    /// </summary>
    public Task SetBrickImplementation(string brickId, string implementation)
    {
        if (Enum.TryParse<ImplementationType>(implementation, ignoreCase: true, out var implType))
        {
            _stateManager.SetBrickImplementation(Context.ConnectionId, brickId, implType);
        }
        return Task.CompletedTask;
    }
    
    private async Task DispatchEvent(ExecutionEvent evt)
    {
        var task = evt switch
        {
            BehaviorStartedEvent e => Clients.Caller.OnBehaviorStarted(e),
            StepStartedEvent e => Clients.Caller.OnStepStarted(e),
            StepCompletedEvent e => Clients.Caller.OnStepCompleted(e),
            StepSkippedEvent e => Clients.Caller.OnStepSkipped(e),
            StepErrorEvent e => Clients.Caller.OnStepError(e),
            CacheHitEvent e => Clients.Caller.OnCacheHit(e),
            BehaviorCompletedEvent e => Clients.Caller.OnBehaviorCompleted(e),
            _ => Task.CompletedTask
        };
        await task;
    }
}

/// <summary>
/// Request to execute a behavior.
/// </summary>
public record ExecuteBehaviorRequest(
    string AgentId,
    string BehaviorId,
    Dictionary<string, object> Parameters,
    bool IsOffline = false,
    bool AuditMode = false,
    string? Provider = null
);

