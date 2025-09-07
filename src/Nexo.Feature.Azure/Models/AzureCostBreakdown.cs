using System;

namespace Nexo.Feature.Azure.Models;

/// <summary>
/// Azure cost breakdown by service
/// </summary>
public record AzureCostBreakdown
{
    public string ServiceName { get; init; } = string.Empty;
    public string ResourceGroup { get; init; } = string.Empty;
    public string ResourceName { get; init; } = string.Empty;
    public decimal Cost { get; init; }
    public string Currency { get; init; } = string.Empty;
    public DateTime UsageDate { get; init; }
} 