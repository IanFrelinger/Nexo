namespace Ashlar.Infrastructure.Testing.CodeAnalysis;

/// <summary>
/// Result of platform compatibility check.
/// </summary>
public record CodeAnalysisCompatibilityResult(
    string Platform,
    bool IsCompatible,
    List<string> Issues);
