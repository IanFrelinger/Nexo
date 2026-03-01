using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Common.Ports;

namespace Nexo.Infrastructure.Workflows;

/// <summary>
/// HTTP implementation of workflow webhook client.
/// </summary>
public sealed class HttpWorkflowWebhookClient : IWorkflowWebhookClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<HttpWorkflowWebhookClient>? _logger;

    public HttpWorkflowWebhookClient(
        IHttpClientFactory httpClientFactory,
        ILogger<HttpWorkflowWebhookClient>? logger = null)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _logger = logger;
    }

    public async Task<string> GetAsync(string url, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("Webhook URL is required", nameof(url));

        using var client = _httpClientFactory.CreateClient();
        var response = await client.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(ct);
    }

    public async Task PostAsync(string url, object data, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("Webhook URL is required", nameof(url));

        using var client = _httpClientFactory.CreateClient();
        var response = await client.PostAsJsonAsync(url, data, ct);
        response.EnsureSuccessStatusCode();
    }
}
