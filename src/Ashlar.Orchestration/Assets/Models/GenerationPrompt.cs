using System.Text.Json;

namespace Ashlar.Orchestration.Assets.Models;

/// <summary>
/// The prompt and parameters used for generation.
/// </summary>
public sealed record GenerationPrompt
{
    /// <summary>Primary text prompt for generation.</summary>
    public required string TextPrompt { get; init; }

    /// <summary>Negative prompt excluding unwanted features.</summary>
    public string? NegativePrompt { get; init; }

    /// <summary>Optional style reference identifier or URL.</summary>
    public string? StyleReference { get; init; }

    /// <summary>Generation service parameters.</summary>
    public IReadOnlyDictionary<string, object> Parameters { get; init; } =
        new Dictionary<string, object>();
}
