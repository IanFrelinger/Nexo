namespace Ashlar.Core.Domain.Export;

/// <summary>
/// Configuration for AI generation during export.
/// </summary>
public class GenerationConfig
{
    /// <summary>
    /// How many variations to generate per item.
    /// </summary>
    public int VariationsPerItem { get; init; } = 10;
    
    /// <summary>
    /// Provider to use for generation.
    /// </summary>
    public string Provider { get; init; } = "openai";
    
    /// <summary>
    /// Model to use.
    /// </summary>
    public string Model { get; init; } = "gpt-4";
    
    /// <summary>
    /// Human review required before export.
    /// </summary>
    public bool RequireReview { get; init; } = true;
}
