namespace Nexo.Spike.S3.Models;

/// <summary>
/// Intent specification exposed to generators — acceptance criteria deliberately excluded.
/// </summary>
public sealed record SealedIntentSpec(
    string IntentId,
    string Title,
    string Description,
    IReadOnlyList<string> Inputs,
    IReadOnlyList<string> Outputs,
    IReadOnlyList<string> Invariants);
