namespace Nexo.Core.Domain.Bricks;

/// <summary>
/// LLM configuration for agentic implementations.
/// </summary>
public class LLMConfig
{
    /// <summary>Default model id for agentic execution.</summary>
    public string Model { get; init; } = "gpt-4";

    /// <summary>System prompt prepended to the model conversation.</summary>
    public string SystemPrompt { get; init; } = default!;

    /// <summary>Sampling temperature controlling output randomness.</summary>
    public double Temperature { get; init; } = 0.1;

    /// <summary>Maximum tokens the model may generate for this brick.</summary>
    public int MaxTokens { get; init; } = 2000;

    /// <summary>Optional tool ids the model may invoke during execution.</summary>
    public IReadOnlyList<string>? Tools { get; init; }
}
