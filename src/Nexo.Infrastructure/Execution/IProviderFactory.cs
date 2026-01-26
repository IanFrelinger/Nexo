namespace Nexo.Infrastructure.Execution;

/// <summary>
/// Factory for creating LLM providers.
/// </summary>
public interface IProviderFactory
{
    bool IsProviderAvailable(string provider);
    Task<string> ExecuteLLMAsync(
        string provider,
        string systemPrompt,
        string userPrompt,
        object config,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Execute LLM with vision support (image analysis).
    /// </summary>
    Task<string> ExecuteVisionAsync(
        string provider,
        string systemPrompt,
        string userPrompt,
        byte[] imageBytes,
        object config,
        CancellationToken cancellationToken = default);
}

