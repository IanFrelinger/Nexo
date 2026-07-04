namespace Nexo.Core.Application.Configuration.Models;

/// <summary>
/// Configuration for validation operations.
/// 
/// Contains:
/// - Default test filter
/// - Timeout settings
/// - Test discovery patterns
/// - Failure behavior configuration
/// 
/// Used by IValidationService to configure validation behavior.
/// Part of NexoConfiguration.
/// </summary>
public record ValidationConfiguration
{
    /// <summary>Default xUnit filter applied when none is specified.</summary>
    public string? DefaultFilter { get; init; }

    /// <summary>Maximum validation run duration in seconds.</summary>
    public int TimeoutSeconds { get; init; } = 300;

    /// <summary>Whether to fail when no tests are discovered.</summary>
    public bool FailOnNoTests { get; init; } = false;

    /// <summary>Glob patterns used to locate test projects.</summary>
    public IReadOnlyList<string> TestProjectPatterns { get; init; } = new[] { "*Test*.csproj", "*Tests.csproj" };
}
