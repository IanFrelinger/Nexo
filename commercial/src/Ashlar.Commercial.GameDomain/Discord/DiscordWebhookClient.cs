namespace Ashlar.Commercial.GameDomain.Discord;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

/// <summary>
/// Posts messages and files to a Discord webhook. Rate-limited.
/// Does NOT require a bot token — webhooks are one-way (post only).
/// For bidirectional interaction (reactions), use DiscordGatewayClient.
/// </summary>
public sealed class DiscordWebhookClient : IDisposable
{
    private readonly HttpClient _http;
    private readonly DiscordRateLimiter _rateLimiter;
    private readonly string _webhookUrl;
    private readonly long _maxUploadBytes;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public DiscordWebhookClient(string webhookUrl, long maxUploadBytes = 25 * 1024 * 1024)
    {
        _webhookUrl = webhookUrl ?? throw new ArgumentNullException(nameof(webhookUrl));
        _maxUploadBytes = maxUploadBytes;
        _http = new HttpClient();
        _rateLimiter = new DiscordRateLimiter();
    }

    /// <summary>Post a text message to the webhook channel.</summary>
    public async Task<bool> SendMessageAsync(string content, CancellationToken ct = default)
    {
        return await _rateLimiter.ExecuteAsync(async () =>
        {
            var payload = JsonSerializer.Serialize(new { content }, JsonOptions);
            using var response = await _http.PostAsync(_webhookUrl,
                new StringContent(payload, Encoding.UTF8, "application/json"), ct);
            return response.IsSuccessStatusCode;
        }, ct);
    }

    /// <summary>Post a rich embed message to the webhook channel.</summary>
    public async Task<bool> SendEmbedAsync(DiscordEmbed embed, CancellationToken ct = default)
    {
        return await _rateLimiter.ExecuteAsync(async () =>
        {
            var payload = JsonSerializer.Serialize(new { embeds = new[] { embed } }, JsonOptions);
            using var response = await _http.PostAsync(_webhookUrl,
                new StringContent(payload, Encoding.UTF8, "application/json"), ct);
            return response.IsSuccessStatusCode;
        }, ct);
    }

    /// <summary>Post a message with a file attachment (video clip, image, etc.).</summary>
    public async Task<bool> SendFileAsync(string filePath, string? message = null, CancellationToken ct = default)
    {
        if (!File.Exists(filePath)) return false;
        var fileInfo = new FileInfo(filePath);
        if (fileInfo.Length > _maxUploadBytes) return false;

        return await _rateLimiter.ExecuteAsync(async () =>
        {
            using var form = new MultipartFormDataContent();
            if (message != null)
            {
                var payloadJson = JsonSerializer.Serialize(new { content = message }, JsonOptions);
                form.Add(new StringContent(payloadJson, Encoding.UTF8, "application/json"), "payload_json");
            }
            var fileContent = new ByteArrayContent(await File.ReadAllBytesAsync(filePath, ct));
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("video/mp4");
            form.Add(fileContent, "files[0]", Path.GetFileName(filePath));

            using var response = await _http.PostAsync(_webhookUrl, form, ct);
            return response.IsSuccessStatusCode;
        }, ct);
    }

    /// <summary>Releases resources held by this instance.</summary>
    public void Dispose() => _http.Dispose();
}
