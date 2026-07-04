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
    /// <summary>Settings for code analysis operations.</summary>
    public AnalysisConfiguration Analysis { get; init; } = new();

    /// <summary>Settings for validation test runs.</summary>
    public ValidationConfiguration Validation { get; init; } = new();

    /// <summary>Settings for application logging.</summary>
    public LoggingConfiguration Logging { get; init; } = new();
}
