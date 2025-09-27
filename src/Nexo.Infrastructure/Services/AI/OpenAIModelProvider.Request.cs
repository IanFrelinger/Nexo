using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Enums;
using Nexo.Feature.AI.Models;
using Nexo.Feature.AI.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace Nexo.Infrastructure.Services.AI
{
/// <summary>
/// Request processing functionality
/// </summary>
public partial class OpenAiModelProvider
{
    private string GetModelFromRequest(ModelRequest request)
    {
        // Try to get model from context first
        if (request.Context?.TryGetValue("model", out var modelObj) == true && modelObj is string model)
        {
            return model;
        }

        // Default to GPT-4
        return "gpt-4";
    }

    private static bool IsModelSupported(string model)
    {
        var supportedModels = new[] { "gpt-4", "gpt-4-turbo", "gpt-3.5-turbo", "gpt-3.5-turbo-16k" };
        return supportedModels.Contains(model.ToLower());
    }

    private object CreateOpenAiRequest(ModelRequest request, string model)
    {
        var openAiRequest = new Dictionary<string, object>
        {
            ["model"] = model,
            ["messages"] = CreateMessages(request),
            ["temperature"] = request.Context?.TryGetValue("temperature", out var tempObj) == true ? tempObj : request.Temperature,
            ["max_tokens"] = request.Context?.TryGetValue("max_tokens", out var maxTokensObj) == true ? maxTokensObj : request.MaxTokens,
            ["top_p"] = request.Context?.TryGetValue("top_p", out var topPObj) == true ? topPObj : 0.9,
            ["frequency_penalty"] = request.Context?.TryGetValue("frequency_penalty", out var freqPenaltyObj) == true ? freqPenaltyObj : 0.0,
            ["presence_penalty"] = request.Context?.TryGetValue("presence_penalty", out var presPenaltyObj) == true ? presPenaltyObj : 0.0
        };

        return openAiRequest;
    }

    private List<object> CreateMessages(ModelRequest request)
    {
        var messages = new List<object>();

        // Add system message if provided
        if (!string.IsNullOrEmpty(request.SystemPrompt))
        {
            messages.Add(new { role = "system", content = request.SystemPrompt });
        }

        // Add user message
        messages.Add(new { role = "user", content = request.Input });

        return messages;
    }

    private async Task<OpenAiResponse> ExecuteOpenAiRequestAsync(object request, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("chat/completions", content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<OpenAiResponse>(responseContent) ?? new OpenAiResponse();
    }

    private static int EstimateTokenCount(string text)
    {
        // Simple estimation: ~4 characters per token
        return text.Length / 4;
    }
}
}
