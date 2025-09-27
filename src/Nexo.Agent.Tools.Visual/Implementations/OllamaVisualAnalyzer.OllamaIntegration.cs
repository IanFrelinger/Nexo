using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Nexo.Agent.Tools.Visual.Contracts;

namespace Nexo.Agent.Tools.Visual.Implementations;

/// <summary>
/// OLLama API integration and communication functionality
/// </summary>
public sealed partial class OllamaVisualAnalyzer
{
    private async Task<string> CallOllamaVisionAsync(string prompt, string imageBase64, CancellationToken cancellationToken)
    {
        var requestBody = new
        {
            model = _visionModel,
            prompt = prompt,
            images = new[] { imageBase64 },
            stream = false
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync($"{_ollamaBaseUrl}/api/generate", content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var responseObj = JsonSerializer.Deserialize<JsonElement>(responseJson);
        
        return responseObj.GetProperty("response").GetString() ?? string.Empty;
    }
}
