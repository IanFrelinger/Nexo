namespace Nexo.Infrastructure.Testing.CodeAnalysis;

/// <summary>
/// Result of a code compilation operation.
/// </summary>
public record CompilationResult(
    bool Success,
    string? AssemblyPath,
    IEnumerable<string> Errors,
    IEnumerable<string> Warnings,
    TimeSpan Duration);
