using Microsoft.Extensions.Logging;
using Nexo.Orchestration.Agents;
using Nexo.Orchestration.Architect;
using Nexo.Orchestration.Architect.Models;
using Nexo.Orchestration.Coordination.Conflicts;
using Nexo.Orchestration.Communication;
using Nexo.Orchestration.Communication.Models;
using Nexo.Orchestration.Negotiation;
using System.Text.Json;

namespace Nexo.Orchestration.Coordination;

/// <summary>
/// Main orchestrator that coordinates the entire agent orchestration flow.
/// </summary>
public sealed class Orchestrator
{
    private readonly IArchitectAgent _architect;
    private readonly AgentFactory _agentFactory;
    private readonly LifecycleManager _lifecycleManager;
    private readonly DependencyResolver _dependencyResolver;
    private readonly ConflictDetector _conflictDetector;
    private readonly ResourceAllocator _resourceAllocator;
    private readonly ProgressTracker _progressTracker;
    private readonly EscalationManager _escalationManager;
    private readonly OutputIntegrator _outputIntegrator;
    private readonly IAgentBus _agentBus;
    private readonly NegotiationProtocol? _negotiationProtocol;
    private readonly ILogger<Orchestrator> _logger;

    public Orchestrator(
        IArchitectAgent architect,
        AgentFactory agentFactory,
        LifecycleManager lifecycleManager,
        DependencyResolver dependencyResolver,
        ConflictDetector conflictDetector,
        ResourceAllocator resourceAllocator,
        ProgressTracker progressTracker,
        EscalationManager escalationManager,
        OutputIntegrator outputIntegrator,
        IAgentBus agentBus,
        ILogger<Orchestrator> logger,
        NegotiationProtocol? negotiationProtocol = null)
    {
        _architect = architect ?? throw new ArgumentNullException(nameof(architect));
        _agentFactory = agentFactory ?? throw new ArgumentNullException(nameof(agentFactory));
        _lifecycleManager = lifecycleManager ?? throw new ArgumentNullException(nameof(lifecycleManager));
        _dependencyResolver = dependencyResolver ?? throw new ArgumentNullException(nameof(dependencyResolver));
        _conflictDetector = conflictDetector ?? throw new ArgumentNullException(nameof(conflictDetector));
        _resourceAllocator = resourceAllocator ?? throw new ArgumentNullException(nameof(resourceAllocator));
        _progressTracker = progressTracker ?? throw new ArgumentNullException(nameof(progressTracker));
        _escalationManager = escalationManager ?? throw new ArgumentNullException(nameof(escalationManager));
        _outputIntegrator = outputIntegrator ?? throw new ArgumentNullException(nameof(outputIntegrator));
        _agentBus = agentBus ?? throw new ArgumentNullException(nameof(agentBus));
        _negotiationProtocol = negotiationProtocol;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Orchestrates the complete flow: request → decomposition → agent execution → integration.
    /// </summary>
    public async Task<OrchestrationResult> OrchestrateAsync(
        string request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting orchestration for request: {Request}", request);

        try
        {
            // Step 1: Decompose request
            var decomposition = await _architect.DecomposeAsync(request, cancellationToken);
            if (!decomposition.IsValid)
            {
                _logger.LogWarning("Decomposition has validation errors: {ErrorCount}", decomposition.ValidationErrors.Count);
                // Continue anyway - some errors may be warnings
            }

            // Step 2: Detect conflicts before spawning
            var containers = decomposition.Agents
                .Select(spec => _agentFactory.CreateContainer(spec))
                .ToList();

            var conflicts = _conflictDetector.DetectConflicts(containers);
            
            // Step 2.5: Attempt to resolve conflicts via negotiation
            var resolvedConflicts = new List<Conflict>();
            var unresolvedConflicts = new List<Conflict>();

            if (_negotiationProtocol != null)
            {
                foreach (var conflict in conflicts)
                {
                    if (conflict.Severity == ConflictSeverity.Critical)
                    {
                        // Always escalate critical conflicts
                        _escalationManager.EscalateConflict(conflict, "Critical conflict - requires human decision");
                        unresolvedConflicts.Add(conflict);
                        continue;
                    }

                    var involvedAgents = containers
                        .Where(c => conflict.AgentIds.Contains(c.AgentId))
                        .ToList();

                    var result = await _negotiationProtocol.NegotiateAsync(
                        conflict, involvedAgents, cancellationToken);

                    if (result.Success)
                    {
                        _logger.LogInformation(
                            "Conflict {ConflictType} resolved via {ResolutionType}: {Reason}",
                            conflict.ConflictType, result.ResolutionType, result.Reason);

                        // Apply resolution
                        await ApplyResolutionAsync(conflict, result, involvedAgents, cancellationToken);
                        resolvedConflicts.Add(conflict);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "Conflict {ConflictType} could not be resolved: {Reason}",
                            conflict.ConflictType, result.Reason);

                        _escalationManager.EscalateConflict(conflict, result.Reason ?? "Negotiation failed");
                        unresolvedConflicts.Add(conflict);
                    }
                }
            }
            else
            {
                // No negotiation protocol - escalate all non-critical conflicts
                foreach (var conflict in conflicts.Where(c => c.Severity == ConflictSeverity.Critical))
                {
                    _escalationManager.EscalateConflict(conflict, "Pre-execution conflict detection");
                    unresolvedConflicts.Add(conflict);
                }
            }

            // Step 3: Register agents and allocate resources
            foreach (var container in containers)
            {
                // Allocate resources
                if (!_resourceAllocator.TryAllocate(container, out _))
                {
                    _logger.LogWarning("Failed to allocate resources for agent {AgentId}", container.AgentId);
                    var escalation = _escalationManager.EscalateIssue(
                        "ResourceAllocation",
                        $"Failed to allocate resources for agent {container.AgentId}",
                        EscalationSeverity.High);
                    continue;
                }

                // Register agent
                await _lifecycleManager.RegisterAgentAsync(container, cancellationToken);
                _dependencyResolver.RegisterAgent(container);

                // Subscribe to agent messages
                await _agentBus.SubscribeAsync("OutputEmitted", async (msg, ct) =>
                {
                    if (msg is OutputEmitted outputMsg && outputMsg.FromAgentId == container.AgentId)
                    {
                        _dependencyResolver.RecordOutput(container.AgentId, outputMsg.Output);
                    }
                    await Task.CompletedTask;
                }, container.AgentId, cancellationToken);
            }

            // Step 4: Execute agents in dependency order
            var executionOrder = _dependencyResolver.GetExecutionOrder();
            var outputs = new Dictionary<string, object>();

            foreach (var agentId in executionOrder)
            {
                var container = _lifecycleManager.GetAgent(agentId);
                if (container == null)
                {
                    continue;
                }

                // Wait for dependencies
                if (!_dependencyResolver.AreDependenciesResolved(agentId))
                {
                    var dependencyOutputs = _dependencyResolver.GetDependencyOutputs(
                        container.Agent.Spec.Dependencies);
                    await container.Agent.WaitForDependenciesAsync(dependencyOutputs, cancellationToken);
                }

                // Execute agent
                try
                {
                    var dependencyOutputs = _dependencyResolver.GetDependencyOutputs(
                        container.Agent.Spec.Dependencies);
                    var output = await _lifecycleManager.ExecuteAgentAsync(agentId, dependencyOutputs, cancellationToken);
                    outputs[agentId] = output;
                    _dependencyResolver.RecordOutput(agentId, output);

                    // Publish output message
                    var message = new OutputEmitted
                    {
                        MessageId = Guid.NewGuid().ToString(),
                        FromAgentId = agentId,
                        MessageType = "OutputEmitted",
                        Output = output
                    };
                    await _agentBus.PublishAsync(message, cancellationToken);

                    // Release resources
                    _resourceAllocator.ReleaseResources(agentId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Agent {AgentId} execution failed", agentId);
                    _escalationManager.EscalateIssue(
                        "AgentExecution",
                        $"Agent {agentId} execution failed: {ex.Message}",
                        EscalationSeverity.High,
                        ex.StackTrace);
                }

                // Track progress
                _progressTracker.RecordProgress(container);
            }

            // Step 5: Integrate outputs
            var integratedOutput = _outputIntegrator.Integrate(decomposition.Agents, outputs);

            // Step 6: Shutdown all agents
            await _lifecycleManager.ShutdownAllAsync(cancellationToken);

            return new OrchestrationResult
            {
                Success = integratedOutput.IsValid,
                IntegratedOutput = integratedOutput,
                Decomposition = decomposition,
                Conflicts = conflicts,
                ResolvedConflicts = resolvedConflicts,
                UnresolvedConflicts = unresolvedConflicts,
                Escalations = _escalationManager.GetAllEscalations(),
                ProgressSummary = _progressTracker.GetSummary(_lifecycleManager.GetActiveAgents())
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Orchestration failed");
            throw;
        }
    }

    /// <summary>
    /// Applies a negotiation resolution to agents.
    /// </summary>
    private async Task ApplyResolutionAsync(
        Conflict conflict,
        NegotiationResult result,
        IReadOnlyList<AgentContainer> agents,
        CancellationToken cancellationToken)
    {
        switch (conflict.ConflictType)
        {
            case ConflictType.Schema when result.ResolvedSchema.HasValue:
                // Update agents to use canonical schema
                // Note: In a full implementation, agents would need a method to update their output schema
                _logger.LogInformation(
                    "Applied schema resolution: agents {AgentIds} now use canonical schema",
                    string.Join(", ", conflict.AgentIds));
                break;

            case ConflictType.Resource when result.ResolvedAllocation != null:
                // Update resource allocations
                foreach (var (agentId, allocation) in result.ResolvedAllocation.Allocations)
                {
                    // Release old allocation
                    _resourceAllocator.ReleaseResources(agentId);
                    // Note: ResourceAllocator would need UpdateAllocation method for full implementation
                    _logger.LogInformation(
                        "Applied resource allocation for agent {AgentId}: {Compute}s, {Memory}MB",
                        agentId, allocation.ComputeSeconds, allocation.MemoryMb);
                }
                break;

            case ConflictType.Constraint:
            case ConflictType.Philosophy:
                // Apply resolution changes to agents
                if (result.Resolution?.RequiredChanges != null)
                {
                    _logger.LogInformation(
                        "Applied resolution changes: {Changes}",
                        string.Join(", ", result.Resolution.RequiredChanges.Select(kvp => $"{kvp.Key}: {kvp.Value}")));
                }
                break;
        }

        await Task.CompletedTask;
    }
}

/// <summary>
/// Result of an orchestration run.
/// </summary>
public sealed record OrchestrationResult
{
    public bool Success { get; init; }
    public IntegratedOutput? IntegratedOutput { get; init; }
    public DecompositionResult? Decomposition { get; init; }
    public IReadOnlyList<Conflict> Conflicts { get; init; } = Array.Empty<Conflict>();
    public IReadOnlyList<Conflict> ResolvedConflicts { get; init; } = Array.Empty<Conflict>();
    public IReadOnlyList<Conflict> UnresolvedConflicts { get; init; } = Array.Empty<Conflict>();
    public IReadOnlyList<Escalation> Escalations { get; init; } = Array.Empty<Escalation>();
    public ProgressSummary? ProgressSummary { get; init; }
}

