using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nexo.Commercial.Fleet.Contracts.Models;

namespace Nexo.Commercial.Fleet.Infrastructure.MeshLab;

public sealed class MeshLabWorkerExecutorClient
{
    public const string HttpClientName = "mesh-lab-worker-executor";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptionsMonitor<MeshLabWorkerExecutorOptions> _options;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MeshLabWorkerExecutorClient> _logger;

    public MeshLabWorkerExecutorClient(
        IHttpClientFactory httpClientFactory,
        IOptionsMonitor<MeshLabWorkerExecutorOptions> options,
        IConfiguration configuration,
        ILogger<MeshLabWorkerExecutorClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<int> TryProcessOneAssignedTaskAsync(CancellationToken cancellationToken)
    {
        var opts = _options.CurrentValue;
        var director = CreateDirectorClient(opts);
        var tasks = await ListTasksAsync(director, cancellationToken).ConfigureAwait(false);
        var candidate = PickCandidate(tasks, opts.TaskNamePrefix);
        if (candidate is null)
            return 0;

        return await ExecuteTaskAsync(director, candidate, opts, cancellationToken).ConfigureAwait(false)
            ? 1
            : 0;
    }

    public static MeshLabTaskSnapshot? PickCandidate(IReadOnlyList<MeshLabTaskSnapshot> tasks, string prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
            return null;

        foreach (var task in tasks)
        {
            if (!IsStatus(task.Status, MeshTaskStatus.Assigned))
                continue;
            if (string.IsNullOrWhiteSpace(task.LeaseToken))
                continue;
            if (string.IsNullOrWhiteSpace(task.AssignedApiBaseUrl))
                continue;
            var name = task.Name ?? string.Empty;
            if (!name.StartsWith(prefix, StringComparison.Ordinal))
                continue;
            return task;
        }

        return null;
    }

    public static bool IsStatus(string? status, MeshTaskStatus expected)
    {
        if (string.IsNullOrWhiteSpace(status))
            return false;
        if (int.TryParse(status, out var numeric) && numeric == (int)expected)
            return true;
        return string.Equals(status, expected.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    private async Task<bool> ExecuteTaskAsync(
        HttpClient director,
        MeshLabTaskSnapshot task,
        MeshLabWorkerExecutorOptions opts,
        CancellationToken cancellationToken)
    {
        var apiKey = ResolveApiKey(opts);
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("mesh-lab-worker-executor: no API key configured; skipping task {TaskId}", task.TaskId);
            return false;
        }

        var runningBody = new
        {
            status = (int)MeshTaskStatus.Running,
            leaseToken = task.LeaseToken,
            reason = "mesh-lab-worker-executor-running",
        };
        if (!await PatchStatusAsync(director, task.TaskId, runningBody, apiKey, cancellationToken).ConfigureAwait(false))
            return false;

        if (opts.ExecuteBrickOnAssignedPeer)
            await TryExecuteBrickOnAssignedPeerAsync(task.AssignedApiBaseUrl!, apiKey, cancellationToken).ConfigureAwait(false);

        var doneBody = new
        {
            status = (int)MeshTaskStatus.Succeeded,
            leaseToken = task.LeaseToken,
            resultSummary = opts.ResultSummary,
            reason = "mesh-lab-worker-executor-succeeded",
        };
        return await PatchStatusAsync(director, task.TaskId, doneBody, apiKey, cancellationToken).ConfigureAwait(false);
    }

    private async Task<bool> PatchStatusAsync(
        HttpClient director,
        string taskId,
        object body,
        string apiKey,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Patch, $"api/mesh/tasks/{Uri.EscapeDataString(taskId)}/status");
        request.Headers.TryAddWithoutValidation("X-Nexo-Api-Key", apiKey);
        request.Content = JsonContent.Create(body);
        using var response = await director.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            var detail = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogWarning(
                "mesh-lab-worker-executor: PATCH {TaskId} failed {StatusCode}: {Detail}",
                taskId,
                (int)response.StatusCode,
                detail.Length > 300 ? detail[..300] : detail);
            return false;
        }

        return true;
    }

    private async Task TryExecuteBrickOnAssignedPeerAsync(
        string assignedApiBaseUrl,
        string apiKey,
        CancellationToken cancellationToken)
    {
        var baseUrl = assignedApiBaseUrl.TrimEnd('/') + "/";
        var peer = _httpClientFactory.CreateClient(HttpClientName);
        peer.BaseAddress = new Uri(baseUrl);
        peer.DefaultRequestHeaders.TryAddWithoutValidation("X-Nexo-Api-Key", apiKey);

        try
        {
            using var catalogResponse = await peer.GetAsync("api/bricks", cancellationToken).ConfigureAwait(false);
            if (!catalogResponse.IsSuccessStatusCode)
                return;

            await using var catalogStream = await catalogResponse.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var doc = await JsonDocument.ParseAsync(catalogStream, cancellationToken: cancellationToken).ConfigureAwait(false);
            var brickId = TryReadFirstBrickId(doc.RootElement);
            if (string.IsNullOrWhiteSpace(brickId))
                return;

            var executeBody = new
            {
                brickId,
                implementation = "Deterministic",
                input = new Dictionary<string, object?>(),
            };
            using var executeResponse = await peer.PostAsJsonAsync(
                    $"api/bricks/{Uri.EscapeDataString(brickId)}/execute",
                    executeBody,
                    cancellationToken)
                .ConfigureAwait(false);
            if (!executeResponse.IsSuccessStatusCode)
            {
                _logger.LogDebug(
                    "mesh-lab-worker-executor: brick execute on {BaseUrl} returned {StatusCode}",
                    baseUrl,
                    (int)executeResponse.StatusCode);
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogDebug(ex, "mesh-lab-worker-executor: brick execute skipped on {BaseUrl}", baseUrl);
        }
    }

    private static string? TryReadFirstBrickId(JsonElement root)
    {
        if (!root.TryGetProperty("bricks", out var bricks) && !root.TryGetProperty("Bricks", out bricks))
            return null;
        if (bricks.ValueKind != JsonValueKind.Array || bricks.GetArrayLength() == 0)
            return null;

        var first = bricks[0];
        foreach (var prop in new[] { "id", "Id", "brickId", "BrickId" })
        {
            if (first.TryGetProperty(prop, out var id) && id.ValueKind == JsonValueKind.String)
            {
                var value = id.GetString();
                if (!string.IsNullOrWhiteSpace(value))
                    return value;
            }
        }

        return null;
    }

    private async Task<IReadOnlyList<MeshLabTaskSnapshot>> ListTasksAsync(HttpClient director, CancellationToken cancellationToken)
    {
        using var response = await director.GetAsync("api/mesh/tasks", cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        var list = await JsonSerializer.DeserializeAsync<List<MeshLabTaskSnapshot>>(stream, JsonOptions, cancellationToken).ConfigureAwait(false);
        return list ?? [];
    }

    private HttpClient CreateDirectorClient(MeshLabWorkerExecutorOptions opts)
    {
        var client = _httpClientFactory.CreateClient(HttpClientName);
        var baseUrl = (opts.DirectorBaseUrl ?? string.Empty).TrimEnd('/') + "/";
        client.BaseAddress = new Uri(baseUrl);
        var apiKey = ResolveApiKey(opts);
        client.DefaultRequestHeaders.Remove("X-Nexo-Api-Key");
        if (!string.IsNullOrWhiteSpace(apiKey))
            client.DefaultRequestHeaders.TryAddWithoutValidation("X-Nexo-Api-Key", apiKey);
        return client;
    }

    private string? ResolveApiKey(MeshLabWorkerExecutorOptions opts) =>
        string.IsNullOrWhiteSpace(opts.ApiKey)
            ? _configuration["Nexo:Security:ApiKey"]
            : opts.ApiKey.Trim();
}

public sealed record MeshLabTaskSnapshot(
    string TaskId,
    string? Name,
    string? Status,
    string? AssignedApiBaseUrl,
    string? LeaseToken);
