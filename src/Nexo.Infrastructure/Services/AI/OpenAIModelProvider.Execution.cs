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
/// Model execution functionality
/// </summary>
public partial class OpenAiModelProvider
{
    public async Task<ModelResponse> ExecuteAsync(ModelRequest request, CancellationToken cancellationToken = default(CancellationToken))
    {
        _logger.LogInformation("Executing OpenAI request with model {Model}", request.Context?.TryGetValue("model", out var modelObj) == true ? modelObj as string : "gpt-4");

        try
        {
            var startTime = DateTime.UtcNow;
            
            // Determine the model to use
            var model = GetModelFromRequest(request);
            
            // Create the OpenAI request
            var openAiRequest = CreateOpenAiRequest(request, model);
            
            // Execute the request
            var response = await ExecuteOpenAiRequestAsync(openAiRequest, cancellationToken);
            
            var executionTime = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
            
            return new ModelResponse
            {
                Response = response.Choices?.FirstOrDefault()?.Message?.Content ?? string.Empty,
                InputTokens = response.Usage?.PromptTokens ?? 0,
                OutputTokens = response.Usage?.CompletionTokens ?? 0,
                ProcessingTimeMs = executionTime,
                Metadata = new Dictionary<string, object>
                {
                    ["finish_reason"] = response.Choices?.FirstOrDefault()?.FinishReason ?? "unknown",
                    ["usage"] = response.Usage ?? new object()
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing OpenAI request");
            throw;
        }
    }

    public Task<ModelValidationResult> ValidateRequestAsync(ModelRequest request)
    {
        var errors = new List<string>();

        // Validate required fields
        if (string.IsNullOrEmpty(request.Input))
        {
            errors.Add("Input is required");
        }

        // Validate model
        var model = GetModelFromRequest(request);
        if (!IsModelSupported(model))
        {
            errors.Add($"Model {model} is not supported");
        }

        // Validate token limits
        var estimatedTokens = EstimateTokenCount(request.Input);
        if (estimatedTokens > 128000)
        {
            errors.Add("Input exceeds maximum token limit");
        }

        return Task.FromResult(new ModelValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors
        });
    }
}
}
