using System;
using System.Collections.Generic;

namespace Nexo.Feature.Azure.Models;

/// <summary>
/// Azure subscription information
/// </summary>
public record AzureSubscriptionInfo
{
    public string SubscriptionId { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string LocationPlacementId { get; init; } = string.Empty;
    public Dictionary<string, string> Tags { get; init; } = new();
    public DateTime CreatedAt { get; init; }
} 