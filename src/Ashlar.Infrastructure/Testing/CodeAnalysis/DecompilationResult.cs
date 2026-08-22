namespace Ashlar.Infrastructure.Testing.CodeAnalysis;

/// <summary>
/// Result of an assembly decompilation operation.
/// </summary>
public record DecompilationResult(
    bool Success,
    string? SourceCode,
    string? OutputPath,
    string? ErrorMessage,
    TimeSpan Duration);
