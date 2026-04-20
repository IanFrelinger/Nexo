using Nexo.Abstractions.Transport;
using Nexo.Orchestration.Architect.Models;

namespace Nexo.Orchestration.Transport;

/// <summary>
/// Context passed to agent transport invocation hooks (metadata policy, tracing, etc.).
/// </summary>
public sealed record AgentInvocationContext(
    string CorrelationId,
    string OrchestratorRequest,
    AgentSpawnSpec Spec,
    AgentInvocationRequest Request);
