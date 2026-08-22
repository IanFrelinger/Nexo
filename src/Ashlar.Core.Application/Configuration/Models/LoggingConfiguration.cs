namespace Ashlar.Core.Application.Configuration.Models;

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
    /// <summary>Minimum log level (e.g. Information, Debug).</summary>
    public string Level { get; init; } = "Information";

    /// <summary>Whether structured logging output is enabled.</summary>
    public bool EnableStructuredLogging { get; init; } = true;

    /// <summary>Whether CLI progress indicators are shown during long operations.</summary>
    public bool EnableProgressIndicators { get; init; } = true;
}
