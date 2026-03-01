namespace Nexo.BackgroundAgents.Trust;

/// <summary>
/// Context being sent to cloud (LLM/vision provider).
/// </summary>
public sealed class OutgoingContext
{
    /// <summary>System prompt text.</summary>
    public string SystemPrompt { get; set; } = string.Empty;

    /// <summary>User prompt text.</summary>
    public string UserPrompt { get; set; } = string.Empty;

    /// <summary>Additional variables or metadata.</summary>
    public IReadOnlyDictionary<string, string> Variables { get; set; } = new Dictionary<string, string>();

    /// <summary>Whether the execution is air-gapped (no cloud).</summary>
    public bool IsAirGapped { get; set; }

    /// <summary>Provider name (e.g. openai, ollama).</summary>
    public string Provider { get; set; } = string.Empty;
}
