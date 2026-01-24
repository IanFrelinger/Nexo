using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace Nexo.API.Services;

/// <summary>
/// Service for sending webhook notifications.
/// </summary>
public class WebhookService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<WebhookService> _logger;

    public WebhookService(IHttpClientFactory httpClientFactory, ILogger<WebhookService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Send webhook notification for job completion.
    /// </summary>
    public async Task SendWebhookAsync(string webhookUrl, string jobId, string status, string? errorMessage = null)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(10);

            var payload = new
            {
                jobId,
                status,
                errorMessage,
                timestamp = DateTime.UtcNow
            };

            var response = await client.PostAsJsonAsync(webhookUrl, payload);
            response.EnsureSuccessStatusCode();

            _logger.LogInformation("Webhook sent successfully for job {JobId} to {WebhookUrl}", jobId, webhookUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send webhook for job {JobId} to {WebhookUrl}", jobId, webhookUrl);
            // Don't throw - webhook failures shouldn't fail the job
        }
    }
}
