using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Nexo.Abstractions;
using Nexo.BackgroundAgents.Configuration;
using Nexo.BackgroundAgents.Extending;
using Nexo.BackgroundAgents.Logging;
using Nexo.BackgroundAgents.Optimization;
using Nexo.BackgroundAgents.Scheduling;
using Nexo.BackgroundAgents.Testing;
using Nexo.Core.Application.Trust.Ports;

namespace Nexo.BackgroundAgents.Registry;

/// <summary>
/// Registry for managing background agent instances.
///
/// Provides:
/// - Agent registration and lifecycle management
/// - State tracking
/// - Execution coordination
/// - Agent lookup
///
/// Thread-safe implementation using concurrent collections.
/// </summary>
public interface IBackgroundAgentRegistry
{
    /// <summary>
    /// Register a background agent.
    /// </summary>
    /// <param name="agent">The agent instance.</param>
    /// <param name="config">The agent configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RegisterAsync(IAgent agent, BackgroundAgentConfig config, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get an agent instance by ID.
    /// </summary>
    /// <param name="agentId">The agent ID.</param>
    /// <returns>The agent instance, or null if not found.</returns>
    BackgroundAgentInstance? GetAgent(string agentId);

    /// <summary>
    /// Get all registered agents.
    /// </summary>
    /// <returns>All registered agent instances.</returns>
    IReadOnlyList<BackgroundAgentInstance> GetAll();

    /// <summary>
    /// Start all registered agents.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task StartAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stop all registered agents.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task StopAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Start a specific agent.
    /// </summary>
    /// <param name="agentId">The agent ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task StartAsync(string agentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stop a specific agent.
    /// </summary>
    /// <param name="agentId">The agent ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task StopAsync(string agentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Run one execution cycle for an agent (for manual/testing use).
    /// </summary>
    /// <param name="agentId">The agent ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ExecuteOnceAsync(string agentId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of IBackgroundAgentRegistry.
/// </summary>
public sealed class BackgroundAgentRegistry : IBackgroundAgentRegistry
{
    private readonly ConcurrentDictionary<string, BackgroundAgentInstance> _agents = new();
    private readonly IAgentScheduler _scheduler;
    private readonly ILogger<BackgroundAgentRegistry>? _logger;
    private readonly IBackgroundAgentLogStore? _logStore;
    private readonly ICodeAnalysisRunner? _codeAnalysisRunner;
    private readonly ITestRunRunner? _testRunRunner;
    private readonly ISelfExtendRunner? _selfExtendRunner;
    private readonly IAggressivenessModeStore? _modeStore;
    private readonly IApprovalGate? _approvalGate;
    private readonly IDataDecisionAuditLog? _auditLog;

    /// <summary>
    /// Initializes a new instance of the <see cref="BackgroundAgentRegistry"/> class.
    /// Agent creation is done by the host (e.g. BackgroundAgentService) before RegisterAsync.
    /// </summary>
    /// <param name="scheduler">Scheduler for agent execution loops.</param>
    /// <param name="logger">Optional logger.</param>
    /// <param name="logStore">Optional log store for agent execution logs.</param>
    /// <param name="codeAnalysisRunner">Optional runner for optimizer agents (dog-food: run analysis on codebase).</param>
    /// <param name="testRunRunner">Optional runner for tester agents (dog-food: run framework tests).</param>
    /// <param name="selfExtendRunner">Optional runner for extender agents (dog-food: LLM-driven code/doc changes within policy).</param>
    /// <param name="modeStore">Optional aggressiveness mode store. When provided, Passive mode skips extender execution.</param>
    /// <param name="approvalGate">Optional approval gate for SemiActive mode. When provided and mode is SemiActive, execution requires approval.</param>
    /// <param name="auditLog">Optional audit log. When provided and mode is Ambient, actions are logged here (no user notification).</param>
    public BackgroundAgentRegistry(
        IAgentScheduler scheduler,
        ILogger<BackgroundAgentRegistry>? logger = null,
        IBackgroundAgentLogStore? logStore = null,
        ICodeAnalysisRunner? codeAnalysisRunner = null,
        ITestRunRunner? testRunRunner = null,
        ISelfExtendRunner? selfExtendRunner = null,
        IAggressivenessModeStore? modeStore = null,
        IApprovalGate? approvalGate = null,
        IDataDecisionAuditLog? auditLog = null)
    {
        _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
        _logger = logger;
        _logStore = logStore;
        _codeAnalysisRunner = codeAnalysisRunner;
        _testRunRunner = testRunRunner;
        _selfExtendRunner = selfExtendRunner;
        _modeStore = modeStore;
        _approvalGate = approvalGate;
        _auditLog = auditLog;
    }

    /// <summary>
    /// Register a background agent.
    /// </summary>
    public Task RegisterAsync(IAgent agent, BackgroundAgentConfig config, CancellationToken cancellationToken = default)
    {
        if (agent == null)
            throw new ArgumentNullException(nameof(agent));
        if (config == null)
            throw new ArgumentNullException(nameof(config));

        // Create agent instance
        var instance = new BackgroundAgentInstance
        {
            Agent = agent,
            Config = config,
            State = BackgroundAgentState.Idle
        };

        _agents[config.Id] = instance;
        _logger?.LogInformation("Registered background agent: {AgentId}", config.Id);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Get an agent instance by ID.
    /// </summary>
    public BackgroundAgentInstance? GetAgent(string agentId)
    {
        return _agents.TryGetValue(agentId, out var instance) ? instance : null;
    }

    /// <summary>
    /// Get all registered agents.
    /// </summary>
    public IReadOnlyList<BackgroundAgentInstance> GetAll()
    {
        return _agents.Values.ToList();
    }

    /// <summary>
    /// Start all registered agents.
    /// </summary>
    public Task StartAllAsync(CancellationToken cancellationToken = default)
    {
        var tasks = _agents.Values
            .Where(a => a.Config.Enabled)
            .Select(a => StartAsync(a.Config.Id, cancellationToken));
        return Task.WhenAll(tasks);
    }

    /// <summary>
    /// Stop all registered agents.
    /// </summary>
    public Task StopAllAsync(CancellationToken cancellationToken = default)
    {
        var tasks = _agents.Values.Select(a => StopAsync(a.Config.Id, cancellationToken));
        return Task.WhenAll(tasks);
    }

    /// <summary>
    /// Start a specific agent.
    /// </summary>
    public Task StartAsync(string agentId, CancellationToken cancellationToken = default)
    {
        if (!_agents.TryGetValue(agentId, out var instance))
        {
            throw new InvalidOperationException($"Agent {agentId} not found");
        }

        if (instance.State == BackgroundAgentState.Running)
        {
            _logger?.LogWarning("Agent {AgentId} is already running", agentId);
            return Task.CompletedTask;
        }

        instance.State = BackgroundAgentState.Starting;
        instance.LastStartedAt = DateTimeOffset.UtcNow;

        _scheduler.StartAsync(instance, ExecuteAgentAsync, cancellationToken);

        instance.State = BackgroundAgentState.Running;
        _logger?.LogInformation("Started background agent: {AgentId}", agentId);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Stop a specific agent.
    /// </summary>
    public Task StopAsync(string agentId, CancellationToken cancellationToken = default)
    {
        if (!_agents.TryGetValue(agentId, out var instance))
        {
            throw new InvalidOperationException($"Agent {agentId} not found");
        }

        if (instance.State == BackgroundAgentState.Stopped || instance.State == BackgroundAgentState.Idle)
        {
            return Task.CompletedTask;
        }

        instance.State = BackgroundAgentState.Stopping;

        _scheduler.Stop(agentId);

        instance.State = BackgroundAgentState.Stopped;
        _logger?.LogInformation("Stopped background agent: {AgentId}", agentId);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task ExecuteOnceAsync(string agentId, CancellationToken cancellationToken = default)
    {
        if (!_agents.TryGetValue(agentId, out var instance))
            throw new InvalidOperationException($"Agent {agentId} not found");
        await ExecuteAgentAsync(instance, cancellationToken).ConfigureAwait(false);
    }

    private async Task ExecuteAgentAsync(BackgroundAgentInstance instance, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var agentId = instance.Config.Id;
        try
        {
            instance.ExecutionCount++;
            _logStore?.Append(agentId, "Info", "Executing background agent.");
            _logger?.LogDebug("Executing background agent: {AgentId}", agentId);

            // Dog-food: optimizer agents run code analysis when Path/AnalysisPath is set and runner is registered
            if (string.Equals(instance.Config.Role, "optimizer", StringComparison.OrdinalIgnoreCase) &&
                _codeAnalysisRunner != null &&
                TryGetParameter(instance.Config, new[] { "Path", "AnalysisPath" }, out var analysisPath))
            {
                var result = await _codeAnalysisRunner.RunAsync(analysisPath, cancellationToken).ConfigureAwait(false);
                _logStore?.Append(agentId, result.Success ? "Info" : "Warning",
                    $"Code analysis: {result.Summary} (violations: {result.ViolationCount})");
                _logger?.LogDebug("Background agent {AgentId} code analysis: {Summary}", agentId, result.Summary);
                instance.LastCompletedAt = DateTimeOffset.UtcNow;
                instance.SuccessCount++;
                _logStore?.Append(agentId, "Info", "Execution completed successfully.");
                return;
            }

            // Dog-food: tester agents run tests when test runner is registered (optional Filter from Parameters)
            if (string.Equals(instance.Config.Role, "tester", StringComparison.OrdinalIgnoreCase) &&
                _testRunRunner != null)
            {
                var filter = TryGetParameter(instance.Config, new[] { "Filter" }, out var f) ? f : null;
                var result = await _testRunRunner.RunAsync(filter, cancellationToken).ConfigureAwait(false);
                _logStore?.Append(agentId, result.Success ? "Info" : "Warning",
                    $"Tests: {result.Summary} (total: {result.TotalTests}, failed: {result.FailedTests})");
                _logger?.LogDebug("Background agent {AgentId} test run: {Summary}", agentId, result.Summary);
                instance.LastCompletedAt = DateTimeOffset.UtcNow;
                instance.SuccessCount++;
                _logStore?.Append(agentId, "Info", "Execution completed successfully.");
                return;
            }

            // Dog-food: extender agents run self-extend cycle (LLM + tools) when runner is registered
            if (string.Equals(instance.Config.Role, "extender", StringComparison.OrdinalIgnoreCase) &&
                _selfExtendRunner != null &&
                TryGetParameter(instance.Config, new[] { "RepoRoot", "Path" }, out var repoRoot))
            {
                var mode = _modeStore?.GetMode() ?? BackgroundAgentAggressivenessMode.Active;
                if (mode == BackgroundAgentAggressivenessMode.Passive)
                {
                    _logStore?.Append(agentId, "Info", "Passive mode: skipping extender execution (observe only).");
                    _logger?.LogDebug("Background agent {AgentId} in Passive mode: extender skipped", agentId);
                    instance.LastCompletedAt = DateTimeOffset.UtcNow;
                    instance.SuccessCount++;
                    return;
                }

                if (mode == BackgroundAgentAggressivenessMode.SemiActive)
                {
                    var approvalResult = _approvalGate != null
                        ? await _approvalGate.RequestApprovalAsync(
                            $"Extender agent {agentId} requests approval to run self-extend cycle.",
                            TimeSpan.FromSeconds(30),
                            cancellationToken).ConfigureAwait(false)
                        : ApprovalResult.Denied;
                    if (approvalResult != ApprovalResult.Approved)
                    {
                        var reason = approvalResult == ApprovalResult.TimedOut ? "timeout" : "denied";
                        _logStore?.Append(agentId, "Info", $"SemiActive mode: execution skipped ({reason}).");
                        _logger?.LogDebug("Background agent {AgentId} in SemiActive mode: extender skipped ({Reason})", agentId, reason);
                        instance.LastCompletedAt = DateTimeOffset.UtcNow;
                        instance.SuccessCount++;
                        return;
                    }
                }

                var result = await _selfExtendRunner.RunAsync(repoRoot, cancellationToken).ConfigureAwait(false);

                if (mode == BackgroundAgentAggressivenessMode.Ambient)
                {
                    _auditLog?.LogAmbientAction(agentId, result.Summary, result.ToolCallsExecuted);
                    _logStore?.Append(agentId, "Info", $"Ambient: executed silently ({result.ToolCallsExecuted} tool calls).");
                    _logger?.LogDebug("Background agent {AgentId} in Ambient mode: executed silently", agentId);
                }
                else
                {
                    _logStore?.Append(agentId, result.Success ? "Info" : "Warning",
                        $"Self-extend: {result.Summary} (executed: {result.ToolCallsExecuted}, denied: {result.ToolCallsDenied})");
                    _logger?.LogDebug("Background agent {AgentId} self-extend: {Summary}", agentId, result.Summary);
                }

                instance.LastCompletedAt = DateTimeOffset.UtcNow;
                instance.SuccessCount++;
                _logStore?.Append(agentId, "Info", "Execution completed successfully.");
                return;
            }

            // Default: simple success (full agent ThinkAsync + toolbox can be wired later)
            var observation = new AgentObservation(new WorldSnapshot(0, new Dictionary<string, object?>
            {
                ["agentId"] = agentId,
                ["timestamp"] = DateTimeOffset.UtcNow
            }));

            // Execute agent (this would need a toolbox and memory - simplified for now).
            // When integrating: use BackgroundAgentPolicyEngineFactory.Create(registry, sensitivityRegistry)
            // as the PolicyEngine for the host so tool calls are enforced by DataExfiltrationPolicy.
            // var actions = await instance.Agent.ThinkAsync(observation, toolbox, memory, cancellationToken);

            instance.LastCompletedAt = DateTimeOffset.UtcNow;
            instance.SuccessCount++;
            _logStore?.Append(agentId, "Info", "Execution completed successfully.");
            _logger?.LogDebug("Background agent {AgentId} executed successfully", agentId);
            return;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            instance.FailureCount++;
            instance.LastError = ex.Message;
            _logStore?.Append(agentId, "Error", $"Execution failed: {ex.Message}");
            _logger?.LogError(ex, "Background agent {AgentId} execution failed", agentId);
            return;
        }
    }

    private static bool TryGetParameter(BackgroundAgentConfig config, string[] keys, out string value)
    {
        value = null!;
        if (config.Parameters == null || config.Parameters.Count == 0)
            return false;
        foreach (var key in keys)
        {
            if (config.Parameters.TryGetValue(key, out var obj) && obj is string s && !string.IsNullOrWhiteSpace(s))
            {
                value = s;
                return true;
            }
        }
        return false;
    }
}
