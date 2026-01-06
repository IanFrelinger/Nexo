namespace Nexo.Core.Application.Configuration.Models;

/// <summary>
/// Root configuration for Nexo CLI.
/// 
/// Contains:
/// - Analysis configuration
/// - Validation configuration
/// - Logging configuration
/// 
/// Loaded and saved by IConfigurationService.
/// Used throughout the application to configure behavior.
/// </summary>
public record NexoConfiguration
{
    public AnalysisConfiguration Analysis { get; init; } = new();
    public ValidationConfiguration Validation { get; init; } = new();
    public LoggingConfiguration Logging { get; init; } = new();
}

/// <summary>
/// Configuration for logging.
/// 
/// Contains:
/// - Log level (Information, Debug, etc.)
/// - Whether structured logging is enabled
/// - Whether progress indicators are enabled
/// 
/// Used to configure application-wide logging behavior.
/// </summary>
public record LoggingConfiguration
{
    public string Level { get; init; } = "Information";
    public bool EnableStructuredLogging { get; init; } = true;
    public bool EnableProgressIndicators { get; init; } = true;
}

