using System;

namespace Nexo.Feature.Azure.Models;

/// <summary>
/// Azure connectivity test result
/// </summary>
public class AzureConnectivityResult
{
    /// <summary>
    /// Whether the connection was successful
    /// </summary>
    public bool IsSuccessful { get; set; }

    /// <summary>
    /// Connection message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Connection latency in milliseconds
    /// </summary>
    public long LatencyMs { get; set; }

    /// <summary>
    /// Test timestamp
    /// </summary>
    public DateTime TestedAt { get; set; }

    /// <summary>
    /// Error details if connection failed
    /// </summary>
    public string? ErrorDetails { get; set; }
} 