using Ashlar.Abstractions.Transport;
using Ashlar.Orchestration.Architect.Models;

namespace Ashlar.Orchestration.Transport;

/// <summary>
/// Context passed to agent transport invocation hooks (metadata policy, tracing, etc.).
/// </summary>
public sealed record AgentInvocationContext(
    string CorrelationId,
    string OrchestratorRequest,
    AgentSpawnSpec Spec,
    AgentInvocationRequest Request);
