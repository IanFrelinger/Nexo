using Microsoft.Extensions.Logging;
using Nexo.Orchestration.Agents;
using Nexo.Orchestration.Architect;
using Nexo.Orchestration.Architect.Models;
using Nexo.Orchestration.Coordination.Conflicts;
using Nexo.Orchestration.Communication;
using Nexo.Orchestration.Communication.Models;

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
        ILogger<Orchestrator> logger)
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
            
            // Escalate critical conflicts
            foreach (var conflict in conflicts.Where(c => c.Severity == ConflictSeverity.Critical))
            {
                _escalationManager.EscalateConflict(conflict, "Pre-execution conflict detection");
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

            // Step 6: Check for unresolved conflicts and escalate
            var unresolvedConflicts = conflicts.Where(c => c.Severity >= ConflictSeverity.Medium).ToList();
            if (unresolvedConflicts.Count > 0)
            {
                foreach (var conflict in unresolvedConflicts)
                {
                    _escalationManager.EscalateConflict(conflict, "Post-execution conflict");
                }
            }

            // Step 7: Shutdown all agents
            await _lifecycleManager.ShutdownAllAsync(cancellationToken);

            return new OrchestrationResult
            {
                Success = integratedOutput.IsValid,
                IntegratedOutput = integratedOutput,
                Decomposition = decomposition,
                Conflicts = conflicts,
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
    public IReadOnlyList<Escalation> Escalations { get; init; } = Array.Empty<Escalation>();
    public ProgressSummary? ProgressSummary { get; init; }
}

