namespace Nexo.Client;

/// <summary>
/// Client interface for Nexo API.
/// </summary>
public interface INexoClient
{
    /// <summary>
    /// Invoke an agent by name.
    /// </summary>
    Task<AgentResponse> RunAgentAsync(AgentRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Run validation tests.
    /// </summary>
    Task<ValidationResponse> RunValidationAsync(ValidationRequest? request = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Run orchestration workflow.
    /// </summary>
    Task<OrchestrationResponse> OrchestrateAsync(OrchestrationRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get background agent status and mode.
    /// </summary>
    Task<StatusResponse> GetStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Build a container image from Dockerfile.
    /// </summary>
    Task<ExecutionBuildResponse> BuildImageAsync(ExecutionBuildRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Run a container.
    /// </summary>
    Task<ExecutionRunResponse> RunContainerAsync(ExecutionRunRequest request, CancellationToken cancellationToken = default);
}
