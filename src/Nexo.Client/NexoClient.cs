using System.Net.Http.Json;
using System.Text.Json;

namespace Nexo.Client;

/// <summary>
/// HTTP client implementation for Nexo API.
/// </summary>
public sealed class NexoClient : INexoClient
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Creates a client that issues HTTP requests against a configured Nexo API host.
    /// </summary>
    /// <param name="httpClient">Pre-configured <see cref="HttpClient"/> with base address and auth headers.</param>
    public NexoClient(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    /// <inheritdoc />
    public async Task<AgentResponse> RunAgentAsync(AgentRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/agent", request, _jsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AgentResponse>(_jsonOptions, cancellationToken))!;
    }

    /// <inheritdoc />
    public async Task<ValidationResponse> RunValidationAsync(ValidationRequest? request = null, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/validate", request ?? new ValidationRequest(), _jsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ValidationResponse>(_jsonOptions, cancellationToken))!;
    }

    /// <inheritdoc />
    public async Task<OrchestrationResponse> OrchestrateAsync(OrchestrationRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/orchestrate", request, _jsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<OrchestrationResponse>(_jsonOptions, cancellationToken))!;
    }

    /// <inheritdoc />
    public async Task<StatusResponse> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("api/status", cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<StatusResponse>(_jsonOptions, cancellationToken))!;
    }

    /// <inheritdoc />
    public async Task<ExecutionBuildResponse> BuildImageAsync(ExecutionBuildRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/execution/build", request, _jsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ExecutionBuildResponse>(_jsonOptions, cancellationToken))!;
    }

    /// <inheritdoc />
    public async Task<ExecutionRunResponse> RunContainerAsync(ExecutionRunRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/execution/run", request, _jsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ExecutionRunResponse>(_jsonOptions, cancellationToken))!;
    }

    /// <inheritdoc />
    public async Task<JsonElement> QueryKnowledgeAsync(string relativeQuery, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(relativeQuery))
            throw new ArgumentException("Query path is required.", nameof(relativeQuery));

        var path = relativeQuery.TrimStart('/');
        var response = await _httpClient.GetAsync(path, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions, cancellationToken).ConfigureAwait(false))!;
    }

    /// <inheritdoc />
    public async Task<HttpResponseMessage> InvokeAsync(
        HttpMethod method,
        string relativeUri,
        HttpContent? content = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(relativeUri))
        {
            throw new ArgumentException("Relative URI is required.", nameof(relativeUri));
        }

        var path = relativeUri.TrimStart('/');
        using var request = new HttpRequestMessage(method, path) { Content = content };
        return await _httpClient
            .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);
    }
}
