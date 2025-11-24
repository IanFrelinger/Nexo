namespace Nexo.Core.Application.Configuration.Models;

/// <summary>
/// Root configuration for Nexo CLI.
/// </summary>
public record NexoConfiguration
{
    public AnalysisConfiguration Analysis { get; init; } = new();
    public ValidationConfiguration Validation { get; init; } = new();
    public LoggingConfiguration Logging { get; init; } = new();
}

/// <summary>
/// Configuration for logging.
/// </summary>
public record LoggingConfiguration
{
    public string Level { get; init; } = "Information";
    public bool EnableStructuredLogging { get; init; } = true;
    public bool EnableProgressIndicators { get; init; } = true;
}

