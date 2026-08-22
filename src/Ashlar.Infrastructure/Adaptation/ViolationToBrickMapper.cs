using Ashlar.Core.Application.Analysis.Models;

namespace Ashlar.Infrastructure.Adaptation;

/// <summary>
/// Maps analysis violations to brick IDs for the adaptation loop.
/// Block 4: closed-loop improve.
/// </summary>
public static class ViolationToBrickMapper
{
    private static readonly IReadOnlyDictionary<string, string> FileToBrick = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["ObservationContextBrick"] = "observation.context",
        ["OWASPScannerBrick"] = "owasp-scanner",
    };

    /// <summary>
    /// Gets the brick ID for a file path, if known.
    /// </summary>
    public static string? GetBrickIdForFile(string filePath)
    {
        var fileName = Path.GetFileNameWithoutExtension(filePath);
        if (string.IsNullOrEmpty(fileName)) return null;
        return FileToBrick.TryGetValue(fileName, out var brickId) ? brickId : null;
    }

    /// <summary>
    /// Maps violation Rule to FailureContext FailureType.
    /// </summary>
    public static string ToFailureType(Violation v) => v.Rule;
}
