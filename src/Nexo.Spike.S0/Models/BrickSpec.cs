using System.Text.Json.Serialization;

namespace Nexo.Spike.S0.Models;

/// <summary>
/// Frozen brick contract produced at the SPEC stage.
/// </summary>
public sealed record BrickSpec
{
    public required string IntentId { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required IReadOnlyList<string> Inputs { get; init; }
    public required IReadOnlyList<string> Outputs { get; init; }
    public required IReadOnlyList<string> Invariants { get; init; }
    public required IReadOnlyList<string> AcceptanceCriteria { get; init; }
    public bool Adversarial { get; init; }
    public string? AdversarialKind { get; init; }
    public IReadOnlyList<string> OpenQuestions { get; init; } = [];

    [JsonIgnore]
    public bool HasOpenQuestions => OpenQuestions.Count > 0;
}
