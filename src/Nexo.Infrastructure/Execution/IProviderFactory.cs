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

    /// <summary>
    /// Execute LLM with multiple frames (poor man's video: recent temporal context).
    /// Models like Llama 3.2 Vision, Gemma 3, Qwen 2.5 VL support multiple images in one request.
    /// Falls back to single-frame (most recent) if provider doesn't support multi-image.
    /// </summary>
    Task<string> ExecuteVisionMultiFrameAsync(
        string provider,
        string systemPrompt,
        string userPrompt,
        IReadOnlyList<byte[]> frameBytes,
        object config,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Ensures Ollama is reachable and, if required, that a vision-capable model is available.
    /// Throws if unreachable or if requireVisionModel and no llava model is present.
    /// </summary>
    Task EnsureOllamaReachableAsync(bool requireVisionModel, CancellationToken cancellationToken = default);
}

