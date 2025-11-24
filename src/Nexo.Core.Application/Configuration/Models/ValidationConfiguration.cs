namespace Nexo.Core.Application.Configuration.Models;

/// <summary>
/// Configuration for validation operations.
/// </summary>
public record ValidationConfiguration
{
    public string? DefaultFilter { get; init; }
    public int TimeoutSeconds { get; init; } = 300;
    public bool FailOnNoTests { get; init; } = false;
    public IReadOnlyList<string> TestProjectPatterns { get; init; } = new[] { "*Test*.csproj", "*Tests.csproj" };
}

