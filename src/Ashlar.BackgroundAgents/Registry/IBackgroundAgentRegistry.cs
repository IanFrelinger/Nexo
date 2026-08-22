using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Ashlar.Abstractions;
using Ashlar.BackgroundAgents.Configuration;
using Ashlar.BackgroundAgents.DataSensitivity;
using Ashlar.BackgroundAgents.Extending;
using Ashlar.BackgroundAgents.Logging;
using Ashlar.BackgroundAgents.Observations;
using Ashlar.BackgroundAgents.Optimization;
using Ashlar.BackgroundAgents.Scheduling;
using Ashlar.BackgroundAgents.Security;
using Ashlar.BackgroundAgents.Telemetry;
using Ashlar.BackgroundAgents.Testing;
using Ashlar.Core.Application.SelfImprovement.Ports;
using Ashlar.Core.Application.Trust.Ports;

namespace Ashlar.BackgroundAgents.Registry;

/// <summary>
/// Registry that owns the lifecycle of all background agent instances.
///
/// <para><b>Thread-safety:</b> All implementations must be safe for concurrent
/// access — the host (<c>BackgroundAgentService</c>) may register agents on the
/// startup thread while the scheduler invokes execution from timer callbacks.</para>
///
/// <para><b>Lifecycle ordering:</b>
/// <c>RegisterAsync</c> → <c>StartAsync / StartAllAsync</c> →
/// (scheduler-driven execution cycles) → <c>StopAsync / StopAllAsync</c>.
/// Calling <c>ExecuteOnceAsync</c> on an unregistered agent ID is an error.
/// Registration after <c>StartAllAsync</c> is permitted; newly registered agents
/// must be started individually.</para>
///
/// <para><b>Aggressiveness modes</b> (see <see cref="IAggressivenessModeStore"/>):
/// <list type="bullet">
///   <item><b>Passive</b> — execution is skipped entirely for extender agents.</item>
///   <item><b>SemiActive</b> — execution proceeds only after the
///         <see cref="IApprovalGate"/> grants approval.</item>
///   <item><b>Active</b> — execution runs without gating.</item>
///   <item><b>Ambient</b> — execution runs silently; actions are written to the
///         <see cref="IDataDecisionAuditLog"/> without user notification.</item>
/// </list></para>
///
/// <para><b>Failure modes:</b> When an execution cycle fails, the failure is
/// logged to <see cref="IBackgroundAgentLogStore"/> and the agent remains
/// registered. The scheduler will attempt the next cycle on the normal cadence;
/// there is no automatic back-off beyond what the scheduler itself applies.</para>
/// </summary>
public interface IBackgroundAgentRegistry
{
    /// <summary>
    /// Register a background agent. Must be called before the agent can be
    /// started or executed. Duplicate registration for the same agent ID
    /// overwrites the previous instance.
    /// </summary>
    /// <param name="origin">
    /// Registration provenance. Defaults to <see cref="AgentRegistrationOrigin.Machine"/> so
    /// callers that omit provenance must justify themselves with a resolvable parent.
    /// </param>
    Task RegisterAsync(
        IAgent agent,
        BackgroundAgentConfig config,
        AgentRegistrationOrigin origin = AgentRegistrationOrigin.Machine,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Look up a registered agent by ID. Returns <c>null</c> for unknown IDs
    /// rather than throwing, so callers can probe availability cheaply.
    /// </summary>
    BackgroundAgentInstance? GetAgent(string agentId);

    /// <summary>
    /// Snapshot of all currently registered agent instances. The returned list is
    /// a point-in-time copy — concurrent registrations won't appear until the
    /// next call.
    /// </summary>
    IReadOnlyList<BackgroundAgentInstance> GetAll();

    /// <summary>
    /// Start all registered agents, delegating scheduling to the
    /// <see cref="IAgentScheduler"/>.
    /// </summary>
    Task StartAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gracefully stop all running agents. In-flight execution cycles are
    /// cancelled via the token propagated through the scheduler.
    /// </summary>
    Task StopAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Start a single agent by ID. Throws if the agent is not registered.
    /// </summary>
    Task StartAsync(string agentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stop a single agent by ID. No-op if the agent is already stopped.
    /// </summary>
    Task StopAsync(string agentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Run exactly one execution cycle for an agent, bypassing the scheduler.
    /// Intended for manual invocations and integration tests. Aggressiveness
    /// mode gates still apply.
    /// </summary>
    Task ExecuteOnceAsync(string agentId, CancellationToken cancellationToken = default);
}
