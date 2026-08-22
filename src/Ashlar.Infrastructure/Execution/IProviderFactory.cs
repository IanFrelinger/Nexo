namespace Ashlar.Infrastructure.Execution;

/// <summary>
/// Factory for creating LLM providers.
/// </summary>
public interface IProviderFactory
{
    /// <summary>Returns true if the provider is configured and available.</summary>
    bool IsProviderAvailable(string provider);
    /// <summary>Executes an LLM text request; returns model response as string.</summary>
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
    /// Execute video analysis via a model container (e.g. SmolVLM2-Video in Docker).
    /// Encodes frames to video, POSTs to VIDEO_SERVICE_URL, returns response as Understanding JSON.
    /// Used when provider is "video" and VIDEO_SERVICE_URL is set.
    /// </summary>
    Task<string> ExecuteVideoAsync(
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

