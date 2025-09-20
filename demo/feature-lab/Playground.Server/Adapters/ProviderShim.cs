using System.Net.Http;

namespace Playground.Server.Adapters;

public sealed class ProviderShim
{
    private readonly ILogger<ProviderShim> _logger;
    private string _mode = "off";
    private string _provider = "local";
    private string _model = "llama3";
    private bool _simulateOutage = false;
    private int _callCount = 0;

    public ProviderShim(ILogger<ProviderShim> logger)
    {
        _logger = logger;
    }

    public void SetMode(string mode, string provider, string model)
    {
        _mode = mode;
        _provider = provider;
        _model = model;
        _logger.LogInformation("Mode set to {Mode} with provider {Provider} and model {Model}", mode, provider, model);
    }

    public void SetSimulateOutage(bool enabled)
    {
        _simulateOutage = enabled;
        _callCount = 0; // Reset call count when toggling outage
        _logger.LogInformation("Simulate outage set to {Enabled}", enabled);
    }

    public async Task<string> ProcessWithProvider(string input, string operation)
    {
        _callCount++;
        
        // Simulate outage on first cloud call
        if (_simulateOutage && _provider != "local" && _callCount == 1)
        {
            _logger.LogWarning("Simulating outage for provider {Provider}", _provider);
            throw new HttpRequestException("Service temporarily unavailable (simulated outage)");
        }

        // Simulate processing based on mode and provider
        await Task.Delay(GetProcessingDelay());

        return _mode switch
        {
            "off" => GenerateOfflineResponse(input, operation),
            "hybrid" => GenerateHybridResponse(input, operation),
            "embedded" => GenerateEmbeddedResponse(input, operation),
            _ => GenerateOfflineResponse(input, operation)
        };
    }

    private int GetProcessingDelay()
    {
        return _mode switch
        {
            "off" => Random.Shared.Next(50, 150),
            "hybrid" => Random.Shared.Next(200, 500),
            "embedded" => Random.Shared.Next(300, 800),
            _ => Random.Shared.Next(100, 300)
        };
    }

    private string GenerateOfflineResponse(string input, string operation)
    {
        return operation switch
        {
            "classify_intent" => "urgent",
            "draft_reply" => "Thank you for your message. We are processing your request.",
            "summarize_contract" => "This is a contract summary generated offline.",
            "translate_and_tone" => "Translated text with professional tone",
            _ => $"Offline response for {operation}"
        };
    }

    private string GenerateHybridResponse(string input, string operation)
    {
        var baseResponse = GenerateOfflineResponse(input, operation);
        return $"{baseResponse} (Hybrid mode with {_provider})";
    }

    private string GenerateEmbeddedResponse(string input, string operation)
    {
        var baseResponse = GenerateOfflineResponse(input, operation);
        return $"{baseResponse} (Embedded mode with {_provider} {_model})";
    }

    public bool ShouldUseNetwork()
    {
        return _mode == "embedded" || (_mode == "hybrid" && _provider != "local");
    }

    public string GetCurrentMode() => _mode;
    public string GetCurrentProvider() => _provider;
    public string GetCurrentModel() => _model;
}
