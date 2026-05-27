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

    /// <summary>
    /// Query unified knowledge / pattern timeline (<c>GET /api/knowledge/query</c>).
    /// <paramref name="relativeQuery"/> is the query string including leading <c>api/knowledge/query</c> and parameters.
    /// </summary>
    Task<System.Text.Json.JsonElement> QueryKnowledgeAsync(string relativeQuery, CancellationToken cancellationToken = default);

    /// <summary>
    /// Escape hatch for API paths not yet exposed as typed methods (see <c>docs/api/index.md</c>).
    /// <paramref name="relativeUri"/> is appended to the client's base URL (leading slash optional).
    /// </summary>
    Task<HttpResponseMessage> InvokeAsync(
        HttpMethod method,
        string relativeUri,
        HttpContent? content = null,
        CancellationToken cancellationToken = default);
}
