using System;
using System.Collections.Generic;

namespace Nexo.Feature.Azure.Models;

/// <summary>
/// Azure service health information
/// </summary>
public record AzureServiceHealthInfo
{
    public string Region { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public List<AzureServiceIssue> Issues { get; init; } = new();
    public DateTime LastUpdated { get; init; }
} 