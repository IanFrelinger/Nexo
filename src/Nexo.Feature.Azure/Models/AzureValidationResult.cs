using System;

namespace Nexo.Feature.Azure.Models;

/// <summary>
/// Azure validation result
/// </summary>
public class AzureValidationResult
{
    /// <summary>
    /// Whether the validation was successful
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Validation message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Validation timestamp
    /// </summary>
    public DateTime ValidatedAt { get; set; }

    /// <summary>
    /// Error details if validation failed
    /// </summary>
    public string? ErrorDetails { get; set; }
} 