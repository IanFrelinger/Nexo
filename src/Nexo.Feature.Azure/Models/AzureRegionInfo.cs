using System.Collections.Generic;

namespace Nexo.Feature.Azure.Models;

/// <summary>
/// Azure region information
/// </summary>
public record AzureRegionInfo
{
    public string Name { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string RegionalDisplayName { get; init; } = string.Empty;
    public List<string> AvailableServices { get; init; } = new();
    public bool IsAvailable { get; init; }
} 