using System.Text.Json;

namespace Ashlar.Orchestration.Assets.Models;

/// <summary>
/// Validation of generated asset against constraints.
/// </summary>
public sealed record AssetValidation
{
    /// <summary>Whether the asset passed all required checks.</summary>
    public bool IsValid { get; init; }

    /// <summary>Validation checks that passed.</summary>
    public IReadOnlyList<string> PassedChecks { get; init; } = Array.Empty<string>();

    /// <summary>Validation checks that failed.</summary>
    public IReadOnlyList<string> FailedChecks { get; init; } = Array.Empty<string>();

    /// <summary>Non-blocking validation warnings.</summary>
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
}
