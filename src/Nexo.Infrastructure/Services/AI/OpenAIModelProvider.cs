using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nexo.Feature.AI.Enums;
using Nexo.Feature.AI.Models;
using Nexo.Feature.AI.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using static Nexo.Feature.AI.Enums.ModelType;

namespace Nexo.Infrastructure.Services.AI
{
/// <summary>
/// OpenAI model provider implementation.
/// This class acts as an orchestrator, delegating specific functionalities to partial class implementations.
/// </summary>
public partial class OpenAiModelProvider : IModelProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenAiModelProvider> _logger;
    private readonly string _apiKey;
    private readonly Dictionary<string, object> _defaultParameters;

    public OpenAiModelProvider(
        HttpClient httpClient,
        ILogger<OpenAiModelProvider> logger,
        string apiKey,
        string baseUrl = "https://api.openai.com/v1")
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));

        // Configure default parameters
        _defaultParameters = new Dictionary<string, object>
        {
            ["temperature"] = 0.7,
            ["max_tokens"] = 2000,
            ["top_p"] = 1.0,
            ["frequency_penalty"] = 0.0,
            ["presence_penalty"] = 0.0
        };

        // Configure HTTP client
        _httpClient.BaseAddress = new Uri(baseUrl);
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Nexo-AI-Provider/1.0");
    }
    // This class acts as an orchestrator for various OpenAI model provider functionalities,
    // with specific categories defined in partial classes.
}
}