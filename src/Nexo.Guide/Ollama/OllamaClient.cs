using System.Net.Http.Json;
using System.Text.Json;

namespace Nexo.Guide.Ollama;

public class OllamaClient : IOllamaClient
{
    private readonly HttpClient _http;
    private readonly string _model;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

    public OllamaClient(string endpoint, string model)
    {
        _http = new HttpClient { BaseAddress = new Uri(endpoint.TrimEnd('/')) };
        _model = model;
    }

    public async Task<string> GenerateAsync(IReadOnlyList<OllamaMessage> messages, CancellationToken cancellationToken = default)
    {
        var request = new
        {
            model = _model,
            messages = messages.Select(m => new { role = m.Role, content = m.Content }).ToArray(),
            stream = false
        };

        using var response = await _http.PostAsJsonAsync("/api/chat", request, JsonOptions, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<OllamaChatResponse>(JsonOptions, cancellationToken).ConfigureAwait(false);
        return body?.Message?.Content ?? "";
    }
}

internal class OllamaChatResponse
{
    public OllamaMessagePart? Message { get; set; }
}

internal class OllamaMessagePart
{
    public string? Content { get; set; }
}
