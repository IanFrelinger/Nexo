using System;

namespace Nexo.Feature.Azure.Models;

/// <summary>
/// Azure service issue information
/// </summary>
public record AzureServiceIssue
{
    public string ServiceName { get; init; } = string.Empty;
    public string IssueType { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime StartTime { get; init; }
    public DateTime? EndTime { get; init; }
} 