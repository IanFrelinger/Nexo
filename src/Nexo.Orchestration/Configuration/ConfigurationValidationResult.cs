using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;

namespace Nexo.Orchestration.Configuration;

/// <summary>
/// Result of configuration validation.
/// 
/// Contains:
/// - Whether configuration is valid
/// - List of configuration errors
/// - List of configuration warnings
/// 
/// Produced by ConfigurationValidator.Validate.
/// </summary>
public sealed record ConfigurationValidationResult
{
    /// <summary>Whether the configuration passed validation.</summary>
    public required bool IsValid { get; init; }

    /// <summary>Validation errors that must be resolved.</summary>
    public IReadOnlyList<ConfigurationError> Errors { get; init; } = new List<ConfigurationError>();

    /// <summary>Non-blocking validation warnings.</summary>
    public IReadOnlyList<ConfigurationWarning> Warnings { get; init; } = new List<ConfigurationWarning>();
}
