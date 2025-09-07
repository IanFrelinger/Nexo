using System;

namespace Nexo.Feature.Azure.Models;

/// <summary>
/// Azure operation result
/// </summary>
public class AzureOperationResult
{
    /// <summary>
    /// Whether the operation was successful
    /// </summary>
    public bool IsSuccessful { get; set; }

    /// <summary>
    /// Operation message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Operation timestamp
    /// </summary>
    public DateTime OperatedAt { get; set; }

    /// <summary>
    /// Error details if operation failed
    /// </summary>
    public string? ErrorDetails { get; set; }
} 