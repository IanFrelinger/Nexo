namespace Ashlar.Infrastructure.Testing.CodeAnalysis;

/// <summary>
/// Result of an assembly analysis operation.
/// </summary>
public record AssemblyAnalysisResult(
    bool Success,
    string? AssemblyName,
    string? Version,
    string? CultureName,
    IEnumerable<string> Types,
    IEnumerable<string> Methods,
    string? ErrorMessage,
    TimeSpan Duration);
