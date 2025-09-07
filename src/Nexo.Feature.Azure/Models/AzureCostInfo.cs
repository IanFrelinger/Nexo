using System;
using System.Collections.Generic;

namespace Nexo.Feature.Azure.Models;

/// <summary>
/// Azure cost and usage information
/// </summary>
public record AzureCostInfo
{
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public decimal TotalCost { get; init; }
    public string Currency { get; init; } = string.Empty;
    public List<AzureCostBreakdown> Breakdown { get; init; } = new();
    public Dictionary<string, decimal> ServiceCosts { get; init; } = new();
} 