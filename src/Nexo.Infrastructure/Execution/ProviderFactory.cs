using Microsoft.Extensions.Logging;

namespace Nexo.Infrastructure.Execution;

/// <summary>
/// Factory for creating and managing LLM providers.
/// </summary>
public class ProviderFactory : IProviderFactory
{
    private readonly ILogger<ProviderFactory> _logger;
    private readonly HashSet<string> _availableProviders = new() { "openai", "azure", "ollama", "mock" };
    
    public ProviderFactory(ILogger<ProviderFactory> logger)
    {
        _logger = logger;
    }
    
    public bool IsProviderAvailable(string provider)
    {
        return _availableProviders.Contains(provider.ToLowerInvariant());
    }
    
    public async Task<string> ExecuteLLMAsync(
        string provider,
        string systemPrompt,
        string userPrompt,
        object config,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executing LLM request with provider {Provider}", provider);
        
        // Mock implementation - in production, this would call actual LLM APIs
        await Task.Delay(100, cancellationToken); // Simulate network latency
        
        if (provider == "mock")
        {
            return "Mock LLM response: This is a simulated response for demo purposes.";
        }
        
        // In production, integrate with actual LLM providers
        // OpenAI, Azure OpenAI, Ollama, etc.
        throw new NotImplementedException($"Provider {provider} not yet implemented");
    }
}

